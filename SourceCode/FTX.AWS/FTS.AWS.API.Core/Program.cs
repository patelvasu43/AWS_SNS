using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FTS.AWS.API.Core
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                })
              .ConfigureAppConfiguration((hostContext, builder) =>
              {
                  builder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

                  // Added before AddUserSecrets to let user secrets override
                  // environment variables.
                  builder.AddEnvironmentVariables();

                  if (hostContext.HostingEnvironment.IsDevelopment())
                  {
                      builder.AddUserSecrets<Program>();
                  }
              });

    }
}
