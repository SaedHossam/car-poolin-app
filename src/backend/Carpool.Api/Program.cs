using Carpool.Api.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>((services, options) =>
    options.UseNpgsql(services.GetRequiredService<IConfiguration>().GetConnectionString("Database")));
builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>("database", tags: ["ready"]);

builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
{
    var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    if (origins.Length > 0)
    {
        policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
    }
}));

var app = builder.Build();
if (string.IsNullOrWhiteSpace(app.Configuration.GetConnectionString("Database")))
{
    throw new InvalidOperationException("Set ConnectionStrings__Database before starting the API.");
}
app.UseExceptionHandler();
app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Carpool API v1"));

    if (app.Configuration.GetValue<bool>("Database:ApplyMigrations"))
    {
        await using var scope = app.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
    }
}

app.MapGet("/api/health", () => Results.Ok(new { status = "Healthy" }))
    .WithName("GetHealth").WithTags("Health");
app.MapHealthChecks("/api/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.ToDictionary(entry => entry.Key, entry => entry.Value.Status.ToString())
        });
    }
});

app.Run();

public partial class Program;
