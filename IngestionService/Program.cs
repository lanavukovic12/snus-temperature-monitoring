using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Shared.Data;
using IngestionService.BackgroundServices;
using IngestionService.Options;
using IngestionService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<FaultToleranceOptions>(
    builder.Configuration.GetSection(FaultToleranceOptions.SectionName));
builder.Services.AddScoped<SensorRegistryService>();
builder.Services.AddScoped<AlarmDetector>();
builder.Services.Configure<NotificationOptions>(
    builder.Configuration.GetSection(NotificationOptions.SectionName));
builder.Services.AddHttpClient<AlarmNotifier>();
builder.Services.AddHostedService<SensorWatchdog>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
