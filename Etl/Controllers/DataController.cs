// /Controllers/DataController.cs
using Microsoft.AspNetCore.Mvc;
using Etl.Data;
using Etl.Services;

namespace Etl.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DataController : ControllerBase
    {
        private readonly  DbConnectionParams _connectionParams;

        public DataController(DbConnectionParams connectionParams)
        {
            _connectionParams = connectionParams;
        }

        [HttpPost]
        public IActionResult GetForest([FromBody] DbConnectionParams connectionParams)
        {
            // Используем using для автоматического завершения подключения
            using (var dbContext = new DbContext(connectionParams))
            {
                var service = new DataRetrievalService(connectionParams);
                var forest = service.BuildForest();

                return Ok(forest);
            }
        }
    }
}