using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace CodeGenerator.Sample.Api
{
    public class Startup
    {
        public string AssemblyName { get;  }
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            AssemblyName = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            var authorizationAuthority = Configuration.GetValue<string>("Authorization:Authority");
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options =>
                    {
                        options.Authority = authorizationAuthority;
                        options.Audience = AssemblyName;
                    });

            services.AddAuthorization(options =>
            {
                options.AddPolicy("ApiScope", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim("scope", AssemblyName);
                });
                options.AddPolicy("ManagerPolicy", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireRole("Manager");
                });
                options.AddPolicy("ContributorPolicy", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireRole("Contributor");
                });
            });

            var allowedOrigins = Configuration.GetSection("Cors:AllowedOrigins").Get<List<string>>();
            services.AddCors(setup =>
            {
                setup.AddDefaultPolicy(policy =>
                {
                    policy.AllowAnyHeader();
                    policy.AllowAnyMethod();
                    policy.AllowCredentials();
                    policy.WithOrigins(allowedOrigins.ToArray());
                });
            });

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = AssemblyName, Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", AssemblyName));
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseCors();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers()
                         .RequireAuthorization("ApiScope");
            });
        }
    }
}
