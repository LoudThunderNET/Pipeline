
namespace Pipeline.Lib.Abstraction
{
    public interface IPipelineContext
    {
        CancellationToken CancellationToken { get; }
        Dictionary<string, PropertyValue> Properties { get; }
    }
}