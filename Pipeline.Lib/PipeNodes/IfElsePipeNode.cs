using Pipeline.Lib.Pipes;

namespace Pipeline.Lib.PipeNodes
{
    public class IfElsePipeNode<TRequest, TResponse> : IfPipeNode<TRequest, TResponse>
        where TRequest : class
        where TResponse : class
    {
        public IfElsePipeNode(
            Func<PipelineContext<TRequest, TResponse>, bool> predicate,
            PipeNode positive,
            PipeNode alternative)
            : base(predicate, positive)
        {
            PipeType = typeof(IfElsePipe<TRequest, TResponse>);
            Alternative = alternative;
        }

        /// <summary>
        /// Выполняется если <see cref="Predicate"/> возвращает <see langword="false"/>.
        /// </summary>
        public PipeNode Alternative { get; set; } = null!;
    }
}
