using AccountingBook.Interfaces;
using AccountingBook.Repository;
using AccountingBook.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using AccountingBook.Services.Interfaces;

namespace AccountingBook
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
            services.AddLogging();
            services.AddSwaggerGen();
            services.AddHttpClient();
            services.AddScoped<IDbConnection>(c => new SqlConnection(Configuration.GetConnectionString("StockDatabase")));
            services.AddScoped<IUserStockRepository, UserStockRepository>();
            services.AddScoped<IUserStockService, UserStockService>();
            services.AddScoped<IStockRepository, StockRepository>();
            services.AddScoped<StockService>();
            services.AddScoped<UserRepository>(); 
            //services.AddScoped<UpdateClosingPriceService>();
            services.AddHostedService<UpdateClosingPriceService>();
            services.AddScoped<IPDFService, PDFService>();
            services.AddScoped<IGmailService, MailService>();
            services.AddControllers();
            services.AddSingleton<GoogleCredential>(provider =>
            {
                return GoogleCredential.FromFile(@"D:\ASP\AccountingBook\client_secret.json")
                    .CreateScoped(GmailService.Scope.GmailReadonly);
            });

            services.AddSingleton<IGmailService>(provider =>
            {
                // 您可以根據需要提供正確的憑證和 token 路徑
                var credentialsPath = @"D:\ASP\AccountingBook\client_secret.json";
                var tokenPath = @"D:\ASP\AccountingBook\token.json";
                return new MailService(credentialsPath, tokenPath);
            });



        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

          
            app.UseAuthorization();
        
            

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Your API V1");
            });



        }
    }
}
