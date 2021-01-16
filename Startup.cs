using AutoMapper;
using JWT_Auth.Microservice.Entities.Context;
using JWT.Auth.Extensions;
using JWT.Auth.Modules.Interafaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using JWT.Auth.Modules;
using Microsoft.EntityFrameworkCore;
using JwtWebTokenSerice.Modules;
using JWT_Auth.Microservice.Modules.Interafaces;
using Microsoft.Extensions.Options;
using JWT.Auth.Helpers;
using System.Text.Json;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using JWT_Auth.Microservice.Modules;
using FluentEmail.Core.Interfaces;
using FluentEmail.SendGrid;
using log4net;
using System.Reflection;
using System.IO;
using log4net.Config;

namespace JWT_Auth.Microservice
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
            AddConnectionStrings(ref services);
                        
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));

            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy",
                    builder => builder.AllowAnyOrigin()
                                      //.WithOrigins("http://localhost",
                                      //"https://localhost",
                                      //"http://localhost:4200")
                                      .AllowAnyMethod()
                                      .AllowAnyHeader()
                                      //.AllowCredentials()
                                      );
            });

            services.AddAutoMapper(typeof(Startup));

            services.AddFluentEmail(Configuration.GetSection("SendGrid:SenderDefaultEmail").Value)                    
                    .AddSendGridSender(Configuration.GetSection("SendGrid:ApiKey").Value);

            #region Authentication
            var appSettingsSection = Configuration.GetSection("AppSettings");
            services.Configure<AppSettings>(appSettingsSection);

            //configure jwt authorize
            var appSettings = appSettingsSection.Get<AppSettings>();
            var key = Encoding.UTF8.GetBytes(appSettings.Secret);

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(x =>
                    {
                        x.RequireHttpsMetadata = false;
                        x.SaveToken = true;
                        x.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuerSigningKey = true,
                            IssuerSigningKey = new SymmetricSecurityKey(key),
                            ValidateIssuer = true,
                            ValidateAudience = true,
                            ValidIssuer = appSettings.Issuer,
                            ValidAudience = appSettings.Audience
                        };
                    });
            #endregion

            services.AddControllers()
                    .AddNewtonsoftJson(options =>
                    {
                        options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                    });

            // configure DI for application services            
            services.AddScoped<IUserModule, UserModule>();
            services.AddScoped<IJwtTokenValidator, JwtTokenModule>();
            services.AddScoped<IJwtTokenModule, JwtTokenModule>();
            services.AddScoped<IUserEmailModule, UserEmailModule>();            
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.ConfigureExceptionHandler();

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();            
            app.UseAuthorization();

            // TODO replace from app settings
            app.UseCors("CorsPolicy");

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        private void AddConnectionStrings(ref IServiceCollection services)
        {
            services.AddDbContext<JWTAuthContext>(options =>
                    options.UseSqlServer(Configuration.GetConnectionString("JWTAuthDefaultConnection")));
        }
    }
}
