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

        private static readonly MethodInfo createIfPipeExpression = typeof(ServiceCollectionExtensions).GetMethod(
            nameof(CreateIfPipeExpression),
            2,
            BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public,
            binder: null,
            [typeof(PipeNode), typeof(Type[])],
            modifiers: null)!;

        private static readonly MethodInfo getPridicateMethodInfo = typeof(ServiceCollectionExtensions).GetMethod(
            nameof(GetPridicate),
            2,
            BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public,
            binder: null,
            [typeof(PipeNode)],
            modifiers: null)!;

        private static readonly MethodInfo getPositiveNodeMethodInfo = typeof(ServiceCollectionExtensions).GetMethod(
            nameof(GetPositiveNode),
            2,
            BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public,
            binder: null,
            [typeof(PipeNode)],
            modifiers: null)!;

        private static readonly MethodInfo getAltNodeMethodInfo = typeof(ServiceCollectionExtensions).GetMethod(
            nameof(GetAltNode),
            2,
            BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public,
            binder: null,
            [typeof(PipeNode)],
            modifiers: null)!;

        private static readonly Type basePipeline = typeof(Pipeline<,>);

        private static readonly Type ifPipeType = typeof(IfPipe<,>);
        private static readonly Type ifElsePipeType = typeof(IfElsePipe<,>);

        private static readonly Type ifPipeNodeType = typeof(IfPipeNode<,>);
        private static readonly Type ifElsePipeNodeType = typeof(IfElsePipeNode<,>);
        private static Type pipelineContextType = typeof(PipelineContext<,>);
        private static Type predicateType = typeof(Predicate<>);

        public static IServiceCollection AddPipelines(this IServiceCollection services, Assembly assembly)
        {
            var pipelineDefinitionTypes = assembly.GetExportedTypes()
                .Where(t => !t.IsAbstract && t.IsClass && IsImplement(t, typeof(PipelineDefinition<,>)))
                .ToList();

            foreach (var pipelineDefinitionType in pipelineDefinitionTypes)
            {
                IPipelineDefinition? definition = CreatePipelineDefinition(pipelineDefinitionType);
                Type[] piplineGenericArguments = GetPipelineGenericArgumets(definition);

                pipelineContextType = typeof(PipelineContext<,>).MakeGenericType(piplineGenericArguments);
                predicateType = typeof(Predicate<>).MakeGenericType(pipelineContextType);

                Type concreatePipelineType = basePipeline.MakeGenericType(piplineGenericArguments);

                ConstructorInfo ctor = GetCtorOf(concreatePipelineType);
                ParameterInfo ctorPipeParam = GetPipelineCtorParameter(concreatePipelineType, ctor);

                var pipelineRegisterExpression = Expression.Lambda(
                    Expression.New(ctor, BuildResolvePipeCtorParamExpression(definition.Root, ctorPipeParam, piplineGenericArguments)),
                    Expression.Parameter(typeof(IServiceProvider), "provider"));

                services.AddScoped(definition.PipelineType, sp => pipelineRegisterExpression.Compile().DynamicInvoke(sp)!);
                services.RegisterPipes(definition.Root, piplineGenericArguments);
            }

            return services;

            static IPipelineDefinition CreatePipelineDefinition(Type pipelineDefinitionType)
            {
                var definition = Activator.CreateInstance(pipelineDefinitionType) as IPipelineDefinition;
                if (definition == null)
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

            var ctor = GetCtorOf(root.PipeType);
            ParameterInfo[] ctorParamInfos = ctor.GetParameters().ToArray();

            var createPipeExpression = createIfPipeExpression.MakeGenericMethod(piplineGenericArguments)
                .Invoke(null, [root, piplineGenericArguments]) as Expression;
            if (createPipeExpression == null)
                throw new Exception($"Полученное значение не явлется производным от {typeof(Expression)}");

            //var ctorParamExpressions = new List<Expression>(ctorParamInfos.Length);
            //foreach (ParameterInfo ctorParamInfo in ctorParamInfos)
            //{
            //    Expression paramExpression = BuildResolveCtorParamExpression(root, ctorParamInfo, piplineGenericArguments);
            //    ctorParamExpressions.Add(paramExpression);
            //}

            var registerExpression = Expression.Lambda(
                createPipeExpression,
                Expression.Parameter(typeof(IServiceProvider), "provider"));

            if (registerExpression is not LambdaExpression registerLambdaExpression)
            {
                throw new InvalidCastException($"Для типа {root.PipeType!.FullName} дерево выражений не является лямбдой.");
            }
            services.AddKeyedScoped(root.PipeType!, root.UniqueId, (sp, _) => registerLambdaExpression.Compile().DynamicInvoke(sp)!);

            // поскольку root может быть узлом развилки логики, то надо зарегистрировать
            // ветки положительного пути и альтернативного пути.
            PipeNode[] altNodes = GetAlternativePipeNodes(root, piplineGenericArguments);
            foreach (var altNode in altNodes)
            {
                services.RegisterPipes(altNode, piplineGenericArguments);
            }

            services.RegisterPipes(root.Next, piplineGenericArguments);
        }

        private static PipeNode[] GetAlternativePipeNodes(PipeNode root, Type[] piplineGenericArguments)
        {
            var positivePipe = getPositiveNodeMethodInfo.MakeGenericMethod(piplineGenericArguments).Invoke(null, [root]) as PipeNode;
            if(positivePipe == null)
                return Array.Empty<PipeNode>();

            var pipeNodes = new List<PipeNode>(2)
            {
                positivePipe
            };
            var altPipe = getAltNodeMethodInfo.MakeGenericMethod(piplineGenericArguments).Invoke(null, [root]) as PipeNode;
            if (altPipe != null)
                pipeNodes.Add(altPipe);

            return pipeNodes.ToArray();
        }

        private static Expression BuildResolveCtorParamExpression(PipeNode root, ParameterInfo ctorParamInfo, Type[] piplineGenericArguments)
        {
            var isPipeParam = ctorParamInfo.ParameterType.IsImplement(genericPipe);
            if (isPipeParam)
            {
                return BuildResolvePipeCtorParamExpression(root, ctorParamInfo, piplineGenericArguments);
            }

            // кэшируем всё, кроме IPipe<TRequest, TResponse>
            if(!expressionCache.TryGetValue(ctorParamInfo.ParameterType, out var paramExpression))
            { 
                paramExpression = Expression.Call(
                    resolveDependencyMethodInfo.MakeGenericMethod(ctorParamInfo.ParameterType),
                    Expression.Parameter(typeof(IServiceProvider), "provider"));

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
        /// <exception cref="Exception"></exception>
        private static Expression BuildResolvePipeCtorParamExpression(PipeNode? root, ParameterInfo ctorPipeParamInfo, Type[] piplineGenericArguments)
        {
            var nextPipeType = root.Next.PipeType;
                // если цепочка pipe`ов закончилась, а pipe имеет зависимость от IPipe<,>, то добавляем заглушку.

            return Expression.Call(
                    resolvePipeDependencyMethodInfo.MakeGenericMethod(piplineGenericArguments),
                    Expression.Constant(root.Next.PipeType, typeof(Type)),
                    Expression.Constant(root.Next.UniqueId, typeof(object)),
                    Expression.Parameter(typeof(IServiceProvider), "provider"));
        }

        private static Expression? CreateIfPipeExpression<TRequest, TResponse>(PipeNode pipeNode, Type[] piplineGenericArguments)
            where TRequest : class
            where TResponse : class
        {
            var ctor = GetCtorOf(pipeNode.PipeType);
            var ctorParams = ctor.GetParameters();
            return pipeNode switch
            {
                IfElsePipeNode<TRequest, TResponse> ifElsePipeNode =>
                    Expression.New(ctor, 
                        Expression.Constant(ifElsePipeNode.Predicate, predicateType.MakeGenericType(pipelineContextType.MakeGenericType(piplineGenericArguments))),
                        BuildResolvePipeCtorParamExpression(ifElsePipeNode.Positive, ctorParams[1], piplineGenericArguments),
                        BuildResolvePipeCtorParamExpression(ifElsePipeNode.Alternative, ctorParams[2], piplineGenericArguments),
                        BuildResolvePipeCtorParamExpression(ifElsePipeNode.Next, ctorParams[3], piplineGenericArguments)),

                IfPipeNode<TRequest, TResponse> ifPipeNode =>
                    Expression.New(ctor,
                        Expression.Constant(ifPipeNode.Predicate, predicateType.MakeGenericType(pipelineContextType.MakeGenericType(piplineGenericArguments))),
                        BuildResolvePipeCtorParamExpression(ifPipeNode.Positive, ctorParams[1], piplineGenericArguments),
                        BuildResolvePipeCtorParamExpression(ifPipeNode.Next, ctorParams[2], piplineGenericArguments)),
                _ => 
                Expression.New(ctor, ctorParams
                    .Select(pi => BuildResolveCtorParamExpression(pipeNode, pi, piplineGenericArguments))
                    .ToArray())
            };
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

        private static Predicate<PipelineContext<TRequest, TResponse>>? GetPridicate<TRequest, TResponse>(PipeNode pipeNode)
            where TRequest : class
            where TResponse : class
        {
            if (pipeNode is IfPipeNode<TRequest, TResponse> ifPipeNode)
            {
                return ifPipeNode.Predicate;
            }
            else if (pipeNode is IfElsePipeNode<TRequest, TResponse> ifElsePipeNode)
            {
                return ifElsePipeNode.Predicate;
            }

            return null;
        }

        private static PipeNode? GetPositiveNode<TRequest, TResponse>(PipeNode pipeNode)
            where TRequest : class
            where TResponse : class
        {
            if (pipeNode is IfPipeNode<TRequest, TResponse> ifPipeNode)
            {
                return ifPipeNode.Positive;
            }
            else if (pipeNode is IfElsePipeNode<TRequest, TResponse> ifElsePipeNode)
            {
                return ifElsePipeNode.Positive;
            }

            return null;
        }

        private static PipeNode? GetAltNode<TRequest, TResponse>(PipeNode pipeNode)
            where TRequest : class
            where TResponse : class
        {
            if (pipeNode is IfPipeNode<TRequest, TResponse> ifPipeNode)
            {
                return null;
            }
            else if (pipeNode is IfElsePipeNode<TRequest, TResponse> ifElsePipeNode)
            {
                return ifElsePipeNode.Alternative;
            }

            return null;
        }
    }
}
