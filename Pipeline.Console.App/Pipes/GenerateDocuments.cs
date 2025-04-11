using Microsoft.Extensions.Logging;
using Pipeline.Sample.App.Models;
using Pipeline.Lib;
using Pipeline.Lib.Abstraction;

namespace Pipeline.Sample.App.Pipes
{
    internal class GenerateDocuments(
        ILogger<GenerateDocuments> logger,
        IPipe<Request, Response> next) : IPipe<Request, Response>
    {
        public Task HandleAsync(PipelineContext<Request, Response> context)
        {
            logger.LogInformation($"{nameof(GenerateDocuments)} called");

            return next.HandleAsync(context);
        }
    }
}
