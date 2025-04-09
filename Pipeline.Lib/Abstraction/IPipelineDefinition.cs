using Pipeline.Lib.PipeNodes;

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
}
