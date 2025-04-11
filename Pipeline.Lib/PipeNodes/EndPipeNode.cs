using Pipeline.Lib.Pipes;

namespace Pipeline.Lib.PipeNodes
{
    internal class EndPipeNode<TRequest, TResponse>() 
        : PipeNode(typeof(EndPipe<TRequest, TResponse>))
        where TRequest : class
        where TResponse : class
    {
    }
}
