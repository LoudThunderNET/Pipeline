using Pipeline.Lib.Abstraction;

namespace Pipeline.Lib.Pipes
{
    internal class IfPipe<TRequest, TResponse> : IPipe<TRequest, TResponse>
        where TRequest : class
        where TResponse : class
    {
        private readonly Func<PipelineContext<TRequest, TResponse>, bool> _predicate;
        private readonly IPipe<TRequest, TResponse> _positive;
        private readonly IPipe<TRequest, TResponse> _main;

        public IfPipe(Func<PipelineContext<TRequest, TResponse>, bool> predicate, 
            IPipe<TRequest, TResponse> positive,
            IPipe<TRequest, TResponse> main) 
        {
            _predicate = predicate;
            _positive = positive;
            _main = main;
        }

        public async Task HandleAsync(PipelineContext<TRequest, TResponse> context)
        {
            if(_predicate(context))
                await _positive.HandleAsync(context);

            await _main.HandleAsync(context);
        }
    }
}
