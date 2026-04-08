using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using api.Dtos.AiRecommendation;
using api.Interfaces;
using api.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace api.Service
{
    public class GeminiRecommendationService : IAiRecommendationService
    {
        private readonly HttpClient _httpClient;
        private readonly IStockDataService _stockDataService;
        private readonly GeminiSettings _geminiSettings;
        private readonly ILogger<GeminiRecommendationService> _logger;

        private readonly string[] _allowedRatings = {
            "Highly Recommended",
            "Recommended",
            "Neutral",
            "Not Recommended",
            "Strongly Not Recommended"
        };

        public GeminiRecommendationService(
            HttpClient httpClient,
            IStockDataService stockDataService,
            IOptions<GeminiSettings> geminiOptions,
            ILogger<GeminiRecommendationService> logger)
        {
            _httpClient = httpClient;
            _stockDataService = stockDataService;
            _geminiSettings = geminiOptions.Value;
            _logger = logger;
        }

        public async Task<AiRecommendationResponseDto?> GetRecommendationAsync(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol)) return null;
            symbol = symbol.Trim().ToUpper();

            try
            {
                
                _logger.LogInformation("Gathering grounded data for {Symbol}...", symbol);
                
                var priceData = await _stockDataService.GetCurrentPriceAsync(symbol);
                if (priceData == null)
                {
                    _logger.LogWarning("Critical data missing: Could not fetch current price for {Symbol}", symbol);
                    return null; 
                }

                
                CompanyInfo? companyInfo = null;
                try 
                { 
                    companyInfo = await _stockDataService.GetCompanyInfoAsync(symbol); 
                } 
                catch (HttpRequestException) 
                { 
                    _logger.LogWarning("Proceeding with partial data: CompanyInfo was rate-limited for {Symbol}", symbol); 
                }

                List<HistoricalPrice>? history = null;
                try 
                { 
                    history = await _stockDataService.GetHistoricalPricesAsync(symbol, 30); 
                } 
                catch (HttpRequestException) 
                { 
                    _logger.LogWarning("Proceeding with partial data: Historical trends were rate-limited for {Symbol}", symbol); 
                }

                if (companyInfo == null) _logger.LogInformation("CompanyInfo was unavailable for {Symbol}", symbol);
                if (history == null || !history.Any()) _logger.LogInformation("Historical trends were unavailable for {Symbol}", symbol);

                
                string prompt = BuildPrompt(symbol, companyInfo, priceData, history);

                
                var response = await CallGeminiAsync(prompt);
                
                if (string.IsNullOrWhiteSpace(response)) return null;

                
                var recommendation = ParseAndValidateResponse(response, symbol, companyInfo?.CompanyName ?? symbol);
                
                return recommendation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating AI recommendation for {Symbol}", symbol);
                throw;
            }
        }

        private string BuildPrompt(string symbol, CompanyInfo? info, StockPriceData price, List<HistoricalPrice>? history)
        {
            var trend = (history != null && history.Any())
                ? string.Join(", ", history.Select(h => $"${h.Close} ({h.Date:yyyy-MM-dd})"))
                : "No recent history available for trend analysis.";

            var companyName = info?.CompanyName ?? symbol;
            var industry = info?.Industry ?? "Unknown";
            var marketCap = info?.MarketCap != null ? $"${info.MarketCap:N0}" : "Unknown";

            return $@"
                You are a senior financial analyst for the NETPRO platform. 
                Your task is to provide a grounded stock recommendation based on the following REAL market data for {symbol}.

                --- STOCK CONTEXT ---
                - Symbol: {symbol}
                - Company: {companyName}
                - Industry: {industry}
                - Market Cap: {marketCap}
                - Current Price: ${price.CurrentPrice}
                - Daily Change: {price.ChangeAmount} ({price.ChangePercent}%)
                - Recent 30-Day Trend (Closing Prices): {trend}
                {(info == null ? "\n[NOTE: Detailed company profile was unavailable. Base your analysis on price action and general knowledge of this symbol.]" : "")}

                --- INSTRUCTIONS ---
                1. Analyze the available data briefly.
                2. Judge the stock's current position and provide a specific rating.
                3. You MUST choose exactly one of these ratings:
                   - Highly Recommended
                   - Recommended
                   - Neutral
                   - Not Recommended
                   - Strongly Not Recommended
                4. Provide a very short, 1-2 sentence summary explaining the rating. Keep it brief.
                5. Return the response ONLY as a structured JSON object.

                --- REQUIRED JSON SCHEMA ---
                {{
                    ""rating"": ""string"",
                    ""summary"": ""string""
                }}
            ";
        }

        private async Task<string?> CallGeminiAsync(string prompt)
        {
            var url = $"{_geminiSettings.BaseUrl}/v1beta/models/{_geminiSettings.Model}:generateContent?key={_geminiSettings.ApiKey}";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                },
                generationConfig = new
                {
                    responseMimeType = "application/json"
                }
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            
            int maxRetries = 2;
            int delayMs = 2000;

            for (int i = 0; i <= maxRetries; i++)
            {
                try
                {
                    var response = await _httpClient.PostAsync(url, jsonContent);

                    if (response.IsSuccessStatusCode)
                    {
                        var jsonResponse = await response.Content.ReadAsStringAsync();
                        using var doc = JsonDocument.Parse(jsonResponse);
                        return doc.RootElement
                            .GetProperty("candidates")[0]
                            .GetProperty("content")
                            .GetProperty("parts")[0]
                            .GetProperty("text")
                            .GetString();
                    }

                    var errorBody = await response.Content.ReadAsStringAsync();
                    
                    
                    if ((response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable || 
                         (int)response.StatusCode == 429) && i < maxRetries)
                    {
                        _logger.LogWarning("Gemini API busy (Status {Status}). Retry {Count} in {Delay}ms...", response.StatusCode, i + 1, delayMs);
                        await Task.Delay(delayMs);
                        delayMs *= 2; 
                        continue;
                    }

                    _logger.LogError("Gemini API call failed with status {Status}: {Error}", response.StatusCode, errorBody);
                    throw new HttpRequestException($"AI Service error: {response.StatusCode}", null, response.StatusCode);
                }
                catch (HttpRequestException ex) when (i < maxRetries && (ex.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable || (int?)ex.StatusCode == 429))
                {
                    _logger.LogWarning(ex, "Gemini connection error. Retry {Count}...", i + 1);
                    await Task.Delay(delayMs);
                    delayMs *= 2;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to communicate with Gemini API.");
                    throw;
                }
            }

            return null;
        }

        private AiRecommendationResponseDto? ParseAndValidateResponse(string json, string symbol, string companyName)
        {
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var raw = JsonSerializer.Deserialize<GeminiResponseRaw>(json, options);

                if (raw == null || string.IsNullOrWhiteSpace(raw.Rating))
                {
                    _logger.LogWarning("Gemini returned invalid or empty recommendation structure.");
                    return null;
                }

                
                string finalRating = _allowedRatings.FirstOrDefault(r => 
                    r.Equals(raw.Rating.Trim(), StringComparison.OrdinalIgnoreCase)) ?? "Neutral";

                return new AiRecommendationResponseDto
                {
                    Symbol = symbol.ToUpper(),
                    CompanyName = companyName,
                    Rating = finalRating,
                    Summary = raw.Summary,
                    ModelUsed = _geminiSettings.Model,
                    GeneratedAt = DateTime.UtcNow
                };
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse Gemini JSON output: {Json}", json);
                return null;
            }
        }

        private class GeminiResponseRaw
        {
            public string Rating { get; set; } = string.Empty;
            public string Summary { get; set; } = string.Empty;
        }
    }
}
