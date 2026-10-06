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

// Swagger enabled for Docker also
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.MapControllers();

app.Run();