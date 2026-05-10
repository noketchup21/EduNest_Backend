using System;
using BusinessLayer.IServices;
using BusinessLayer.Mappings;
using BusinessLayer.Services;
using DataAccessLayer.Entities;
using DataAccessLayer.IRepositories;
using DataAccessLayer.Repositories;
using EduNest_Backend.Middleware.RateLimit;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<EduNestDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

builder.Services.AddMapster();
MapsterConfig.Configure();
builder.Services.AddRateLimiting(builder.Configuration);

#region ADDSCOPE
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();
#endregion

builder.Services.AddControllers();
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

app.UseRateLimiting();

app.MapControllers();

app.Run();
