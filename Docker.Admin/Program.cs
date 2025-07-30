using Microsoft.OpenApi.Models;
using System.Reflection;
using Docker.Admin.Filter;
using GameFrameX.Foundation.Logger;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);
var logger = LogHandler.Create(LogOptions.Default);

builder.Host.UseSerilog(logger);

// 添加 MVC 服务
builder.Services.AddControllers(option => { FilterApp.Init(option.Filters); });

// 只在开发环境中添加 Swagger 服务
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));

        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Version = "v1",
            Title = "Docker Manager API",
            Description = "一个用于管理Docker容器的ASP.NET Core Web API",
            Contact = new OpenApiContact
            {
                Name = "AlianBlank",
                Email = "wangfj11@foxmail.com",
            }
        });
    });
}

builder.Services.AddSingleton<DockerService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 添加路由中间件
app.UseRouting();
app.MapControllers();

app.Run();