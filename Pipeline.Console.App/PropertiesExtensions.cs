using Pipeline.Console.App.Models;
using Pipeline.Lib.Abstraction;

namespace Pipeline.Console.App
{
    public static class PropertiesExtensions
    {
        private const string ValidKey = nameof(ValidKey);
        public static PipelineContext<TRequest, TResponse> Valid<TRequest, TResponse>(this PipelineContext<TRequest, TResponse> ctx, bool valid)
            where TRequest : class
            where TResponse : class
        {
            return ctx;
        }

        public static bool? IsValid<TRequest, TResponse>(this PipelineContext<TRequest, TResponse> ctx)
            where TRequest : class
            where TResponse : class
        {
            return ctx.Properties.TryGetValue(ValidKey, out var propVal) ? propVal.BoolVal : null;
        }
    }
}
