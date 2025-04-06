using Microsoft.Extensions.DependencyInjection;
using Pipeline.Console.App;
using Pipeline.Lib;

var services = new ServiceCollection();
services.AddPipelins(typeof(SamplePipelineDefinition).Assembly);

Console.WriteLine("Hello, World!");
