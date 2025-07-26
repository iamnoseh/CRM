using Infrastructure.ExtensionMethods.Register;
using SwaggerThemes;
using Domain.DTOs.EmailDTOs;
using Hangfire;
using Infrastructure.BackgroundTasks;
using Infrastructure.Services.ExportToExel;

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
// builder.Services.AddBackgroundServices();
builder.Services.AddControllers();
// builder.Services.AddScoped<IStudentExportService, StudentExportService>();
// builder.Services.AddScoped<IMentorExportService, MentorExportService>();
var app = builder.Build();
await app.ApplyMigrationsAndSeedData();
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

app.UseHangfireDashboard();
app.UseHangfireServer();

app.ConfigureHangfireJobs(); 

app.Run();
