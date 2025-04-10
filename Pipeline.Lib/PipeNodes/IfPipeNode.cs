using Pipeline.Lib.Pipes;

namespace Pipeline.Lib.PipeNodes
{
    public class IfPipeNode<TRequest, TResponse> : PipeNode
        where TRequest : class
        where TResponse : class

    {
        public IfPipeNode(
            Predicate<PipelineContext<TRequest, TResponse>> predicate,
            PipeNode positive)
            : base(typeof(IfPipe<TRequest, TResponse>))
        {
            Predicate = predicate;
            Positive = positive;
        }

        /// <summary>
        /// Условный предикат.
        /// </summary>
        public Predicate<PipelineContext<TRequest, TResponse>> Predicate { get; }

        /// <summary>
        /// Выполняется если <see cref="Predicate"/> возвращает <see langword="true"/>.
        /// </summary>
        public PipeNode Positive { get; set; } = null!;
    }
}
