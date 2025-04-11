using Pipeline.Lib.Abstraction;
using Pipeline.Lib.PipeNodes;

namespace Pipeline.Lib
{
    public abstract class PipelineDefinition<TRequest, TResponse> : IPipelineDefinition
        where TRequest : class
        where TResponse : class
    {
        private Type? _pipelineType;
        private PipelineBuilder? _pipelineBuilder;

        /// <inheritdoc/>
        public Type PipelineType => GetPipelineType();

        /// <inheritdoc/>
        public PipeNode Root => _pipelineBuilder?.Root ?? throw new InvalidOperationException("Корень конвейера не задан");

        public PipelineDefinition()
        {
            Define();
        }

        protected abstract void Define();

        protected PipelineBuilder Pipeline()
        {
            _pipelineType = typeof(IPipeline<TRequest, TResponse>);
            _pipelineBuilder = new PipelineBuilder();

            return _pipelineBuilder;
        }

        protected class PipelineBuilder
        {
            private PipeNode? _root = null;
            private PipeNode? _current = null;

            internal PipeNode Root
            {
                get
                {
                    ArgumentNullException.ThrowIfNull(_root);
                    return _root;
                }
            }

            public PipelineBuilder AddPipe<TPipe>()
                where TPipe : class, IPipe<TRequest, TResponse>
            {
                var pipeNode = new PipeNode(typeof(TPipe), new EndPipeNode<TRequest, TResponse>());
                AttachCurrentNext(pipeNode);

                return this;
            }

            public PipelineBuilder If(
                Predicate<PipelineContext<TRequest, TResponse>> predicate,
                Action<PipelineBuilder> positiveBranch)
            {
                var builder = new PipelineBuilder();
                positiveBranch(builder);

                if (builder._root == null)
                    throw new InvalidOperationException("Не задано промежуточное ПО для простой развилки");

                var pipeNode = new IfPipeNode<TRequest, TResponse>(predicate, builder._root);
                AttachCurrentNext(pipeNode);

                return this;
            }

            public PipelineBuilder IfElse(
                Predicate<PipelineContext<TRequest, TResponse>> predicate,
                Action<PipelineBuilder> positiveBranch,
                Action<PipelineBuilder> altBranch)
            {
                var builder = new PipelineBuilder();
                positiveBranch(builder);

                if (builder._root == null)
                    throw new InvalidOperationException("Не задано промежуточное ПО для простой развилки");

                var pipeNode = new IfPipeNode<TRequest, TResponse>(predicate, builder._root);
                AttachCurrentNext(pipeNode);

                return this;
            }

            public PipelineBuilder Alter(
                Predicate<PipelineContext<TRequest, TResponse>> predicate,
                Action<PipelineBuilder> positiveBranch)
            {
                var builder = new PipelineBuilder();
                positiveBranch(builder);

                if (builder._root == null)
                    throw new InvalidOperationException("Не задано промежуточное ПО для простой развилки");

                var pipeNode = new AlterPipeNode<TRequest, TResponse>(predicate, builder._root);
                AttachCurrentNext(pipeNode);

                return this;
            }

            private void AttachCurrentNext(PipeNode pipeNode)
            {
                if (_current == null)
                    _current = pipeNode;
                else
                    _current.Next = pipeNode;

                if (_root == null)
                    _root = _current;

                _current = pipeNode;
            }
        }

        private Type GetPipelineType()
        {
            ArgumentNullException.ThrowIfNull(_pipelineType);
            return _pipelineType;
        }
    }
}
