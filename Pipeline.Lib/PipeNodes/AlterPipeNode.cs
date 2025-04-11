using Pipeline.Lib.Pipes;

namespace Pipeline.Lib.PipeNodes
{
    public class AlterPipeNode<TRequest, TResponse>(
            Predicate<PipelineContext<TRequest, TResponse>> predicate,
            PipeNode positive) : PipeNode(typeof(AlterPipe<TRequest, TResponse>), new EndPipeNode<TRequest, TResponse>())
        where TRequest : class
        where TResponse : class

    {
        /// <summary>
        /// Условный предикат.
        /// </summary>
        public Predicate<PipelineContext<TRequest, TResponse>> Predicate { get; } = predicate;

        /// <summary>
        /// Выполняется если <see cref="Predicate"/> возвращает <see langword="true"/>.
        /// </summary>
        public PipeNode Positive { get; set; } = positive;

        /// <inheritdoc/>
        public override IEnumerable<PipeNode> Children
        {
            get
            {
                yield return Positive;
                foreach (var item in base.Children)
                {
                    yield return item;
                }
            }
        }
    }
}
