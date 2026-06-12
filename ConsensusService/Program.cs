using ConsensusService;
using Microsoft.EntityFrameworkCore;
using Shared.Data;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.Configure<ConsensusOptions>(
    builder.Configuration.GetSection(ConsensusOptions.SectionName));
builder.Services.AddSingleton<ConsensusCalculator>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
