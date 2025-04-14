using Etl.Application.Services;
using Etl.Data;
using Etl.Infrastructure.Repositories;
using Wolverine;

namespace Etl
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddScoped<IMappingRecordRepository, MappingRecordRepository>();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}