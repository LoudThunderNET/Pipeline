using Pipeline.Lib.Abstraction;

namespace Pipeline.Lib.Pipes
{
    public class AlterPipe<TRequest, TResponse>(
        Predicate<PipelineContext<TRequest, TResponse>> predicate,
            IPipe<TRequest, TResponse> positive,
            IPipe<TRequest, TResponse> main) : IPipe<TRequest, TResponse>
        where TRequest : class
        where TResponse : class
    {
        public Task HandleAsync(PipelineContext<TRequest, TResponse> context)
        {
            if(predicate(context))
                return positive.HandleAsync(context);
            else
                return main.HandleAsync(context);
        }
    }
}
