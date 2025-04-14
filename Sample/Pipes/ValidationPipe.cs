using Microsoft.Extensions.Logging;
using Pipeline.Sample.App.Models;
using Pipeline.Lib;
using Pipeline.Lib.Abstraction;

namespace Pipeline.Sample.App.Pipes
{
    internal class ValidationPipe(ILogger<ValidationPipe> logger, IPipe<Request, Response> next) 
        : IPipe<Request, Response>
    {
        public Task HandleAsync(PipelineContext<Request, Response> context)
        {
            logger.LogInformation($"{nameof(ValidationPipe)} called");
            context.Valid(false);

            return next.HandleAsync(context);
        }
    }
}
