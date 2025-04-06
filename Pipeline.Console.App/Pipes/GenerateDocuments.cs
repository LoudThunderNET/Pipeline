using Microsoft.Extensions.Logging;
using Pipeline.Console.App.Models;
using Pipeline.Lib.Abstraction;

namespace Pipeline.Console.App.Pipes
{
    internal class GenerateDocuments : IPipe<Request, Response>
    {
        private readonly ILogger<GenerateDocuments> _logger;

        public GenerateDocuments(ILogger<GenerateDocuments> logger)
        {
            _logger = logger;
        }

        public Task HandleAsync(PipelineContext<Request, Response> context)
        {
            _logger.LogInformation("Документы сформированы");

            return Task.CompletedTask;
        }
    }
}
