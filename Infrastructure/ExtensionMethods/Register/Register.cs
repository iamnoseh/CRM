using Domain.DTOs.EmailDTOs;
using Domain.Entities;
using Infrastructure.BackgroundTasks;
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
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.ExtensionMethods.Register;

public static class Register
{
    // Базовая регистрация сервисов
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
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IAttendanceService, AttendanceService>();
        services.AddScoped<ICommentService, CommentService>();
    }
    
    // Регистрация identity и аутентификации
    public static void AddIdentityServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Конфигурация Identity
        services.AddIdentity<User, IdentityRole<int>>()
            .AddEntityFrameworkStores<DataContext>()
            .AddDefaultTokenProviders();
            
        // Аутентификация и JWT
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
                st.GetRequiredService<IHttpContextAccessor>(),
                st.GetRequiredService<UserManager<User>>(),
                uploadPath,
                st.GetRequiredService<IEmailService>()
            ));
            
        // services.AddScoped<IUserService>(us => 
        //     new UserService(
        //         us.GetRequiredService<DataContext>(),
        //         us.GetRequiredService<UserManager<User>>(),
        //         us.GetRequiredService<IHttpContextAccessor>()
        //     ));
        //     
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
            
        // services.AddScoped<IGroupService>(us => 
        //     new GroupService(
        //         us.GetRequiredService<DataContext>(),
        //         uploadPath
        //     ));
        //     
        // services.AddScoped<IStudentGroupService, StudentGroupService>();
        
        // Регистрация сервиса для планировщика уроков
        services.AddScoped<LessonSchedulerService>();
        services.AddLogging(logging =>
        {
            logging.AddConsole();
            logging.AddDebug();
        });
        
       
        services.AddScoped<SeedData>();
        
        // Конфигурация Email
        var emailConfig = configuration
            .GetSection("EmailConfiguration")
            .Get<EmailConfiguration>();
        services.AddSingleton(emailConfig!);
    }
    
    // Регистрация Swagger
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
        services.AddCors(options =>
        {
            options.AddPolicy("AllowBlazorClient",
                policyBuilder =>
                {
                    policyBuilder.WithOrigins("http://localhost:5005")
                        .AllowAnyMethod()
                        .AllowAnyHeader();
                });
        });
    }
    
    // Регистрация Hangfire
    public static void AddHangfireServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHangfire(config =>
        {
            config.UsePostgreSqlStorage(configuration.GetConnectionString("DefaultConnection"));
        });
        services.AddHangfireServer();
    }
    
    // Настройка задач для Hangfire
    // public static void ConfigureHangfireJobs(this IApplicationBuilder app)
    // {
    //     var scopeFactory = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>();
    //     
    //     // Для планировщика уроков
    //     RecurringJob.AddOrUpdate("check-scheduled-lessons",
    //         () => scopeFactory.CreateScope().ServiceProvider
    //             .GetRequiredService<LessonSchedulerService>().CheckAndCreateLessonsAsync(),
    //         Cron.Daily(6)); // проверка ежедневно в 6 утра
    // }
    
    // Применение миграций и заполнение начальными данными
    public static async Task ApplyMigrationsAndSeedData(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var services = scope.ServiceProvider;
        
        try
        {
            var context = services.GetRequiredService<DataContext>();
            await context.Database.MigrateAsync();
            
            var seedService = services.GetRequiredService<SeedData>();
            await seedService.SeedRole();
            await seedService.SeedUser();

        }
        catch (Exception ex)
        {
            //
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