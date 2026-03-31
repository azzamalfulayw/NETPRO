using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using api.Extensions;
using api.Dtos.Transaction;
using api.Helpers;
using System.Text;
using MediatR;
using api.Features.Transaction.Queries;
using api.Features.Transaction.Commands;

namespace api.Controllers
{
    [Route("api/transaction")]
    [ApiController]
    public class TransactionController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IMediator _mediator;

        public TransactionController(UserManager<AppUser> userManager, IMediator mediator)
        {
            _userManager = userManager;
            _mediator = mediator;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetUserTransactions([FromQuery] TransactionQueryObject query)
        {
            var username = User.GetUsername();
            var appUser = await _userManager.FindByNameAsync(username);

            if (appUser == null) return Unauthorized();

            var transactions = await _mediator.Send(new GetUserTransactionsQuery { User = appUser, Query = query });
            var transactionDtos = transactions.Select(t => new TransactionDto
            {
                Id = t.Id,
                StockId = t.StockId,
                Symbol = t.Stock.Symbol,
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

            if (appUser == null) return Unauthorized();

            var transaction = await _mediator.Send(new GetTransactionByIdQuery { Id = id });

            if (transaction == null) return NotFound("Transaction not found");
            if (transaction.AppUserId != appUser.Id) return Unauthorized();

            var transactionDto = new TransactionDto
            {
                Id = transaction.Id,
                StockId = transaction.StockId,
                Symbol = transaction.Stock.Symbol,
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

            if (appUser == null) return Unauthorized();

            var result = await _mediator.Send(new CreateTransactionCommand { AppUser = appUser, TransactionDto = transactionDto });
            
            if (result.StartsWith("Error"))
            {
                if (result.Contains("not found")) return NotFound(result);
                return BadRequest(result);
            }

            return Ok("Transaction created successfully");
        }

        [HttpGet("summary")]
        [Authorize]
        public async Task<IActionResult> GetSummary()
        {
            var username = User.GetUsername();
            var appUser = await _userManager.FindByNameAsync(username);

            if (appUser == null) return Unauthorized();

            var summary = await _mediator.Send(new GetUserSummaryQuery { User = appUser });

            return Ok(summary);
        }

        [HttpGet("stock/{stockId:int}")]
        [Authorize]
        public async Task<IActionResult> GetTransactionsForStock([FromRoute] int stockId)
        {
            var username = User.GetUsername();
            var appUser = await _userManager.FindByNameAsync(username);

            if (appUser == null) return Unauthorized();

            var transactions = await _mediator.Send(new GetTransactionsForStockQuery { User = appUser, StockId = stockId });

            var transactionDtos = transactions.Select(t => new TransactionDto
            {
                Id = t.Id,
                StockId = t.StockId,
                Symbol = t.Stock.Symbol,
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

            if (appUser == null) return Unauthorized();

            var result = await _mediator.Send(new GetRealizedGainLossQuery { User = appUser });

            return Ok(result);
        }

        [HttpGet("export")]
        [Authorize]
        public async Task<IActionResult> ExportTransactionsToCsv([FromQuery] TransactionQueryObject query)
        {
            var username = User.GetUsername();
            var appUser = await _userManager.FindByNameAsync(username);

            if (appUser == null) return Unauthorized();

            var transactions = await _mediator.Send(new GetAllUserTransactionsForExportQuery { User = appUser, Query = query });

            var builder = new StringBuilder();
            builder.AppendLine("Id,StockId,Symbol,CompanyName,Type,Category,Quantity,PricePerShare,TotalAmount,TransactionDate,Notes");

            foreach (var t in transactions)
            {
                var notes = t.Notes?.Replace(",", " ") ?? "";
                builder.AppendLine(
                    $"{t.Id},{t.StockId},{t.Stock.Symbol},{t.Stock.CompanyName},{t.Type},{t.Category},{t.Quantity},{t.PricePerShare},{t.TotalAmount},{t.TransactionDate:yyyy-MM-dd HH:mm:ss},{notes}"
                );
            }

            var bytes = Encoding.UTF8.GetBytes(builder.ToString());

            return File(bytes, "text/csv", "transactions.csv");
        }
    }
}