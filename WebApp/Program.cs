using Infrastructure.ExtensionMethods.Register;
using SwaggerThemes;
using Domain.DTOs.EmailDTOs;
using Hangfire;

var builder = WebApplication.CreateBuilder(args);

var uploadPath = builder.Configuration.GetValue<string>("UploadPath") ?? "wwwroot";

builder.Services.AddRegisterService(builder.Configuration);

builder.Services.AddHttpContextAccessor();

builder.Services.AddIdentityServices(builder.Configuration);

builder.Services.AddCorsServices();


var emailConfig = builder.Configuration.GetSection("EmailConfiguration").Get<EmailConfiguration>();
builder.Services.AddSingleton(emailConfig);


builder.Services.AddApplicationServices(builder.Configuration, uploadPath);


builder.Services.AddSwaggerServices();

builder.Services.AddHangfireServices(builder.Configuration);

builder.Services.AddControllers();

var app = builder.Build();

await app.ApplyMigrationsAndSeedData();

app.UseStaticFilesConfiguration(uploadPath);

app.UseCors("AllowFrontend");


app.UseSwagger();
app.UseSwaggerUI(c =>
{  
    c.AddThemes(app);  
});
app.UseHangfireDashboard("/hangfire");
app.UseHangfireServer();


app.UseRouting();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();


 app.ConfigureHangfireJobs(); 

app.Run();
