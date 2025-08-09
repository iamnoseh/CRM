using Domain.DTOs.EmailDTOs;
using Domain.Entities;
using Infrastructure.Data;
using Infrastructure.Interfaces;
using Infrastructure.Seed;
using Infrastructure.Services;
using Infrastructure.Services.EmailService;
using Infrastructure.Services.HashService;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.ExtensionMethods.Register;

public static class Register
{

    public static void AddRegisterService(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<DataContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"), 
                npgsqlOptions => 
                {
                    npgsqlOptions.UseNodaTime();
                }));
        services.AddScoped<IHashService, HashService>();
    
        services.AddScoped<IEmailService>(sp => 
            new EmailService(
                sp.GetRequiredService<EmailConfiguration>(),
                sp.GetRequiredService<IConfiguration>()
            ));
        services.AddScoped<ICenterService>(sp =>
            new CenterService(
                sp.GetRequiredService<DataContext>(),
                sp.GetRequiredService<IConfiguration>()["UploadPath"] ,sp.GetRequiredService<IHttpContextAccessor>()?? throw new InvalidOperationException("UploadPath not configured")
            ));      
    }
    

    public static void AddIdentityServices(this IServiceCollection services, IConfiguration configuration)
    {
    
        services.AddIdentity<User, IdentityRole<int>>()
            .AddEntityFrameworkStores<DataContext>()
            .AddDefaultTokenProviders();
      
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = configuration["Jwt:Issuer"],
                ValidAudience = configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"] 
                                       ?? throw new InvalidOperationException("JWT Key is not configured")))
            };
        });
    }
    
   
    public static void AddApplicationServices(this IServiceCollection services, IConfiguration configuration, string uploadPath)
    {
        
        services.AddScoped<IAccountService>(sp =>
            new AccountService(
                sp.GetRequiredService<UserManager<User>>(),
                sp.GetRequiredService<RoleManager<IdentityRole<int>>>(),
                sp.GetRequiredService<IConfiguration>(),
                sp.GetRequiredService<DataContext>(),
                sp.GetRequiredService<IEmailService>(),
                sp.GetRequiredService<IHashService>(),
                uploadPath
            ));

        services.AddScoped<IEmployeeService>(sp =>
            new EmployeeService(
                sp.GetRequiredService<DataContext>(),
                sp.GetRequiredService<UserManager<User>>(),
                uploadPath,
                sp.GetRequiredService<IEmailService>(),
                sp.GetRequiredService<IHttpContextAccessor>()));
        services.AddScoped<IStudentService>(st =>
            new StudentService(
                st.GetRequiredService<DataContext>(),
                st.GetRequiredService<IHttpContextAccessor>(),
                st.GetRequiredService<UserManager<User>>(),
                uploadPath,
                st.GetRequiredService<IEmailService>()
            ));
            
        services.AddScoped<IMentorService>(st => 
            new MentorService(
                st.GetRequiredService<DataContext>(),
                st.GetRequiredService<UserManager<User>>(),
                uploadPath,
                st.GetRequiredService<IEmailService>(),
                st.GetRequiredService<IHttpContextAccessor>()
            ));
        
        services.AddScoped<ICourseService>(us => 
            new CourseService(
                us.GetRequiredService<DataContext>(),
                uploadPath,
                us.GetRequiredService<IHttpContextAccessor>()
            ));

        
        services.AddScoped<ICenterService>(cs => 
            new CenterService(
                cs.GetRequiredService<DataContext>(),
                uploadPath,
                cs.GetRequiredService<IHttpContextAccessor>()
            ));
        
            
      
        services.AddScoped<IGroupActivationService>(sp => 
            new GroupActivationService(
                sp.GetRequiredService<DataContext>(),
                sp.GetRequiredService<IHttpContextAccessor>()
            ));
            
        services.AddScoped<IStudentGroupService, StudentGroupService>();
        
        services.AddScoped<IMentorGroupService, MentorGroupService>();       
        
        services.AddScoped<IClassroomService>(cs => 
            new ClassroomService(
                cs.GetRequiredService<DataContext>(),
                cs.GetRequiredService<IHttpContextAccessor>()
            ));
        
        services.AddScoped<IScheduleService, ScheduleService>();
        services.AddScoped<IJournalService, JournalService>();
        
        services.AddScoped<IGroupService>(gs => 
            new GroupService(
                gs.GetRequiredService<DataContext>(),
                uploadPath,
                gs.GetRequiredService<IHttpContextAccessor>()
            ));
        
        
        
        services.AddLogging(logging =>
        {
            logging.AddConsole();
            logging.AddDebug();
        });
        
       
        services.AddScoped<SeedData>();
       
    }
    
    public static void AddSwaggerServices(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Kavsar Academy",
                Version = "v1",
                Description = "API барои идораи системаи таълимии Kavsar Academy",
                Contact = new OpenApiContact
                {
                    Name = "Kavsar Academy Support",
                    Email = "info@kavsaracademy.com"
                }
            });
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
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
                    Array.Empty<string>()
                }
            });
        });
    }
        public static void AddCorsServices(this IServiceCollection services)
    {
        var defaultOrigins = new[]
        {
            "http://localhost:5173",
            "http://localhost:5174",
            "http://localhost:5064/",
            "http://localhost:5032/"
        };
        
        services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", policy =>
            {
                policy.WithOrigins(defaultOrigins)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
                policy.SetPreflightMaxAge(TimeSpan.FromMinutes(10));
            });
        });
    }
    
   
    

    
    [Obsolete("Obsolete")]
    public static void AddHangfireServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHangfire(config =>
        {
            config.UsePostgreSqlStorage(configuration.GetConnectionString("DefaultConnection"));
        });
        services.AddHangfireServer();
    }
    
    public static void ConfigureHangfireJobs(this IApplicationBuilder app)
    {
        RecurringJob.AddOrUpdate("daily-lesson-creation", 
            () => System.Console.WriteLine("Daily Lesson Creator Service is working automatically as a BackgroundService"),
            "1 0 * * 1-5", TimeZoneInfo.Local); // 00:01 с пн по пт
        
        
        RecurringJob.AddOrUpdate("group-expiration-check", 
            () => System.Console.WriteLine("Group Expiration Service is working automatically as a BackgroundService"),
            "7 0 * * *", TimeZoneInfo.Local); // 00:07 ежедневно
        RecurringJob.AddOrUpdate("student-status-update", 
            () => System.Console.WriteLine("Student Status Updater Service is working automatically as a BackgroundService"),
            "10 0 * * *", TimeZoneInfo.Local); // 00:10 ежедневно
    }
    
    public static async Task ApplyMigrationsAndSeedData(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var services = scope.ServiceProvider;
        
        try
        {
            var context = services.GetRequiredService<DataContext>();
            var configuration = services.GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            var builder = new NpgsqlConnectionStringBuilder(connectionString);
            var databaseName = builder.Database;
            builder.Database = "postgres";
            var masterConnectionString = builder.ConnectionString;
            using (var connection = new NpgsqlConnection(masterConnectionString))
            {
                await connection.OpenAsync();
                var checkDbCommand = new NpgsqlCommand($"SELECT 1 FROM pg_database WHERE datname = '{databaseName}'", connection);
                var exists = await checkDbCommand.ExecuteScalarAsync() != null;
                
                if (!exists)
                {
                    var createDbCommand = new NpgsqlCommand($"CREATE DATABASE \"{databaseName}\" WITH OWNER = postgres ENCODING = 'UTF8' CONNECTION LIMIT = -1;", connection);
                    await createDbCommand.ExecuteNonQueryAsync();
                }
                else
                {
                    // logger.LogInformation($"База данных {databaseName} уже существует");
                }
            }
            
            await context.Database.MigrateAsync();
            var seedService = services.GetRequiredService<SeedData>();
            await seedService.SeedAllData();
        }
        catch (Exception ex)
        {
            // 
        }
    }
    
    
    public static void UseStaticFilesConfiguration(this IApplicationBuilder app, string webRootPath)
    {
        var basePath = Path.Combine(Directory.GetCurrentDirectory(), webRootPath);
        if (!Directory.Exists(basePath))
        {
            Directory.CreateDirectory(basePath);
        }

        var uploadsPath = Path.Combine(basePath, "uploads");
        if (!Directory.Exists(uploadsPath))
        {
            Directory.CreateDirectory(uploadsPath);
        }

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(basePath),
            RequestPath = ""
        });

        app.UseStaticFiles(new StaticFileOptions()
        {
            FileProvider = new PhysicalFileProvider(uploadsPath),
            RequestPath = "/uploads"
        });
    }
}