using Microsoft.Extensions.Logging;
using Pipeline.Sample.App.Models;
using Pipeline.Lib;
using Pipeline.Lib.Abstraction;

namespace Pipeline.Sample.App.Pipes
{
    internal class NotifyInvalidRequest(ILogger<NotifyInvalidRequest> logger) : IPipe<Request, Response>
    {
        public Task HandleAsync(PipelineContext<Request, Response> context)
        {
            logger.LogInformation($"{nameof(NotifyInvalidRequest)} called");
            return Task.CompletedTask;
        }
    }
}
