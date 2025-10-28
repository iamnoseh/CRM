using Infrastructure.ExtensionMethods.Register;
using SwaggerThemes;
using Domain.DTOs.EmailDTOs;
using Infrastructure.BackgroundTasks;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.HttpOverrides;
using Serilog;

var builder = WebApplication.CreateBuilder(args);


builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext();
});

var uploadPath = builder.Configuration.GetValue<string>("UploadPath") ?? "wwwroot";
var migrationsEnabled = builder.Configuration.GetValue<bool>("Features:ApplyMigrationsOnStartup", true);
var enableSwagger = builder.Configuration.GetValue<bool>("Swagger:Enabled", false);

builder.Services.AddRegisterService(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddIdentityServices(builder.Configuration);
builder.Services.AddCorsServices();

var emailConfig = builder.Configuration.GetSection("EmailConfiguration").Get<EmailConfiguration>();
builder.Services.AddSingleton(emailConfig);


builder.Services.AddHangfire(cfg =>
    cfg.UsePostgreSqlStorage(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHangfireServer();


builder.Services.AddApplicationServices(builder.Configuration, uploadPath);
builder.Services.AddSwaggerServices();
builder.Services.AddBackgroundServices();
builder.Services.AddControllers();

var app = builder.Build();

var fwd = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor
                       | ForwardedHeaders.XForwardedProto
                       | ForwardedHeaders.XForwardedHost
};
fwd.KnownNetworks.Clear();
fwd.KnownProxies.Clear();
app.UseForwardedHeaders(fwd);

app.UseSerilogRequestLogging();

if (migrationsEnabled)
{
    await app.ApplyMigrationsAndSeedData();
}


app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    DashboardTitle = "Kavsar Academy - Background Jobs",
    StatsPollingInterval = 5000,
    AppPath = "/docs-secure"
});

using var scope = app.Services.CreateScope();
var hangfireTaskService =
    scope.ServiceProvider.GetRequiredService<Infrastructure.Services.HangfireBackgroundTaskService>();
hangfireTaskService.StartAllBackgroundTasks();


if (enableSwagger)
{
    app.UseSwagger(c =>
    {
        c.RouteTemplate = "docs-secure/{documentName}/swagger.json";
    });
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/docs-secure/v1/swagger.json", "Kavsar Academy v1");
        c.RoutePrefix = "docs-secure";
        c.AddThemes(app);
    });
}

app.UseStaticFilesConfiguration(uploadPath);
app.UseHttpsRedirection();

app.UseRouting();
app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<WebApp.Middleware.LogEnrichmentMiddleware>();

app.MapControllers();

app.Run();