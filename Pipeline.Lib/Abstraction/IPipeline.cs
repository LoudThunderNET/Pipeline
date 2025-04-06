namespace Pipeline.Lib.Abstraction
{
    public interface IPipeline<TRequest, TResponse>
        where TRequest : class
        where TResponse: class
    {
        Task<TResponse?> HandleAsync(TRequest context, CancellationToken cancellationToken);
    }
}
