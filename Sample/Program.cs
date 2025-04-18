﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pipeline.Sample.App;
using Pipeline.Sample.App.Models;
using Pipeline.Lib;
using Pipeline.Lib.Abstraction;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var services = new ServiceCollection();
        services.AddPipelines(typeof(SamplePipelineDefinition).Assembly);
        services.AddLogging(logBuilder =>
        {
            logBuilder.AddConsole();
            logBuilder.AddDebug();
        });

        var serviceProvider = services.BuildServiceProvider();
        using var asyncScope = serviceProvider.CreateAsyncScope();
        var pipeline = asyncScope.ServiceProvider.GetRequiredService<IPipeline<Request, Response>>();
        var response = await pipeline.HandleAsync(new Request 
        { 
            Id = 123,
            Name = "Hello world"
        }, default);

        Console.WriteLine(response?.Message ??  $"Конвейер обработки {pipeline.GetType().FullName} не вернул результат.");
    }
}