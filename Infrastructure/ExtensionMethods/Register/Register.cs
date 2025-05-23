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
using Infrastructure.BackgroundTasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Infrastructure.ExtensionMethods.Register;

public static class Register
{

    public static void AddRegisterService(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<DataContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"), 
                npgsqlOptions => 
                {
                    // Настройка для работы с датами в UTC
                    npgsqlOptions.UseNodaTime();
                }));
        services.AddScoped<IHashService, HashService>();
    
        services.AddScoped<IEmailService>(sp => 
            new EmailService(
                sp.GetRequiredService<EmailConfiguration>(),
                sp.GetRequiredService<IConfiguration>()
            ));
        services.AddScoped<IAttendanceService, AttendanceService>();
        services.AddScoped<ICommentService, CommentService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ILessonService, LessonService>();
        services.AddScoped<IGradeService, GradeService>();
        services.AddScoped<IExamService, ExamService>();
        services.AddScoped<INotificationService, NotificationService>();
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
                uploadPath
            ));

        
        services.AddScoped<ICenterService>(cs => 
            new CenterService(
                cs.GetRequiredService<DataContext>(),
                uploadPath
            ));
            
        services.AddScoped<IGroupService>(gs => 
            new GroupService(
                gs.GetRequiredService<DataContext>(),
                uploadPath
            ));
            
        services.AddScoped<IGroupActivationService, GroupActivationService>();
            
        services.AddScoped<IStudentGroupService, StudentGroupService>();
        
        services.AddScoped<IMentorGroupService, MentorGroupService>();
   
        services.AddHostedService<DailyLessonCreatorService>();
        // services.AddHostedService<WeeklyExamCreatorService>();
        services.AddHostedService<GroupExpirationService>();
        services.AddHostedService<StudentStatusUpdaterService>();
        services.AddHostedService<CenterIncomeUpdaterService>();
   
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
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Description = "Введите JWT через Bearer",
                Name = "Authorization",
                Type = SecuritySchemeType.ApiKey
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
                    new List<string>()
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
                    
                // Enable CORS Preflight caching
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
        // var logger = services.GetRequiredService<ILogger<Register>>();
        
        try
        {
            var context = services.GetRequiredService<DataContext>();
            var configuration = services.GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            
            // logger.LogInformation("Проверка соединения с базой данных и создание базы при необходимости");
            
            // Создаем новое подключение только для создания базы данных
            var builder = new NpgsqlConnectionStringBuilder(connectionString);
            var databaseName = builder.Database;
            
            // Временно удаляем имя базы данных из строки подключения для подключения к серверу
            builder.Database = "postgres";
            var masterConnectionString = builder.ConnectionString;
            
            // Проверяем существование базы данных и создаем её при необходимости
            using (var connection = new NpgsqlConnection(masterConnectionString))
            {
                await connection.OpenAsync();
                
                // Проверяем существование базы данных
                var checkDbCommand = new NpgsqlCommand($"SELECT 1 FROM pg_database WHERE datname = '{databaseName}'", connection);
                var exists = await checkDbCommand.ExecuteScalarAsync() != null;
                
                if (!exists)
                {
                    // logger.LogInformation($"База данных {databaseName} не найдена. Создание базы данных...");
                    var createDbCommand = new NpgsqlCommand($"CREATE DATABASE \"{databaseName}\" WITH OWNER = postgres ENCODING = 'UTF8' CONNECTION LIMIT = -1;", connection);
                    await createDbCommand.ExecuteNonQueryAsync();
                    // logger.LogInformation($"База данных {databaseName} успешно создана");
                }
                else
                {
                    // logger.LogInformation($"База данных {databaseName} уже существует");
                }
            }
            
            // Теперь применяем миграции к созданной/существующей базе данных
            // logger.LogInformation("Применение миграций...");
            await context.Database.MigrateAsync();
            // logger.LogInformation("Миграции успешно применены");
            
            // Заполняем начальными данными
            // logger.LogInformation("Заполнение базы начальными данными...");
            var seedService = services.GetRequiredService<SeedData>();
            await seedService.SeedAllData();
            // logger.LogInformation("Начальные данные успешно добавлены");
        }
        catch (Exception ex)
        {
            // logger.LogError(ex, "Ошибка при настройке базы данных: {Message}", ex.Message);
        }
    }
    
    
    public static void UseStaticFilesConfiguration(this IApplicationBuilder app, string webRootPath)
    {
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(
                Path.Combine(Directory.GetCurrentDirectory(), webRootPath)),
            RequestPath = ""
        });
        
        
        app.UseStaticFiles(new StaticFileOptions()
        {
            FileProvider = new PhysicalFileProvider(
                Path.Combine(Directory.GetCurrentDirectory(), webRootPath, "uploads")),
            RequestPath = "/uploads"
        });
    }
}