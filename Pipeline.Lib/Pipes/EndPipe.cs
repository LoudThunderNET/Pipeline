using Pipeline.Lib.Abstraction;

namespace Pipeline.Lib.Pipes
{
    public class EndPipe<TRequest, TResponse> : IPipe<TRequest, TResponse>
        where TRequest : class
        where TResponse : class
    {
        /// <inheritdoc/>
        public Task HandleAsync(PipelineContext<TRequest, TResponse> context)
        {
            return Task.CompletedTask;
        }
    }
}
