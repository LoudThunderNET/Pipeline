using Pipeline.Console.App.Models;
using Pipeline.Lib.Abstraction;

namespace Pipeline.Console.App.Pipes
{
    internal class NotifyInvalidRequest : IPipe<Request, Response>
    {
        public NotifyInvalidRequest() 
        { 
        }

        public Task HandleAsync(PipelineContext<Request, Response> context)
        {
            // Notify(context.Response);
            return Task.CompletedTask;
        }
    }
}
