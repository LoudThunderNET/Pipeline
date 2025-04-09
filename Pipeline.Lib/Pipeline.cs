using Pipeline.Lib.Abstraction;

namespace Pipeline.Lib
{
    /// <inheritdoc cref="IPipeline{TRequest}"/>
    public class Pipeline<TRequest, TResponse> : IPipeline<TRequest, TResponse>
        where TRequest : class
        where TResponse : class
    {
        private readonly IPipe<TRequest, TResponse> _root;

        public Pipeline(IPipe<TRequest, TResponse> root)
        {
            _root = root;
        }

        /// <inheritdoc/>
        public async Task<TResponse?> HandleAsync(TRequest request, CancellationToken cancellationToken)
        {
            var context = new PipelineContext<TRequest, TResponse>(request, cancellationToken);
            await _root.HandleAsync(context);

            return context.Response;
        }
    }
}
