
using Etl.Data;

namespace Etl.Application.Queries
{
    public class BuildForestCommand
    {
        public DbConnectionParams ConnectionParams { get; }

        public BuildForestCommand(DbConnectionParams connectionParams)
        {
            ConnectionParams = connectionParams;
        }
    }
}