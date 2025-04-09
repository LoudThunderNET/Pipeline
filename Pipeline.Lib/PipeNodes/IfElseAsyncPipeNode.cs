namespace Pipeline.Lib.PipeNodes
{
    public class IfElseAsyncPipeNode<TRequest, TResponse> : IfAsyncPipeNode<TRequest, TResponse>
        where TRequest : class
        where TResponse : class
    {
        public IfElseAsyncPipeNode(
            Type pipeType,
            Func<PipelineContext<TRequest, TResponse>, Task<bool>> predicateAsync,
            PipeNode positive,
            PipeNode alternative)
            : base(pipeType, predicateAsync, positive)
        {
            Alternative = alternative;
        }

        /// <summary>
        /// Выполняется если <see cref="Predicate"/> возвращает <see langword="false"/>.
        /// </summary>
        public PipeNode Alternative { get; set; } = null!;
    }
}
