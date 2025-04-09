using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;
using Pipeline.Console.App;
using Pipeline.Console.App.Models;
using Pipeline.Console.App.Pipes;
using Pipeline.Lib;
using Pipeline.Lib.Abstraction;

var services = new ServiceCollection();
//services.AddPipelins(typeof(SamplePipelineDefinition).Assembly);
Expression createPipe = (IServiceProvider sp) => new ValidationPipe(ServiceCollectionExtensions.CreatePipeDependency<Request, Response>(typeof(GenerateDocuments), sp));
Expression constant = Expression.Constant(typeof(GenerateDocuments), typeof(Type));

Console.WriteLine("Hello, World!");
