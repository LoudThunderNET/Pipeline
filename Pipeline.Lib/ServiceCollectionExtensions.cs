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
        private static readonly Dictionary<Type, Expression> expressionCache = [];

        private static readonly Type genericPipe = typeof(IPipe<,>);

        private static readonly MethodInfo resolvePipeDependencyMethodInfo = typeof(ServiceCollectionExtensions).GetMethod(
            nameof(ResolvePipeDependency),
            2,
            BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public,
            binder: null,
            [typeof(Type), typeof(object), typeof(IServiceProvider)],
            modifiers: null)!;

        private static readonly MethodInfo resolveDependencyMethodInfo = typeof(ServiceCollectionExtensions).GetMethod(
            nameof(ResolveDependency),
            1,
            BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public,
            binder: null,
            [typeof(IServiceProvider)],
            modifiers: null)!;

        private static readonly MethodInfo createPipeExpressionMethodIndo = typeof(ServiceCollectionExtensions).GetMethod(
            nameof(CreatePipeExpression),
            2,
            BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public,
            binder: null,
            [typeof(PipeNode), typeof(Type[]), typeof(ParameterExpression)],
            modifiers: null)!;

        private static readonly Type basePipeline = typeof(Pipeline<,>);

        private static Type pipelineContextType = typeof(PipelineContext<,>);
        private static Type predicateType = typeof(Predicate<>);

        //private static Type concreatePipelineContextType = null!;
        private static Type concreatePredicateType = null!;

        public static IServiceCollection AddPipelines(this IServiceCollection services, Assembly assembly)
        {
            var pipelineDefinitionTypes = assembly.GetExportedTypes()
                .Where(t => !t.IsAbstract && t.IsClass && IsImplement(t, typeof(PipelineDefinition<,>)))
                .ToList();

            foreach (var pipelineDefinitionType in pipelineDefinitionTypes)
            {
                IPipelineDefinition? definition = CreatePipelineDefinition(pipelineDefinitionType);
                Type[] piplineGenericArguments = GetPipelineGenericArgumets(definition);

                concreatePredicateType = predicateType.MakeGenericType(pipelineContextType.MakeGenericType(piplineGenericArguments));

                Type concreatePipelineType = basePipeline.MakeGenericType(piplineGenericArguments);

                ConstructorInfo ctor = GetCtorOf(concreatePipelineType);
                ParameterInfo ctorPipeParam = GetPipelineCtorParameter(concreatePipelineType, ctor);
                var serviceproviderParameter = Expression.Parameter(typeof(IServiceProvider), "provider");

                MethodCallExpression ctorParamExpression = BuildResolvePipeCtorParamExpression(definition.Root, ctorPipeParam, piplineGenericArguments, serviceproviderParameter);
                var pipelineRegisterExpression = Expression.Lambda(
                    Expression.New(ctor, ctorParamExpression),
                    serviceproviderParameter);

                services.AddScoped(definition.PipelineType, sp => pipelineRegisterExpression.Compile().DynamicInvoke(sp)!);
                services.RegisterPipes(definition.Root, piplineGenericArguments);
            }

            return services;

            static IPipelineDefinition CreatePipelineDefinition(Type pipelineDefinitionType)
            {
                if (Activator.CreateInstance(pipelineDefinitionType) is not IPipelineDefinition definition)
                    throw new InvalidCastException($"Тип {pipelineDefinitionType.FullName} не реализует {typeof(IPipelineDefinition).FullName}");

                return definition;
            }

            static Type[] GetPipelineGenericArgumets(IPipelineDefinition definition)
            {
                return GetGenericType(definition.PipelineType)?.GetGenericArguments()
                    ?? throw new InvalidCastException($"Тип {definition.PipelineType.FullName} не является обощенным типом " +
                    $"и не реализует обощенный тип.");
            }

            static ParameterInfo GetPipelineCtorParameter(Type concreatePipelineType, ConstructorInfo ctor)
            {
                var ctorParameters = ctor.GetParameters();
                if (ctorParameters.Length == 0 || ctorParameters.All(pi => GetGenericType(pi.ParameterType)?.GetGenericTypeDefinition() != genericPipe))
                    throw new InvalidDataException($"Конструктор тип {concreatePipelineType.FullName} должен иметь как " +
                        $"минимум 1 параметр типа {genericPipe.FullName}");

                return ctorParameters.First();
            }
        }

        private static ConstructorInfo GetCtorOf(Type? type)
        {
            ArgumentNullException.ThrowIfNull(type, nameof(type));

            var ctors = type.GetConstructors();
            if (ctors.Length != 1)
            {
                throw new Exception($"Тип {type.FullName} должен иметь строго 1 публичный конструктор");
            }
            var ctor = ctors[0];
            return ctor;
        }

        private static bool IsImplement(this Type? type, Type baseType)
        {
            if (type == null)
                return false;

            if (type.IsGenericType && type.GetGenericTypeDefinition() == baseType)
                return true;

            if (type.BaseType == baseType)
                return true;

            return IsImplement(type.BaseType, baseType);
        }

        private static void RegisterPipes(this IServiceCollection services, PipeNode? root, Type[] piplineGenericArguments)
        {
            // граничное условия окончания рекурсии
            if (root == null)
                return;

            var serviceProviderParameter = Expression.Parameter(typeof(IServiceProvider), "provider");
            if (createPipeExpressionMethodIndo.MakeGenericMethod(piplineGenericArguments)
                .Invoke(null, [root, piplineGenericArguments, serviceProviderParameter]) is not Expression createPipeExpression)
                throw new Exception($"Полученное значение не явлется производным от {typeof(Expression)}");

            var registerExpression = Expression.Lambda(
                createPipeExpression,
                serviceProviderParameter);

            if (registerExpression is not LambdaExpression registerLambdaExpression)
            {
                throw new InvalidCastException($"Для типа {root.PipeType!.FullName} дерево выражений не является лямбдой.");
            }
            services.AddKeyedScoped(root.PipeType!, root.UniqueId, (sp, _) => registerLambdaExpression.Compile().DynamicInvoke(sp)!);

            foreach (var altNode in root.Children)
            {
                services.RegisterPipes(altNode, piplineGenericArguments);
            }
        }

        private static Expression BuildResolveCtorParamExpression(PipeNode root, ParameterInfo ctorParamInfo, Type[] piplineGenericArguments, ParameterExpression provider)
        {
            var isPipeParam = ctorParamInfo.ParameterType.IsImplement(genericPipe);
            if (isPipeParam)
            {
                return BuildResolvePipeCtorParamExpression(root, ctorParamInfo, piplineGenericArguments, provider);
            }

            // кэшируем всё, кроме IPipe<TRequest, TResponse>
            if(!expressionCache.TryGetValue(ctorParamInfo.ParameterType, out var paramExpression))
            { 
                paramExpression = Expression.Call(
                    resolveDependencyMethodInfo.MakeGenericMethod(ctorParamInfo.ParameterType),
                    provider);

                expressionCache[ctorParamInfo.ParameterType] = paramExpression;
            }

            return paramExpression;
        }

        /// <summary>
        /// Формирует Expression tree разрешения зависимости типа IPipe<,>.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="ctorPipeParamInfo"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        private static MethodCallExpression BuildResolvePipeCtorParamExpression(PipeNode? root, ParameterInfo ctorPipeParamInfo, Type[] pipelineGenericArguments, ParameterExpression provider)
        {
            ArgumentNullException.ThrowIfNull(root, nameof(root));

            return Expression.Call(
                resolvePipeDependencyMethodInfo.MakeGenericMethod(pipelineGenericArguments),
                Expression.Constant(root.PipeType, typeof(Type)),
                Expression.Constant(root.UniqueId, typeof(object)),
                provider);
        }

        private static Expression? CreatePipeExpression<TRequest, TResponse>(PipeNode pipeNode, Type[] piplineGenericArguments, ParameterExpression provider)
            where TRequest : class
            where TResponse : class
        {
            return pipeNode switch
            {
                IfElsePipeNode<TRequest, TResponse> ifElsePipeNode => CreateNewIfElsePipeExpression(ifElsePipeNode, piplineGenericArguments, provider),
                IfPipeNode<TRequest, TResponse> ifPipeNode => CreateNewIfPipeExpression(ifPipeNode, piplineGenericArguments, provider),
                AlterPipeNode<TRequest, TResponse> alterPipeNode => CreateNewAlterPipeExpression(alterPipeNode, piplineGenericArguments, provider),
                _ => CreatePipeNewExpression(pipeNode, piplineGenericArguments, provider)
            };

            static Expression CreateNewIfElsePipeExpression(IfElsePipeNode<TRequest, TResponse> ifElsePipeNode, Type[] piplineGenericArguments, ParameterExpression provider)
            {
                var ctor = GetCtorOf(ifElsePipeNode.PipeType);
                var ctorParams = ctor.GetParameters();

                return Expression.New(
                    ctor,
                    Expression.Constant(ifElsePipeNode.Predicate, concreatePredicateType),
                    BuildResolvePipeCtorParamExpression(ifElsePipeNode.Positive, ctorParams[1], piplineGenericArguments, provider),
                    BuildResolvePipeCtorParamExpression(ifElsePipeNode.Alternative, ctorParams[2], piplineGenericArguments, provider),
                    BuildResolvePipeCtorParamExpression(ifElsePipeNode.Next, ctorParams[3], piplineGenericArguments, provider));
            }

            static Expression CreateNewAlterPipeExpression(AlterPipeNode<TRequest, TResponse> alterPipeNode, Type[] piplineGenericArguments, ParameterExpression provider)
            { 
                var ctor = GetCtorOf(alterPipeNode.PipeType);
                var ctorParams = ctor.GetParameters();

                return Expression.New(
                    ctor,
                    Expression.Constant(alterPipeNode.Predicate, concreatePredicateType),
                    BuildResolvePipeCtorParamExpression(alterPipeNode.Positive, ctorParams[1], piplineGenericArguments, provider),
                    BuildResolvePipeCtorParamExpression(alterPipeNode.Next, ctorParams[2], piplineGenericArguments, provider));
            }

            static Expression CreateNewIfPipeExpression(IfPipeNode<TRequest, TResponse> ifPipeNode, Type[] piplineGenericArguments, ParameterExpression provider)
            {
                var ctor = GetCtorOf(ifPipeNode.PipeType);
                var ctorParams = ctor.GetParameters();

                return Expression.New(
                    ctor,
                    Expression.Constant(ifPipeNode.Predicate, concreatePredicateType),
                    BuildResolvePipeCtorParamExpression(ifPipeNode.Positive, ctorParams[1], piplineGenericArguments, provider),
                    BuildResolvePipeCtorParamExpression(ifPipeNode.Next, ctorParams[2], piplineGenericArguments, provider));
            }

            static Expression CreatePipeNewExpression(PipeNode pipeNode, Type[] piplineGenericArguments, ParameterExpression provider)
            {
                var ctor = GetCtorOf(pipeNode.PipeType);
                var ctorParams = ctor.GetParameters();
                var paramExpressions = new List<Expression>(ctorParams.Length);
                foreach (var ctorParam in ctorParams)
                {
                    var dependecyResolutionPipeNode = ctorParam.ParameterType.IsImplement(genericPipe) 
                        // поскольку pipeNode не является ветвлением конвейера и имеет зависимость от IPipe<,>,
                        // то эта зависимость всегда следующий pipe типа pipeNode.Next
                        ? pipeNode.Next
                        : pipeNode;
                    if (dependecyResolutionPipeNode == null)
                        throw new InvalidOperationException("Pipe разрешения зависимости неожаданно равен null");

                    paramExpressions.Add(BuildResolveCtorParamExpression(dependecyResolutionPipeNode, ctorParam, piplineGenericArguments, provider));
                }

                return Expression.New(ctor, paramExpressions);
            }
        }

        private static Type? GetGenericType(Type sourceType)
        { 
            if(sourceType.IsGenericType)
                return sourceType;

            if (sourceType.BaseType == null || sourceType.BaseType == typeof(object))
                return null;

            return GetGenericType(sourceType.BaseType);
        }

        private static TObject ResolveDependency<TObject>(IServiceProvider serviceProvider)
            where TObject : class
        {
            return serviceProvider.GetRequiredService<TObject>();
        }

        private static IPipe<TRequest, TResponse> ResolvePipeDependency<TRequest, TResponse>(Type pipeType, object? serviceKey, IServiceProvider serviceProvider)
            where TRequest : class
            where TResponse : class
        {
            object pipeObject = serviceProvider.GetRequiredKeyedService(pipeType, serviceKey);

            if (pipeObject is IPipe<TRequest, TResponse> pipe)
                return pipe;

            throw new InvalidOperationException($"Тип {pipeType.FullName} не реализует {genericPipe.FullName}.");
        }
    }
}
