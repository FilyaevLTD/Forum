using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using ForumProject.Services;
using ForumProject.Models.Context;
using ForumProject.Models;
using ForumProject.Models.EmailConfig;

namespace ForumProject
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
            string connection_str_WorkDB = Configuration.GetValue<string>("WorkDB:ConnectionString");
            services.AddDbContext<ContextServiceDB>(option => option.UseLazyLoadingProxies().UseSqlServer(connection_str_WorkDB));
            services.Configure<AppEmailSettings>(Configuration.GetSection("EmailConfig"));
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddAuthentication(x => {
                x.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
                .AddCookie(options => // конфигурации cookie аутентификации
                {
                    options.LoginPath = new Microsoft.AspNetCore.Http.PathString("/Authorize/Login");
                });

            //
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            //

            services.AddTransient<IAuthorizeService, AuthorizeService>();
            services.AddTransient<IForumServices, ForumServices>();
            services.AddTransient<IUsersService, UsersService>();
            services.AddTransient<IEmailService, EmailService>();
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ContextServiceDB serviceDB, IAuthorizeService authorizeService)
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
            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseAuthentication();
            app.UseCookiePolicy();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });

            //
            SeedData.InitialiseUsersAndRoles(serviceDB, authorizeService);
        }
    }
}
