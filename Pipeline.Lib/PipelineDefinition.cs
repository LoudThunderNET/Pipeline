using Pipeline.Lib.Abstraction;
using Pipeline.Lib.PipeNodes;
using System.Runtime.CompilerServices;

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
        public PipeNode Root => GetRoot();

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

        private interface IRoot
        {
            PipeNode Root { get; }
        }

        protected class PipelineBuilder : IRoot
        {
            private PipeNode? _root = null;
            private PipeNode? _current = null;

            public PipelineBuilder()
            {
            }

            public PipelineBuilder(PipeNode root)
            {
                _root = _current = root;
            }

            PipeNode IRoot.Root
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
                var pipeNode = new PipeNode(typeof(TPipe));
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

            public PipelineBuilder If(
                Func<PipelineContext<TRequest, TResponse>, bool> predicate,
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
                Func<PipelineContext<TRequest, TResponse>, bool> predicate,
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
        }

        private PipeNode GetRoot()
        {
            if (_pipelineBuilder == null)
                return PipeNode.Empty;

            return ((IRoot)_pipelineBuilder).Root;
        }

        private Type GetPipelineType()
        {
            ArgumentNullException.ThrowIfNull(_pipelineType);
            return _pipelineType;
        }
    }
}
