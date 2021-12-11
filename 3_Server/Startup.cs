using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lab
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration config)
        {
            Configuration = config;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddSwaggerDocument();
            services.AddSingleton<IRecognisionStorage>(new PersistentRecognisionStorage());
            services.AddSingleton<IRecogniser>(new RecogniserWrapper());
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            //app.UseAuthorization();

            app.UseOpenApi();
            app.UseSwaggerUi3();

            app.UseCors(b => b.AllowAnyOrigin().AllowAnyMethod());
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        } 
    }
}
