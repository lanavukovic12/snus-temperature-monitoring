using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Shared.Data;
using IngestionService.BackgroundServices;
using IngestionService.Options;
using IngestionService.Services;
using Shared.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<FaultToleranceOptions>(
    builder.Configuration.GetSection(FaultToleranceOptions.SectionName));
builder.Services.AddScoped<SensorRegistryService>();
builder.Services.AddScoped<AlarmDetector>();
builder.Services.Configure<NotificationOptions>(
    builder.Configuration.GetSection(NotificationOptions.SectionName));
builder.Services.Configure<SecureMessagingOptions>(
    builder.Configuration.GetSection(SecureMessagingOptions.SectionName));
builder.Services.AddHttpClient<AlarmNotifier>();
builder.Services.AddSingleton<SecureIngestGuard>();
builder.Services.AddHostedService<SensorWatchdog>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
}

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
