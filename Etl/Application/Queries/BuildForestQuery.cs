
using Etl.Data;
using Etl.Models;

namespace Etl.Application.Queries
{
    public class BuildForestQuery
    {
        public DbConnectionParams ConnectionParams { get; }

        public BuildForestQuery(DbConnectionParams connectionParams)
        {
            ConnectionParams = connectionParams;
        }
    }
}