using Pipeline.Lib.Abstraction;

namespace Pipeline.Lib.PipeNodes
{
    public class PipeNode
    {
        public static PipeNode Empty = new PipeNode(null);

        private readonly string _uniqueId;
        public PipeNode(Type? pipeType)
        {
            PipeType = pipeType;
            _uniqueId = Guid.NewGuid().ToString();
        }

        public PipeNode(Type? pipeType, PipeNode next) : this(pipeType)
        {
            Next = next;
        }

        /// <summary>
        /// Уникальный идентификатор нода. Используется при 
        /// регистрации зависимостей.
        /// </summary>
        public string UniqueId => _uniqueId;

        /// <summary>
        /// Тип <see cref="IPipe{TRequest, TResponse}"/>
        /// </summary>
        public Type? PipeType { get; protected set; }

        /// <summary>
        /// Следующий узел.
        /// </summary>
        public PipeNode? Next { get; set; }
    }
}
