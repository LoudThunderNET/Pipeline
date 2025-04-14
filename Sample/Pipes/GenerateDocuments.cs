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

            context.Response = new Response
            {
                IsSuccess = true,
                Message = context.Request.Name,
            };

            return next.HandleAsync(context);
        }
    }
}
