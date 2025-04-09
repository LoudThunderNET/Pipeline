using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pipeline.Lib.Abstraction;
using Pipeline.Lib.PipeNodes;
using Pipeline.Lib.Pipes;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Metadata;

namespace Pipeline.Lib
{
    public static class ServiceCollectionExtensions
    {
        private static readonly Dictionary<Type, Expression> expressionCache = new Dictionary<Type, Expression>();

        private static readonly Type genericPipe = typeof(IPipe<,>);

        private static readonly MethodInfo createPipeDependencyMethodInfo = typeof(ServiceCollectionExtensions).GetMethod(
            "CreatePipeDependency",
            2,
            BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public,
            binder: null,
            [typeof(Type), typeof(IServiceProvider)],
            modifiers: null)!;

        private static readonly MethodInfo createDependencyMethodInfo = typeof(ServiceCollectionExtensions).GetMethod(
            "CreateDependency",
            1,
            BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public,
            binder: null,
            [typeof(IServiceProvider)],
            modifiers: null)!;

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

        public static IServiceCollection AddPipelines(this IServiceCollection services, Assembly assembly)
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

                Type[] piplineGenericArguments = definition.PipelineType.GetGenericArguments();
                Type concreatePipelineType = basePipeline.MakeGenericType(piplineGenericArguments);

                ConstructorInfo ctor = GetCtorOf(concreatePipelineType);
                var ctorPipeParam = ctor.GetParameters().First();

                var pipelineRegisterExpression = Expression.Lambda(
                    Expression.New(ctor, BuildResolvePipeCtorParamExpression(definition.Root, ctorPipeParam)),
                    Expression.Parameter(typeof(IServiceProvider), "provider"));

                services.AddScoped(definition.PipelineType, sp => pipelineRegisterExpression.Compile().DynamicInvoke(sp)!);
                services.RegisterPipes(definition.Root);
            }

            return services;
        }

        private static ConstructorInfo GetCtorOf(Type concreatePipelineType)
        {
            var ctors = concreatePipelineType.GetConstructors();
            if (ctors.Length != 1)
            {
                throw new Exception($"Тип {concreatePipelineType.FullName} должен иметь строго 1 публичный конструктор");
            }
            var ctor = ctors[0];
            return ctor;
        }

        private static bool IsImplemented(this Type? type, Type baseType)
        {
            if (type == null)
                return false;

            if (type.IsGenericType && type.GetGenericTypeDefinition() == baseType)
                return true;

            return IsImplemented(type.BaseType, baseType);
        }

        private static void RegisterPipes(this IServiceCollection services, PipeNode? root)
        {
            if (root == null)
                return;

            var ctor = GetCtorOf(root.PipeType);
            ParameterInfo[] ctorParamInfos = ctor.GetParameters().ToArray();

            var ctorParamExpressions = new List<Expression>(ctorParamInfos.Length);
            foreach (ParameterInfo ctorParamInfo in ctorParamInfos)
            {
                Expression paramExpression = BuildResolveCtorParamExpression(root, ctorParamInfo);
                ctorParamExpressions.Add(paramExpression);
            }

            var registerExpression = Expression.Lambda(
                Expression.New(ctor, ctorParamExpressions),
                Expression.Parameter(typeof(IServiceProvider), "provider"));

            if (registerExpression is not LambdaExpression registerLambdaExpression)
            {
                throw new InvalidCastException($"Для типа {root.PipeType.FullName} дерево выражений не является лямбдой.");
            }

            services
                .AddScoped(root.PipeType, sp => registerLambdaExpression.Compile().DynamicInvoke(sp)!)
                .RegisterPipes(root.Next);
        }

        private static Expression BuildResolveCtorParamExpression(PipeNode root, ParameterInfo ctorParamInfo)
        {
            var isPipeParam = ctorParamInfo.ParameterType.IsImplemented(genericPipe);
            if (isPipeParam)
            {
                return BuildResolvePipeCtorParamExpression(root, ctorParamInfo);
            }
            else
            {
                if(!expressionCache.TryGetValue(ctorParamInfo.ParameterType, out var paramExpression))
                { 
                    paramExpression = Expression.Call(
                        createDependencyMethodInfo.MakeGenericMethod(ctorParamInfo.ParameterType),
                        Expression.Parameter(typeof(IServiceProvider), "provider"));

                    expressionCache[ctorParamInfo.ParameterType] = paramExpression;
                }

                return paramExpression;
            }

        }

        private static Expression BuildResolvePipeCtorParamExpression(PipeNode root, ParameterInfo ctorParamInfo)
        {
            var genericParameters = GetGenericType(ctorParamInfo.ParameterType)!.GetGenericArguments();
            var nextPipeType = root.Next?.PipeType ?? typeof(EndPipe<,>).MakeGenericType(genericParameters);

            var paramExpression = Expression.Call(
                    createPipeDependencyMethodInfo.MakeGenericMethod(genericParameters),
                    Expression.Constant(nextPipeType, typeof(Type)),
                    Expression.Parameter(typeof(IServiceProvider), "provider"));

            return paramExpression;
        }

        private static Type? GetGenericType(Type sourceType)
        { 
            if(sourceType.IsGenericType)
                return sourceType;

            if (sourceType.BaseType == null || sourceType.BaseType == typeof(object))
                return null;

            return GetGenericType(sourceType.BaseType);
        }

        private static TObject CreateDependency<TObject>(IServiceProvider serviceProvider)
            where TObject : class
        {
            return serviceProvider.GetRequiredService<TObject>();
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
