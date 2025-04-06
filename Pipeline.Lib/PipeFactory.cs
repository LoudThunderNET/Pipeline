using Microsoft.Extensions.DependencyInjection;
using Pipeline.Lib.Abstraction;

namespace Pipeline.Lib
{
    internal class PipeFactory : IPipeFactory
    {
        private readonly IServiceProvider _serviceProvider;
        public PipeFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IPipe<TRequest, TResponse> Create<TRequest, TResponse>(Type pipeType)
            where TRequest : class
            where TResponse : class
        {
            var pipe = _serviceProvider.GetRequiredService(pipeType) as IPipe<TRequest, TResponse>;
            if (pipe == null)
                throw new Exception();

            return pipe;
        }
    }
}
