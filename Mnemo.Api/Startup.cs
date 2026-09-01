using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Mnemo.Data;
using Mnemo.Data.Queries;
using Mnemo.Services.AccountService;
using Mnemo.Services.EnrichmentService;
using Mnemo.Services.EnrichmentService.ExternalDictionaries;
using Mnemo.Services.PackService;
using Mnemo.Services.RepetitionService;
using Mnemo.Services.RepetitionService.Factories;
using Mnemo.Services.RepetitionService.Providers.DistractorProviders;
using Mnemo.Services.RepetitionService.Providers.TaskTypeProviders;
using Mnemo.Services.RepetitionService.Strategies;
using Mnemo.Services.VocabularyService;
using System.Text;

namespace Mnemo
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }
        public IConfigurationSection JwtSettings => Configuration.GetSection("Jwt");


        public void ConfigureServices(IServiceCollection services)
        {
            // Configurations
            services.Configure<EnrichmentOptions>(Configuration.GetSection("EnrichmentOptions"));
            services.Configure<RepetitionOptions>(Configuration.GetSection("RepetitionOptions"));
            services.Configure<SM2Options>(Configuration.GetSection("SM2Options"));

            // Add MemoryCache
            services.AddMemoryCache();

            // Add SQLite AppDbContext
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(Configuration.GetConnectionString("DefaultConnection")));

            // Add Validators
            services.AddValidatorsFromAssemblyContaining<Program>();

            // Add AutoMapper
            services.AddAutoMapper(typeof(Program));

            // Add Swagger
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Mnemo" });


                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme.",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
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
                        new string[] { }
                    }
                });
            });

            // JWT Authentication & Authorization services
            byte[] key = Encoding.UTF8.GetBytes(JwtSettings["Key"]);

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = JwtSettings["Issuer"],
                    ValidAudience = JwtSettings["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(key)
                };
            });

            services.AddAuthorization();

            // DI Queries
            services.AddScoped<AccountQueries>();
            services.AddScoped<TaskQueries>();
            services.AddScoped<StateQueries>();
            services.AddScoped<VocabularyQueries>();
            services.AddScoped<PackQueries>();

            // DI Services
            services.AddScoped<AccountManagementService>();
            services.AddScoped<RepetitionTaskService>();
            services.AddScoped<StateManagementService>();
            services.AddScoped<QualityCalculationService>();
            services.AddScoped<VocabularyManagementService>();
            services.AddScoped<PackManagementService>();

            // DI Enrichment
            services.AddHttpClient<IExternalDictionary, FreeDictionaryApi>();
            services.AddHostedService<EnrichmentBackgroundService>();

            // DI Distractor Providers
            services.AddScoped<AntonymDistractorProvider>();
            services.AddScoped<PrefixDistractorProvider>();
            services.AddScoped<ByPartOfSpeechDistractorProvider>();
            services.AddScoped<RandomDistractorProvider>();
            services.AddScoped<SyllableDistractorProvider>();
            services.AddScoped<IDistractorProvider, CompositeDistractorProvider>();

            // DI Task Factory
            services.AddScoped<ITaskTypeProvider, WeightTaskTypeProvider>();
            services.AddScoped<RepetitionTaskFactory>();

            // DI Task Strategies
            services.AddScoped<FastRepetitionTaskStrategy>();
            services.AddScoped<PlannedRepetitionTaskStrategy>();

            // Use Controllers
            services.AddControllers();
        }

        public void Configure(WebApplication app, IWebHostEnvironment env)
        {
            using var scope = app.Services.CreateScope();
            scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();


            if (env.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();
        }
    }
}
