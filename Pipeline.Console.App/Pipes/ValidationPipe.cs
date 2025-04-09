using Microsoft.Extensions.Logging;
using Pipeline.Console.App.Models;
using Pipeline.Lib;
using Pipeline.Lib.Abstraction;

namespace Pipeline.Console.App.Pipes
{
    internal class ValidationPipe(ILogger<ValidationPipe> logger, IPipe<Request, Response> next) 
        : IPipe<Request, Response>
    {
        public Task HandleAsync(PipelineContext<Request, Response> context)
        {
            logger.LogInformation($"{nameof(ValidationPipe)} called");
            context.Properties["IsValid"] = new PropertyValue { BoolVal = true };

            return next.HandleAsync(context);
        }
    }
}
