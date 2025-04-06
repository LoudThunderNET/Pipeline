using Pipeline.Console.App.Models;
using Pipeline.Lib.Abstraction;

namespace Pipeline.Console.App.Pipes
{
    internal class ValidationPipe(IPipe<Request, Response> next) 
        : IPipe<Request, Response>
    {
        public Task HandleAsync(PipelineContext<Request, Response> context)
        {
            context.Properties["IsValid"] = new PropertyValue { BoolVal = true };

            return next.HandleAsync(context);
        }
    }
}
