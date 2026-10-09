
using Microsoft.EntityFrameworkCore;
using PaytmSeatReservation.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Request ID and structured request logging
app.Use(async (context, next) =>
{
    var requestId = context.Request.Headers["X-Request-Id"].FirstOrDefault()
                    ?? Guid.NewGuid().ToString();

    context.Response.Headers["X-Request-Id"] = requestId;

    using (app.Logger.BeginScope(new Dictionary<string, object>
    {
        ["request_id"] = requestId
    }))
    {
        app.Logger.LogInformation(
            "Request started: {Method} {Path}",
            context.Request.Method,
            context.Request.Path);

        await next();

        app.Logger.LogInformation(
            "Request completed: {StatusCode}",
            context.Response.StatusCode);
    }
});

// Swagger
app.UseSwagger();
app.UseSwaggerUI();

// Health check
app.MapGet("/health", () =>
{
    return Results.Ok(new { status = "healthy" });
});

// Readiness check - verifies database connectivity
app.MapGet("/ready", async (AppDbContext db) =>
{
    try
    {
        var canConnect = await db.Database.CanConnectAsync();

        if (canConnect)
        {
            return Results.Ok(new
            {
                status = "ready",
                database = "available"
            });
        }

        return Results.Json(
            new
            {
                status = "not_ready",
                database = "unavailable"
            },
            statusCode: 503);
    }
    catch
    {
        return Results.Json(
            new
            {
                status = "not_ready",
                database = "unavailable"
            },
            statusCode: 503);
    }
});

// Existing controller routes
app.MapControllers();

app.Run();

