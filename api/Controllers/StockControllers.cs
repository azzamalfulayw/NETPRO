using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.Stock;
using api.Features.Stock.Queries;
using api.Features.Stock.Commands;
using api.Helpers;
using api.Interfaces;

using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MapsterMapper;
using api.Models;

namespace api.Controllers
{
    [Route("api/stock")]
    [ApiController]
    public class StockController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IStockDataService _stockDataService;
        private readonly IMapper _mapper;

        public StockController(IMediator mediator, IStockDataService stockDataService, IMapper mapper)
        {
            _mediator = mediator;
            _stockDataService = stockDataService;
            _mapper = mapper;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAll([FromQuery] QueryObject query)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var stocks = await _mediator.Send(new GetAllStocksQuery { Query = query });
            var stockDto = stocks.Select(s => _mapper.Map<StockDto>(s)).ToList();
            return Ok(stockDto);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById([FromRoute] int id)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var stock = await _mediator.Send(new GetStockByIdQuery { Id = id });
            if (stock == null) return NotFound();

            return Ok(_mapper.Map<StockDto>(stock));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateStockRequestDto stockDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var stockModel = _mapper.Map<Stock>(stockDto);
            await _mediator.Send(new CreateStockCommand { StockModel = stockModel });
            
            return CreatedAtAction(nameof(GetById), new { id = stockModel.Id }, _mapper.Map<StockDto>(stockModel));
        }

        [HttpPut]
        [Route("{id:int}")]
        public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateStockRequestDto updateDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var stockModel = await _mediator.Send(new UpdateStockCommand { Id = id, UpdateDto = updateDto });
            if (stockModel == null) return NotFound();

            return Ok(_mapper.Map<StockDto>(stockModel));
        }

        [HttpDelete]
        [Route("{id:int}")]
        public async Task<IActionResult> Delete([FromRoute] int id)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var stockModel = await _mediator.Send(new DeleteStockCommand { Id = id });
            if (stockModel == null) return NotFound();

            return NoContent();
        }

        [HttpGet("{symbol}/live-price")]
        public async Task<IActionResult> GetLivePrice([FromRoute] string symbol)
        {
            var result = await _stockDataService.GetCurrentPriceAsync(symbol);
            if (result == null) return NotFound("Live price not found");

            return Ok(result);
        }
    }
}