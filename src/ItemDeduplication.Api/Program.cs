using System.Text.Json.Serialization;
using ItemDeduplication.Application;
using ItemDeduplication.Application.Common.Interfaces;
using ItemDeduplication.Infrastructure;
using ItemDeduplication.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services
    .AddApplication(builder.Configuration)
    .AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Dev bootstrap. In production use: dotnet ef migrations add Initial / database update.
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreatedAsync();

    try
    {
        await scope.ServiceProvider.GetRequiredService<IVectorStore>().EnsureCollectionAsync();
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Qdrant is not reachable yet. Semantic matching is disabled until it is.");
    }
}

app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.Run();
