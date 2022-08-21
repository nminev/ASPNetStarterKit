// Copyright © 2014-present Kriasoft, LLC. All rights reserved.
// This source code is licensed under the MIT license found in the
// LICENSE.txt file in the root directory of this source tree.

using System.IO;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Server.Models;

namespace Server
{
    public class Startup
    {
        // Load application settings from JSON file(s)
        // https://docs.asp.net/en/latest/fundamentals/configuration.html
        public Startup(IHostEnvironment env)
        {
            Configuration = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile($"appsettings.json", optional: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .Build();
        }

        public IConfiguration Configuration { get; set; }

        // Configure IoC container
        // https://docs.asp.net/en/latest/fundamentals/dependency-injection.html
        public void ConfigureServices(IServiceCollection services)
        {
            // https://docs.asp.net/en/latest/security/anti-request-forgery.html
            services.AddAntiforgery(options => options.HeaderName = "X-XSRF-TOKEN");

            // Register Entity Framework database context
            // https://docs.efproject.net/en/latest/platforms/aspnetcore/new-db.html
            services.AddDbContext<DatabaseContext>(options =>
            {
                options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"));
            });


            services.AddIdentity<User, Role>()
                .AddEntityFrameworkStores<DatabaseContext>()
                .AddDefaultTokenProviders();

            services.AddMvcCore()
                .AddAuthorization()
                .AddViews()
                .AddRazorViewEngine();
        }

        public void Configure(IApplicationBuilder app)
        {
            // Configure logging
            // https://docs.asp.net/en/latest/fundamentals/logging.html
            var builder = WebApplication.CreateBuilder();
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();

            // Serve static files
            // https://docs.asp.net/en/latest/fundamentals/static-files.html
            app.UseStaticFiles();

            // Enable external authentication provider(s)
            // https://docs.asp.net/en/latest/security/authentication/sociallogins.html
            builder.Services.AddAuthentication()
                .AddFacebook(facebookOptions =>
                {
                    facebookOptions.AppId = Configuration["Authentication:Facebook:AppId"];
                    facebookOptions.AppSecret = Configuration["Authentication:Facebook:AppSecret"];
                    facebookOptions.Scope.Add("email");
                    facebookOptions.Fields.Add("name");
                    facebookOptions.Fields.Add("email");
                    facebookOptions.SaveTokens = true;
                });

            // Configure ASP.NET MVC
            // https://docs.asp.net/en/latest/mvc/index.html
            app.UseRouting();
            app.UseEndpoints(endpoint =>
            {
                endpoint.MapControllerRoute(name:"default", pattern:"{controller=Home}/{action=Index}");
            });
        }

        public static void Main()
        {
            var cwd = Directory.GetCurrentDirectory();
            var web = Path.GetFileName(cwd) == "server" ? "../public" : "public";

            var host = new WebHostBuilder()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseWebRoot(web)
                .UseKestrel()
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}
