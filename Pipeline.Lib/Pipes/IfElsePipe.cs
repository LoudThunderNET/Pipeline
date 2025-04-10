using Pipeline.Lib.Abstraction;

namespace Pipeline.Lib.Pipes
{
    public class IfElsePipe<TRequest, TResponse>(
            // порядок аргументов не менять !
            Predicate<PipelineContext<TRequest, TResponse>> predicate,
            IPipe<TRequest, TResponse> positive,
            IPipe<TRequest, TResponse> alter,
            IPipe<TRequest, TResponse> main)
        : IPipe<TRequest, TResponse>
        where TRequest : class
        where TResponse : class
    {
        public async Task HandleAsync(PipelineContext<TRequest, TResponse> context)
        {
            if(predicate(context))
                await positive.HandleAsync(context);
            else
                await alter.HandleAsync(context);

            await main.HandleAsync(context);
        }
    }
}
