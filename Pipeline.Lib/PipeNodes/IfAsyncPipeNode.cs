using Pipeline.Lib.Abstraction;

namespace Pipeline.Lib.PipeNodes
{
    public class IfAsyncPipeNode<TRequest, TResponse>(
            Type pipeType,
            PredicateAsync<PipelineContext<TRequest, TResponse>> predicateAsync,
            PipeNode positive) : PipeNode(pipeType, new EndPipeNode<TRequest, TResponse>())
        where TRequest : class
        where TResponse : class

    {
        /// <summary>
        /// Асинхронный условный предикат.
        /// </summary>
        public PredicateAsync<PipelineContext<TRequest, TResponse>> Predicate { get; set; } = predicateAsync;

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
                yield return Positive;
                foreach (var item in base.Children)
                {
                    yield return item;
                }
            }
        }
    }
}
