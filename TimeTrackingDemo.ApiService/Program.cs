using JasperFx;
using JasperFx.Events;
using JasperFx.Events.Projections;
using Marten;
using Scalar.AspNetCore;
using TimeTrackingDemo.ApiService.Features;
using TimeTrackingDemo.ApiService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();
builder.Services.AddNpgsqlDataSource(builder.Configuration.GetConnectionString("marten")!);

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddMarten(config =>
    {
        config.AutoCreateSchemaObjects = AutoCreate.All;
        config.Events.StreamIdentity = StreamIdentity.AsString;
        config.Projections.Add<MonthlyHoursProjection>(ProjectionLifecycle.Inline);
    })
    .UseLightweightSessions()
    .UseNpgsqlDataSource();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapDefaultEndpoints();
ShiftEndpoints.Map(app);

app.Run();
