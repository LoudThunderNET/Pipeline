using Microsoft.Extensions.Logging;
using Pipeline.Console.App.Models;
using Pipeline.Lib;
using Pipeline.Lib.Abstraction;

namespace Pipeline.Console.App.Pipes
{
    internal class GenerateDocuments(
        ILogger<GenerateDocuments> logger,
        IPipe<Request, Response> next) : IPipe<Request, Response>
    {
        public Task HandleAsync(PipelineContext<Request, Response> context)
        {
            logger.LogInformation("Документы сформированы");

            return next.HandleAsync(context);
        }
    }
}
