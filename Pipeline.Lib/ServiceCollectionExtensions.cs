using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pipeline.Lib.Abstraction;
using Pipeline.Lib.Pipes;
using System.Linq.Expressions;
using System.Reflection;

namespace Pipeline.Lib
{
    public static class ServiceCollectionExtensions
    {
        private static Dictionary<Type, Expression> s_expressionCache = new Dictionary<Type, Expression>();
        private static MethodInfo getRequiredServiceMethodInfo = typeof(ServiceProviderServiceExtensions).GetMethod(
            "GetRequiredService",
            0,
            [typeof(IServiceProvider), typeof(Type)])!;
        private static Type genericPipe = typeof(IPipe<,>);
        private static MethodInfo CreatePipeDependencyMethodInfo = typeof(ServiceProviderServiceExtensions).GetMethod(
            "CreatePipeDependency",
            2,
            [typeof(Type), typeof(IServiceProvider)])!;

        private static MethodInfo CreateDependencyMethodInfo = typeof(ServiceProviderServiceExtensions).GetMethod(
            "CreateDependency",
            1,
            [typeof(Type), typeof(IServiceProvider)])!;

        public static IServiceCollection AddPipeline<TRequest, TResponse>(this IServiceCollection services, Assembly assembly)
            where TRequest : class
            where TResponse : class
        { 
            services.TryAddSingleton<IPipeFactory, PipeFactory>();
            services.TryAddSingleton<EndPipe<TRequest, TResponse>>();

            var pipelineDefinition = Activator.CreateInstance<PipelineDefinition<TRequest, TResponse>>();
            //pipelineDefinition.PipelineType

            return services;
        }

        public static IServiceCollection AddPipelines<TRequest, TResponse>(this IServiceCollection services, Assembly assembly)
        {

            var pipelineDefinitionTypes = assembly.GetExportedTypes()
                .Where(t => !t.IsAbstract && t.IsClass && IsImplemented(t, typeof(PipelineDefinition<,>)))
                .ToList();

            var basePipeline = typeof(Pipeline<,>);

            foreach (var pipelineDefinitionType in pipelineDefinitionTypes)
            {
                var definition = Activator.CreateInstance(pipelineDefinitionType) as IPipelineDefinition;
                if (definition == null)
                    throw new InvalidCastException($"Тип {pipelineDefinitionType.FullName} не реализует {typeof(IPipelineDefinition).FullName}");

                var piplineGenericArguments = definition.PipelineType.GetGenericArguments();
                var concreatePipelineType = basePipeline.MakeGenericType(piplineGenericArguments);
                //TODO[SS] переделать регистрацию
                services.AddScoped(definition.PipelineType, concreatePipelineType);
                RegisterPipes(definition.Root, services);
            }

            return services;
        }

        private static bool IsImplemented(this Type? type, Type baseType)
        {
            if (type == null)
                return false;

            if (type.IsGenericType && type.GetGenericTypeDefinition() == baseType)
                return true;

            return IsImplemented(type.BaseType, baseType);
        }

        private static void RegisterPipes(PipeNode root, IServiceCollection services)
        {
            if (root.Next == null)
                return;

            if (s_expressionCache.TryGetValue(root.PipeType, out var registerExpression))
            {
                var ctor = root.PipeType.GetConstructors().First();
                var ctorParamInfos = ctor.GetParameters()
                    .ToArray();
                var ctorParamExpressions = new List<Expression>(ctorParamInfos.Length);
                foreach (var ctorParamInfo in ctorParamInfos)
                {
                    if (!s_expressionCache.TryGetValue(ctorParamInfo.ParameterType, out var paramExpression))
                    {
                        if (ctorParamInfo.ParameterType.IsImplemented(genericPipe))
                        {
                            var genericParameters = GetGenericType(ctorParamInfo.ParameterType)!.GetGenericArguments();
                            var nextPipeType = root.Next?.PipeType ?? typeof(EndPipe<,>).MakeGenericType(genericParameters);

                            paramExpression = Expression.Call(CreatePipeDependencyMethodInfo.MakeGenericMethod(genericParameters), Expression.Constant(nextPipeType, typeof(Type)), Expression.Parameter());
                        }
                        else
                        { 
                        }
                    }
                    ctorParamExpressions.Add(paramExpression);
                }
                var providerParam = Expression.Parameter(typeof(IServiceProvider), "provider");
                var newPipeExpression = Expression.Lambda(Expression.New(ctor, ctorParamExpressions), providerParam);

                var genericPipeType = GetGenericType(root.PipeType);
                if (genericPipeType == null)
                    throw new InvalidOperationException($"Тип {root.PipeType.FullName} не реализует");

                var genericParams = genericPipeType.GetGenericArguments();

                //var providerParam = Expression.Parameter(typeof(IServiceProvider), "provider");
                var serviceTypeParam = Expression.Parameter(typeof(Type), "serviceType");
                var nextExpr = Expression.Lambda(
                    Expression.TypeAs(
                        Expression.Call(getRequiredServiceMethodInfo, providerParam, serviceTypeParam),
                        genericPipe.MakeGenericType(genericParams)),
                    providerParam,
                    serviceTypeParam);
                s_expressionCache[root.PipeType] = registerExpression;
            }

            //services.AddScoped(root.PipeType, sp => newPipeExpression.Compile().DynamicInvoke(sp)!);
        }

        private static Type? GetGenericType(Type sourceType)
        { 
            if(sourceType.IsGenericType)
                return sourceType.GetGenericTypeDefinition();

            if (sourceType.BaseType == null || sourceType.BaseType == typeof(object))
                return null;

            return GetGenericType(sourceType.BaseType);
        }

        private static TObject CreateDependency<TObject>(Type dependencyType, IServiceProvider serviceProvider)
        {
            object dependencyObject = serviceProvider.GetRequiredService(dependencyType);

            if (dependencyObject is TObject dependency)
                return dependency;

            throw new InvalidOperationException($"Тип {dependencyType} не реализует {typeof(TObject).FullName}.");
        }

        public static IPipe<TRequest, TResponse> CreatePipeDependency<TRequest, TResponse>(Type pipeType, IServiceProvider serviceProvider)
            where TRequest : class
            where TResponse : class
        {
            object pipeObject = serviceProvider.GetRequiredService(pipeType);

            if (pipeObject is IPipe<TRequest, TResponse> pipe)
                return pipe;

            throw new InvalidOperationException($"Тип {pipeType.FullName} не реализует {genericPipe.FullName}.");
        }
    }
}
