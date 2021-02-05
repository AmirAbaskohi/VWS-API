using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SpaServices.AngularCli;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using vws.web.Extensions;
using vws.web.Hubs;
using vws.web.Repositories;
using vws.web.Domain;
using vws.web.Domain._base;
using vws.web.Enums;

namespace vws.web
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
            services.AddScoped<IVWS_DbContext, VWS_DbContext>();
            services.AddLocalization(options => { options.ResourcesPath = "Resources"; });
            services.AddSignalR();
            services.AddCors();
            services.Configure<RequestLocalizationOptions>(options =>
            {
                var supportedCultures = new List<CultureInfo>
                {
                    new CultureInfo("en-US"),
                    new CultureInfo("fa-IR")
                };

                options.DefaultRequestCulture = new RequestCulture(culture: "en-US", uiCulture: "en-US");
                options.SupportedCultures = supportedCultures;
                options.SupportedUICultures = supportedCultures;
                options.RequestCultureProviders = new[] { new vws.web.Extensions.RouteDataRequestCultureProvider { IndexOfCulture = 1, IndexofUICulture = 1 } };
            });

            services.Configure<RouteOptions>(options =>
            {
                options.ConstraintMap.Add("culture", typeof(LanguageRouteConstraint));
            });

            services.AddControllersWithViews();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1",
                    Title = "VWS API",
                    Description = "VWS ASP.NET Core Web API"
                });
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer 12345abcdef\"",
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                          new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = "Bearer"
                                }
                            },
                            new string[] {}

                    }
                });
            });

            services.AddDbContextPool<VWS_DbContext>(options =>
            {
                options.UseSqlServer(Configuration.GetConnectionString("SqlServer"));
            });

            services.AddIdentity<ApplicationUser, IdentityRole>()
                    .AddEntityFrameworkStores<VWS_DbContext>()
                    .AddDefaultTokenProviders();

            services.Configure<IdentityOptions>(opts =>
            {
                opts.Password.RequiredLength = Int16.Parse(Configuration["Security:PasswordLength"]);
                opts.Password.RequireNonAlphanumeric = Boolean.Parse(Configuration["Security:RequireNonAlphanumeric"]);
                opts.Password.RequireLowercase = Boolean.Parse(Configuration["Security:RequireLowercase"]);
                opts.Password.RequireUppercase = Boolean.Parse(Configuration["Security:RequireUppercase"]);
                opts.Password.RequireDigit = Boolean.Parse(Configuration["Security:RequireDigit"]);
            });

            services.AddScoped<IEmailSender, EmailSender>();
            
            services.AddScoped<IFileManager, FileManager>();

            services.AddAuthentication(option =>
            {
                option.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                option.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;

            }).AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.Zero,
                    ValidIssuer = Configuration["Jwt:Issuer"],
                    ValidAudience = Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["Jwt:Secret"]))
                };
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
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseCors(builder => builder.WithOrigins(Configuration["Angular:Url"])
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials());

            app.UseSwagger();

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "VWSAPI");
                c.RoutePrefix = string.Empty;
            });

            app.UseHttpsRedirection();

            app.UseStaticFiles();
           
            var localizeOptions = app.ApplicationServices.GetService<IOptions<RequestLocalizationOptions>>();

            app.UseRequestLocalization(localizeOptions.Value);

            app.UseRouting();

            app.UseAuthentication();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{culture:culture}/{controller}/{action=Index}/{id?}");
                endpoints.MapHub<ChatHub>("/chatHub");
            });


            // Automatically Create database and tables and do the migrations
            using (var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
            {
                var context = serviceScope.ServiceProvider.GetRequiredService<IVWS_DbContext>();
                context.DatabaseFacade.Migrate();

                //seed data:
                foreach (var messageType in Enum.GetValues(typeof(SeedDataEnum.MessageTypes)))
                {
                    string dbMessageType = context.GetMessageType((byte)messageType);
                    if (dbMessageType == null)
                        context.AddMessageType(new Domain._chat.MessageType { Id = (byte)messageType, Name = messageType.ToString() });
                    else if (dbMessageType != messageType.ToString())
                        context.UpdateMessageType((byte)messageType, messageType.ToString());
                }
                foreach (var teamType in Enum.GetValues(typeof(SeedDataEnum.TeamTypes)))
                {
                    string dbTeamType = context.GetTeamType((byte)teamType);
                    if (dbTeamType == null)
                        context.AddTeamType(new Domain._team.TeamType { Id = (byte)teamType, NameMultiLang = teamType.ToString() });
                    else if (dbTeamType != teamType.ToString())
                        context.UpdateTeamType((byte)teamType, teamType.ToString());
                }
                foreach (var teamType in Enum.GetValues(typeof(SeedDataEnum.TeamTypes)))
                {
                    string dbTeamType = context.GetTeamType((byte)teamType);
                    if (dbTeamType == null)
                        context.AddTeamType(new Domain._team.TeamType { Id = (byte)teamType, NameMultiLang = teamType.ToString() });
                    else if (dbTeamType != teamType.ToString())
                        context.UpdateTeamType((byte)teamType, teamType.ToString());
                }
                foreach (var channelType in Enum.GetValues(typeof(SeedDataEnum.ChannelTypes)))
                {
                    string dbChannelType = context.GetChannelType((byte)channelType);
                    if (dbChannelType == null)
                        context.AddChannelType(new Domain._chat.ChannelType { Id = (byte)channelType, Name = channelType.ToString() });
                    else if (dbChannelType != channelType.ToString())
                        context.UpdateChannelType((byte)channelType, channelType.ToString());
                }
                foreach (var status in Enum.GetValues(typeof(SeedDataEnum.ProjectStatuses)))
                {
                    string dbStatusType = context.GetStatus((byte)status);
                    if (dbStatusType == null)
                        context.AddStatus(new Domain._project.ProjectStatus { Id = (byte)status, NameMultiLang = status.ToString() });
                    else if (dbStatusType != status.ToString())
                        context.UpdateStatus((byte)status, status.ToString());
                }
                foreach (var culture in Enum.GetValues(typeof(SeedDataEnum.Cultures)))
                {
                    string dbCulture = context.GetCulture((byte)culture);
                    if (dbCulture == null)
                        context.AddCulture(new Domain._base.Culture { Id = (byte)culture, CultureAbbreviation = culture.ToString().Replace('_', '-') });
                    else if (dbCulture != culture.ToString())
                        context.UpdateCulture((byte)culture, culture.ToString().Replace('_','-'));
                }
                context.Save();
            }


        }
    }
}
