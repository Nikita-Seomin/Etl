using Etl.Data;
using Etl.Models;
using System.Collections.Generic;

namespace Etl.Infrastructure.Repositories
{
    public class MappingRecordRepository : IMappingRecordRepository
    {
        public IEnumerable<MappingRecord> GetMappingRecords(DbConnectionParams connectionParams)
        {
            using (var dbContext = new DbContext(connectionParams))
            {
                return dbContext.GetMappingRecords();
            }
        }
    }
}