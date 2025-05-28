using Infrastructure.ExtensionMethods.Register;
using SwaggerThemes;
using Domain.DTOs.EmailDTOs;
using Hangfire;

var builder = WebApplication.CreateBuilder(args);

var uploadPath = builder.Configuration.GetValue<string>("UploadPath") ?? "wwwroot";

builder.Services.AddRegisterService(builder.Configuration);

builder.Services.AddHttpContextAccessor();

builder.Services.AddIdentityServices(builder.Configuration);

// Настройка конфигурации электронной почты
var emailConfig = builder.Configuration.GetSection("EmailConfiguration").Get<EmailConfiguration>();
builder.Services.AddSingleton(emailConfig);


builder.Services.AddApplicationServices(builder.Configuration, uploadPath);

builder.Services.AddCorsServices();

builder.Services.AddSwaggerServices();

builder.Services.AddHangfireServices(builder.Configuration);

builder.Services.AddControllers();

var app = builder.Build();

await app.ApplyMigrationsAndSeedData();

app.UseStaticFilesConfiguration(uploadPath);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {  
        c.AddThemes(app);  
    });
    
}
app.UseHangfireDashboard("/hangfire");
app.UseHangfireServer();

app.UseCors("AllowBlazorClient");
app.UseRouting();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Фоновые службы работают автоматически через механизм BackgroundService
 app.ConfigureHangfireJobs(); // отключено, чтобы избежать ошибок инициализации Hangfire

app.Run();
