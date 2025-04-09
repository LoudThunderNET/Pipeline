using Pipeline.Lib.Abstraction;

namespace Pipeline.Lib.PipeNodes
{
    public class PipeNode
    {
        public static PipeNode Empty = new PipeNode(null);

        public PipeNode(Type? pipeType)
        {
            PipeType = pipeType;
        }

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
