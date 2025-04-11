using Pipeline.Sample.App.Models;
using Pipeline.Lib;

namespace Pipeline.Sample.App
{
    public static class PropertiesExtensions
    {
        private const string ValidKey = nameof(ValidKey);
        public static PipelineContext<TRequest, TResponse> Valid<TRequest, TResponse>(this PipelineContext<TRequest, TResponse> ctx, bool valid)
            where TRequest : class
            where TResponse : class
        {
            if (!ctx.Properties.TryGetValue(ValidKey, out var propVal))
            {
                propVal = new PropertyValue();
                ctx.Properties[ValidKey] = propVal;
            }
            propVal.BoolVal = valid;

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
