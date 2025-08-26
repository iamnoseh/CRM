using Infrastructure.ExtensionMethods.Register;
using SwaggerThemes;
using Domain.DTOs.EmailDTOs;
using Infrastructure.BackgroundTasks;
using Hangfire;
using Hangfire.PostgreSql;
using Infrastructure.Interfaces;

var builder = WebApplication.CreateBuilder(args);
var uploadPath = builder.Configuration.GetValue<string>("UploadPath") ?? "wwwroot";
builder.Services.AddRegisterService(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddIdentityServices(builder.Configuration);
builder.Services.AddCorsServices();

var emailConfig = builder.Configuration.GetSection("EmailConfiguration").Get<EmailConfiguration>();
builder.Services.AddSingleton(emailConfig);

// Add Hangfire services
builder.Services.AddHangfire(configuration => configuration
    .UsePostgreSqlStorage(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHangfireServer();

builder.Services.AddApplicationServices(builder.Configuration, uploadPath);
builder.Services.AddSwaggerServices();
builder.Services.AddBackgroundServices();
builder.Services.AddControllers();
var app = builder.Build();
await app.ApplyMigrationsAndSeedData();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    DashboardTitle = "Kavsar Academy - Background Jobs",
    StatsPollingInterval = 5000,
    AppPath = "/swagger"
});

// Start Hangfire background tasks
using (var scope = app.Services.CreateScope())
{
    var hangfireTaskService = scope.ServiceProvider.GetRequiredService<Infrastructure.Services.HangfireBackgroundTaskService>();
    hangfireTaskService.StartAllBackgroundTasks();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {  
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Kavsar Academy v1");
        c.AddThemes(app);  
    });
}

app.UseStaticFilesConfiguration(uploadPath);
app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseRouting();
app.UseAuthentication(); 
app.UseAuthorization();  
app.MapControllers(); 

app.Run();
