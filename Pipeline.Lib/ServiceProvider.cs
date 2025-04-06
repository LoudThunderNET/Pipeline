using Pipeline.Lib.Abstraction;

namespace Pipeline.Lib
{
    public delegate IPipe<TRequest, TResponse> ServiceProvider<TRequest, TResponse>(Type pipeType)
        where TRequest : class
        where TResponse : class;
}
