using Pipeline.Lib.Abstraction;

namespace Pipeline.Lib.PipeNodes
{
    public class IfElseAsyncPipeNode<TRequest, TResponse> : IfAsyncPipeNode<TRequest, TResponse>
        where TRequest : class
        where TResponse : class
    {
        public IfElseAsyncPipeNode(
            Type pipeType,
            PredicateAsync<PipelineContext<TRequest, TResponse>> predicateAsync,
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

        /// <inheritdoc/>
        public override IEnumerable<PipeNode> Children
        {
            get
            {
                yield return Positive;
                yield return Alternative;
                yield return Positive;
                foreach (var item in base.Children)
                {
                    yield return item;
                }
            }
        }
    }
}
