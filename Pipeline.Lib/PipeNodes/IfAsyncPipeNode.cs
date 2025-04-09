namespace Pipeline.Lib.PipeNodes
{
    public class IfAsyncPipeNode<TRequest, TResponse>(
            Type pipeType,
            Func<PipelineContext<TRequest, TResponse>, Task<bool>> predicateAsync,
            PipeNode positive) : PipeNode(pipeType)
        where TRequest : class
        where TResponse : class

    {
        /// <summary>
        /// Асинхронный условный предикат.
        /// </summary>
        public Func<PipelineContext<TRequest, TResponse>, Task<bool>> PredicateAsync { get; set; } = predicateAsync;

        /// <summary>
        /// Выполняется если <see cref="Predicate"/> возвращает <see langword="true"/>.
        /// </summary>
        public PipeNode Positive { get; set; } = positive;
    }
}
