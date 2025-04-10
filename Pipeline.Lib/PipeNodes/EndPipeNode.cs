using Pipeline.Lib.Pipes;

namespace Pipeline.Lib.PipeNodes
{
    internal class EndPipeNode<TRequest, TResponse> : PipeNode
        where TRequest : class
        where TResponse : class
    {
        public EndPipeNode() : base(typeof(EndPipe<TRequest, TResponse>))
        {
        }
    }
}
