using Pipeline.Lib.Abstraction;

namespace Pipeline.Lib
{
    /// <inheritdoc cref="IPipeline{TRequest}"/>
    public class Pipeline<TRequest, TResponse> : IPipeline<TRequest, TResponse>
        where TRequest : class
        where TResponse : class
    {
        private readonly IPipeFactory _serviceProvider;
        private readonly Type _root;

        public Pipeline(IPipeFactory serviceProvider, Type root)
        {
            _serviceProvider = serviceProvider;
            _root = root;
        }

        /// <inheritdoc/>
        public async Task<TResponse?> HandleAsync(TRequest request, CancellationToken cancellationToken)
        {
            var pipe = _serviceProvider.Create<TRequest, TResponse>(_root);
            if (pipe == null)
            { 
                throw new InvalidOperationException($"Промежуточное ПО {_root.FullName} не зарегистрировано");
            }
            var context = new PipelineContext<TRequest, TResponse>(request, cancellationToken);
            await pipe.HandleAsync(context);

            return context.Response;
        }
    }
}
