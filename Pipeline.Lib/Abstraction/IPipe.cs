namespace Pipeline.Lib.Abstraction
{
    public interface IPipe<TRequest, TResponse>
        where TRequest : class
        where TResponse : class
    {
        Task HandleAsync(PipelineContext<TRequest, TResponse> context);
    }
}
