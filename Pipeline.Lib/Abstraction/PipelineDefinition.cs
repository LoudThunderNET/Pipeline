using System.Runtime.CompilerServices;
using Pipeline.Lib.Pipes;

namespace Pipeline.Lib.Abstraction
{
    internal interface IPipelineDefinition
    {
        /// <summary>
        /// Тип <see cref="IPipeline{TRequest, TResponse}"/>
        /// </summary>
        Type PipelineType { get; }

        /// <summary>
        /// Первый Pipe конвейера.
        /// </summary>
        PipeNode Root { get; }
    }

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
            var _pipelineBuilder = new PipelineBuilder();

            return _pipelineBuilder;
        }

        private interface IRoot
        {
            PipeNode Root { get; }
        }

        protected class PipelineBuilder: IRoot
        {
            private PipeNode? _root = PipeNode.Empty;
            private PipeNode? _current = PipeNode.Empty;

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

                if (_root == null)
                    _root = _current;

                return this;
            }

            private void AttachCurrentNext(PipeNode pipeNode)
            {
                if (_current == null)
                    _current = pipeNode;
                else
                    _current.Next = pipeNode;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private PipeNode GetRoot() 
        {
            if (_pipelineBuilder == null)
                return PipeNode.Empty;

            return ((IRoot)_pipelineBuilder).Root;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Type GetPipelineType()
        {
            ArgumentNullException.ThrowIfNull(_pipelineType);
            return _pipelineType;
        }
    }


    public class PipeNode
    {
        public static PipeNode Empty = new PipeNode(null);

        public PipeNode(Type pipeType)
        {
            PipeType = pipeType;
        }

        /// <summary>
        /// Тип <see cref="IPipe{TRequest, TResponse}"/>
        /// </summary>
        public Type PipeType { get; set; } = null!;

        /// <summary>
        /// Следующий узел.
        /// </summary>
        public PipeNode? Next { get; set; }
    }

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
