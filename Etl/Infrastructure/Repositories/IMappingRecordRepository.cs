using System.Collections.Generic;
using Etl.Data;
using Etl.Domain.Entities;

namespace Etl.Infrastructure.Repositories
{
    public interface IMappingRecordRepository
    {
        IEnumerable<MappingRecord> GetMappingRecords(DbConnectionParams connectionParams);
    }
}