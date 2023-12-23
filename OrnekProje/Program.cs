using OrnekProje.Models;
using OrnekProje.Service;
using Hangfire;
using HangfireBasicAuthenticationFilter;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using HealthChecks.UI.Client;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Hangfire.Logging;
using Serilog;
using Serilog.Events;
using k8s.KubeConfigModels;
using System.Reflection.Metadata.Ecma335;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();


//Log.Logger = new LoggerConfiguration()
//          .WriteTo.Seq("http://localhost:5341")  // Seq sunucu adresini buraya ekleyin
//          .MinimumLevel.Debug()
//          .CreateLogger();

//builder.Host.UseSerilog((context,loggerconfig)=>
//loggerconfig.ReadFrom.Configuration(context.Configuration));




builder.Services.AddControllers();

var connectionString = builder.Configuration.GetConnectionString("dbdeneme");
var connectionString2 = builder.Configuration.GetConnectionString("NorthwindContext");
//var connectionString3 = builder.Configuration.GetConnectionString("dbyeni");

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//builder.Services
//    .AddHealthChecks()
//    .AddSqlServer(connectionString3);

builder.Services
    .AddHealthChecks()
    .AddCheck<HealthCheckDeneme>("HealtCheckDeneme")
  
    .AddSeqPublisher(options =>
    {
        options.ApiKey = "AkOKVfQ5kCTw1S1lLEN7";
        options.Endpoint = "http://localhost:5341";
        options.DefaultInputLevel = HealthChecks.Publisher.Seq.SeqInputLevel.Information;
    }, "MS SQL Server Check")

    .AddSqlServer(
    connectionString: builder.Configuration.GetConnectionString("dbyeni")!,
    //healthQuery: "SELECT 1",
    name: "MS SQL Server Check",//sql server baðlantýsý saðlýk kontrolü adý
    failureStatus: HealthStatus.Unhealthy | HealthStatus.Degraded,
    tags: new string[] { "db", "sql", "sqlserver" });


builder.Services
    .AddHealthChecksUI()
    .AddSqlServerStorage(connectionString: builder.Configuration.GetConnectionString("dbyeni"));//servislerin durumlarýna dair verileri tutacaðým storage entegrasyonunu


builder.Services.AddDbContext<NorthwndContext>(options => options.UseSqlServer(connectionString2));

builder.Services.AddHangfire(configuration =>
{
    configuration
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseSqlServerStorage(connectionString);
});

builder.Services.AddHangfireServer();


builder.Services.AddTransient<IServiceManagement, ServiceManagement>();
builder.Host.UseSerilog();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseSerilogRequestLogging();//Apimize gelen http isteklerini günlüðe kaydetmemizi saðlar.
app.UseHttpsRedirection();

app.UseAuthorization();

app.UseHangfireDashboard("/hangfire", new DashboardOptions()
{
    DashboardTitle = "My Dashboard",
    Authorization = new[]
    {
        new HangfireCustomBasicAuthenticationFilter()
        {
            Pass = "deneme",
            User = "deneme"
        }
    }
});

app.MapHealthChecks("/healthcheck", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse//json formatýnda verileri getirmek için
    
});

//app.MapHealthChecks("/healthcheck");

//app.MapHealthChecksUI();
app.UseHealthChecksUI(options => options.UIPath = "/health-ui");

app.MapControllers();

app.Run();
