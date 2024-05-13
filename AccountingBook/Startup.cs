using AccountingBook.Interfaces;
using AccountingBook.Repository;
using AccountingBook.Repository.Interfaces;
using AccountingBook.Services;
using AccountingBook.Services.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Data;
using System.Data.SqlClient;

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
            services.AddTransient<IDbConnection>(c => new SqlConnection(Configuration.GetConnectionString("StockDatabase")));
            services.AddTransient<IUserStockRepository, UserStockRepository>();
            services.AddTransient<IUserStockService, UserStockService>();
            services.AddTransient<IStockRepository, StockRepository>();
            services.AddTransient<IStockTransactionsRepository, StockTransactionsRepository>();
            services.AddTransient<UserRepository>();
            services.AddTransient<StockTransactionsRepository>();
            services.AddHostedService<JobManagerService>();
            services.AddTransient<IPDFService, PDFService>();
            services.AddTransient<IGmailService, MailService>();
            services.AddTransient<IUpdateStockService, UpdateStockService>();

            services.AddControllers();
        }

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