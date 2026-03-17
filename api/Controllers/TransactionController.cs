using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Data;
using api.Interfaces;
using api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RouteAttribute = Microsoft.AspNetCore.Mvc.RouteAttribute;
using Microsoft.EntityFrameworkCore;
using api.Extensions;
using api.Dtos.Transaction;
using api.Helpers;
using System.Text;


namespace api.Controllers
{
    [Route("api/transaction")]
    [ApiController]
    public class TransactionController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ITransactionRepository _transactionRepo;
        private readonly IStockRepository _stockRepo;
        private readonly ApplicationDBContext _context;
        public TransactionController(UserManager<AppUser> userManager, ITransactionRepository transactionRepo,
        IStockRepository stockRepo, ApplicationDBContext context)
        {
            _userManager = userManager;
            _transactionRepo = transactionRepo;
            _stockRepo = stockRepo;
            _context = context;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetUserTransactions([FromQuery] TransactionQueryObject query)
        {
            var username = User.GetUsername();
            var appUser = await _userManager.FindByNameAsync(username);

            if (appUser == null)
                return Unauthorized();

            var transactions = await _transactionRepo.GetUserTransactionsAsync(appUser, query);
            var transactionDtos = transactions.Select(t => new TransactionDto
            {
               Id = t.Id,
                StockId = t.StockId,
                Symbol = t.Stock.Sympol,
                CompanyName = t.Stock.CompanyName,
                Type = t.Type.ToString(),
                Quantity = t.Quantity,
                PricePerShare = t.PricePerShare,
                TotalAmount = t.TotalAmount,
                TransactionDate = t.TransactionDate,
                Category = t.Category.ToString(),
                Notes = t.Notes
            });

            return Ok(transactionDtos);
        }

        [HttpGet("{id:int}")]
        [Authorize]
        public async Task<IActionResult> GetById([FromRoute] int id)
        {
            var username = User.GetUsername();
            var appUser = await _userManager.FindByNameAsync(username);

            if (appUser == null)
                return Unauthorized();

            var transaction = await _transactionRepo.GetByIdAsync(id);

            if (transaction == null)
                return NotFound("Transaction not found");

            if (transaction.AppUserId != appUser.Id)
                return Unauthorized();

            var transactionDto = new TransactionDto
            {
                Id = transaction.Id,
                StockId = transaction.StockId,
                Symbol = transaction.Stock.Sympol,
                CompanyName = transaction.Stock.CompanyName,
                Type = transaction.Type.ToString(),
                Quantity = transaction.Quantity,
                PricePerShare = transaction.PricePerShare,
                TotalAmount = transaction.TotalAmount,
                TransactionDate = transaction.TransactionDate,
                Category = transaction.Category.ToString(),
                Notes = transaction.Notes
            };

            return Ok(transactionDto);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateTransaction([FromBody] CreateTransactionDto transactionDto)
        {
            var username = User.GetUsername();
            var appUser = await _userManager.FindByNameAsync(username);

            if (appUser == null)
                return Unauthorized();

            if (transactionDto.Quantity <= 0)
                return BadRequest("Quantity must be greater than 0");

            if (transactionDto.PricePerShare <= 0)
                return BadRequest("Price per share must be greater than 0");

            var stock = await _stockRepo.GetByIdAsync(transactionDto.StockId);

            if (stock == null)
                return NotFound("Stock not found");

            var portfolioItem = await _context.portfolios
                .FirstOrDefaultAsync(p => p.AppUserId == appUser.Id && p.StockId == transactionDto.StockId);

            if (transactionDto.Type == TransactionType.Sell)
            {
                if (portfolioItem == null || portfolioItem.Quantity < transactionDto.Quantity)
                    return BadRequest("You do not own enough shares to sell");
            }

            var transaction = new Transaction
            {
                AppUserId = appUser.Id,
                StockId = transactionDto.StockId,
                Type = transactionDto.Type,
                Quantity = transactionDto.Quantity,
                PricePerShare = transactionDto.PricePerShare,
                TotalAmount = transactionDto.Quantity * transactionDto.PricePerShare,
                TransactionDate = transactionDto.TransactionDate ?? DateTime.UtcNow,
                Category = transactionDto.Category,
                Notes = transactionDto.Notes
            };

            await _transactionRepo.CreateAsync(transaction);

            if (transactionDto.Type == TransactionType.Buy)
            {
                if (portfolioItem == null)
                {
                    portfolioItem = new Portfolio
                    {
                        AppUserId = appUser.Id,
                        StockId = transactionDto.StockId,
                        Quantity = transactionDto.Quantity
                    };

                    await _context.portfolios.AddAsync(portfolioItem);
                }
                else
                {
                    portfolioItem.Quantity += transactionDto.Quantity;
                }
            }
            else if (transactionDto.Type == TransactionType.Sell)
            {
                portfolioItem.Quantity -= transactionDto.Quantity;

                if (portfolioItem.Quantity == 0)
                {
                    _context.portfolios.Remove(portfolioItem);
                }
            }

            await _context.SaveChangesAsync();

            return Ok("Transaction created successfully");
        }

        [HttpGet("summary")]
        [Authorize]
        public async Task<IActionResult> GetSummary()
        {
            var username = User.GetUsername();
            var appUser = await _userManager.FindByNameAsync(username);

            if (appUser == null)
                return Unauthorized();

            var summary = await _transactionRepo.GetUserSummaryAsync(appUser);

            return Ok(summary);
        }

        [HttpGet("stock/{stockId:int}")]
        [Authorize]
        public async Task<IActionResult> GetTransactionsForStock([FromRoute] int stockId)
        {
            var username = User.GetUsername();
            var appUser = await _userManager.FindByNameAsync(username);

            if (appUser == null)
                return Unauthorized();

            var transactions = await _transactionRepo.GetUserTransactionsForStockAsync(appUser, stockId);

            var transactionDtos = transactions.Select(t => new TransactionDto
            {
                Id = t.Id,
                StockId = t.StockId,
                Symbol = t.Stock.Sympol,
                CompanyName = t.Stock.CompanyName,
                Type = t.Type.ToString(),
                Quantity = t.Quantity,
                PricePerShare = t.PricePerShare,
                TotalAmount = t.TotalAmount,
                TransactionDate = t.TransactionDate,
                Category = t.Category.ToString(),
                Notes = t.Notes
            });

            return Ok(transactionDtos);
        }

        [HttpGet("realized-gains")]
        [Authorize]
        public async Task<IActionResult> GetRealizedGainLoss()
        {
            var username = User.GetUsername();
            var appUser = await _userManager.FindByNameAsync(username);

            if (appUser == null)
                return Unauthorized();

            var result = await _transactionRepo.GetRealizedGainLossAsync(appUser);

            return Ok(result);
        }

        [HttpGet("export")]
        [Authorize]
        public async Task<IActionResult> ExportTransactionsToCsv([FromQuery] TransactionQueryObject query)
        {
            var username = User.GetUsername();
            var appUser = await _userManager.FindByNameAsync(username);

            if (appUser == null)
                return Unauthorized();

            var transactions = await _transactionRepo.GetAllUserTransactionsForExportAsync(appUser, query);

            var builder = new StringBuilder();
            builder.AppendLine("Id,StockId,Symbol,CompanyName,Type,Category,Quantity,PricePerShare,TotalAmount,TransactionDate,Notes");

            foreach (var t in transactions)
            {
                var notes = t.Notes?.Replace(",", " ") ?? "";
                builder.AppendLine(
                    $"{t.Id},{t.StockId},{t.Stock.Sympol},{t.Stock.CompanyName},{t.Type},{t.Category},{t.Quantity},{t.PricePerShare},{t.TotalAmount},{t.TransactionDate:yyyy-MM-dd HH:mm:ss},{notes}"
                );
            }

            var bytes = Encoding.UTF8.GetBytes(builder.ToString());

            return File(bytes, "text/csv", "transactions.csv");
        }

    }
}