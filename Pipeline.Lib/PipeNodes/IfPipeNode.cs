using Pipeline.Lib.Pipes;

namespace Pipeline.Lib.PipeNodes
{
    public class IfPipeNode<TRequest, TResponse> : PipeNode
        where TRequest : class
        where TResponse : class

    {
        public IfPipeNode(
            Func<PipelineContext<TRequest, TResponse>, bool> predicate,
            PipeNode positive)
            : base(typeof(IfPipe<TRequest, TResponse>))
        {
            Predicate = predicate;
            Positive = positive;
        }

        /// <summary>
        /// Условный предикат.
        /// </summary>
        public Func<PipelineContext<TRequest, TResponse>, bool> Predicate { get; set; } = null!;

        /// <summary>
        /// Выполняется если <see cref="Predicate"/> возвращает <see langword="true"/>.
        /// </summary>
        public PipeNode Positive { get; set; } = null!;
    }
}
