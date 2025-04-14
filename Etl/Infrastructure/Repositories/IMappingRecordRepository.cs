using Etl.Models;
using System.Collections.Generic;
using Etl.Data;

namespace Etl.Infrastructure.Repositories
{
    public interface IMappingRecordRepository
    {
        IEnumerable<MappingRecord> GetMappingRecords(DbConnectionParams connectionParams);
    }
}