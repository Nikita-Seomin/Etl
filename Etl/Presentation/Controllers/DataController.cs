using Etl.Application.Queries;
using Etl.Data;
using Etl.DataStructures.Forest;
using Etl.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Wolverine;

[ApiController]
[Route("xml")]
public class ForestController : ControllerBase
{
    private readonly IMessageBus _bus;

    public ForestController(IMessageBus bus)
    {
        _bus = bus;
    }

    [HttpPost("structure_build")]
    public async Task<IActionResult> BuildForest([FromBody] DbConnectionParams connectionParams)
    {
        var query = new BuildForestCommand(connectionParams);
        var forest = await _bus.InvokeAsync<Forest<int, MappingRecord>>(query);
        return Ok(forest);
    }
}