using Etl.Data;
using System.Collections.Generic;
using Etl.Domain.Entities;

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