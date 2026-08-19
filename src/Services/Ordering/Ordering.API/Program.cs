using Ordering.API;
using Ordering.Application;
using Ordering.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

//------------------------------------------------
//Infrastructure Layer - EF Core
//Application Layer - MediatR
//API Layer - Carter, HealthChecks, ...

//builder.Services
//    .AddApplicationServices()
//    .AddInfrastructureServices(builder.Configuration)
//    .AddWebServices();
//------------------------------------------------

builder.Services
    .AddApplicationServices()
    .AddInfrastructureServices(builder.Configuration)
    .AddApiServices();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.Run();
