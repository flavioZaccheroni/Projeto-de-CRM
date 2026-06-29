var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/", () => Results.Ok(new
{
    name = "CRM Autopecas e Servicos API",
    status = "online",
    version = "0.1.0"
}))
.WithName("ApiInfo");

app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    checkedAt = DateTimeOffset.UtcNow
}))
.WithName("HealthCheck");

app.Run();
