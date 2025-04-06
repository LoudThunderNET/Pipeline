using Microsoft.Extensions.DependencyInjection;
using Pipeline.Lib.Abstraction;
using System.Linq.Expressions;
using System.Reflection;

namespace Pipeline.Lib
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddPipelins(this IServiceCollection services, Assembly assembly)
        {
            services.AddSingleton<IPipeFactory, PipeFactory>();

            var definitionTypes = assembly.GetExportedTypes()
                .Where(t => !t.IsAbstract && t.IsClass && IsImplemented(t, typeof(PipelineDefinition<,>)))
                .ToList();
            var delegateServiceProvider = typeof(ServiceProvider<,>);
            var genericPipeType = typeof(IPipe<,>);

            var basePipeline = typeof(Pipeline<,>);

            foreach (var definitionType in definitionTypes)
            {
                var definition = Activator.CreateInstance(definitionType) as IPipelineDefinition;
                if (definition == null)
                    throw new InvalidCastException($"Тип {definitionType.FullName} не реализует {typeof(IPipelineDefinition).FullName}");

                var piplineGenericArguments = definition.PipelineType.GetGenericArguments();
                var concreatePipelineType = basePipeline.MakeGenericType(piplineGenericArguments);

                //definition.Root
                services.AddScoped(definition.PipelineType, concreatePipelineType);
            }

            return services;
        }

        private static bool IsImplemented(Type? type, Type baseType)
        {
            if (type == null)
                return false;

            if (type.IsGenericType && type.GetGenericTypeDefinition() == baseType)
                return true;

            return IsImplemented(type.BaseType, baseType);
        }

        private static void RegisterPipes(PipeNode root, IServiceCollection services, IServiceProvider serviceProvider)
        {
            if (root.Next == null)
                return;

            //serviceProvider.GetRequiredService()
            var getRequiredServiceMethodInfo = typeof(ServiceProviderServiceExtensions)
                .GetMethod("GetRequiredService", 0, [typeof(IServiceProvider), typeof(Type)]);

            if (getRequiredServiceMethodInfo == null)
                throw new Exception();

            var providerParamExp = Expression.Parameter(typeof(IServiceProvider), "provider");


            var ctor = root.PipeType.GetConstructors().First();
            //var ctorParamExpressions = ctor.GetParameters()
            //    .Select( p => Expression.Call(Expression.Parameter(p.ParameterType, p.Name)))
            //    .ToArray();

            //Expression.New(ctor, ctorParamExpressions);
            var genericParams = root.PipeType.GetGenericArguments();
            var genericPipe = typeof(IPipe<,>);
            var nextExpr = Expression.Lambda
                (
                Expression.TypeAs
                    (
                    Expression.Call
                        (
                            getRequiredServiceMethodInfo,
                            Expression.Parameter(typeof(IServiceProvider), "provider"),
                            Expression.Parameter(typeof(Type), "serviceType")
                        ), 
                        genericPipe.MakeGenericType(genericParams)
                    ),
                Expression.Parameter(typeof(IServiceProvider), "provider")
                );

           // services.AddScoped(root.PipeType, sp => );
        }
    }
}
