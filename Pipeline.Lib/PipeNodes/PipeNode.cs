﻿using Pipeline.Lib.Abstraction;

namespace Pipeline.Lib.PipeNodes
{
    public class PipeNode
    {
        private readonly string _uniqueId;
        public PipeNode(Type? pipeType)
        {
            PipeType = pipeType;
            _uniqueId = Guid.NewGuid().ToString();
        }

        public PipeNode(Type? pipeType, PipeNode next) : this(pipeType)
        {
            Next = next;
        }

        /// <summary>
        /// Уникальный идентификатор нода. Используется при 
        /// регистрации зависимостей.
        /// </summary>
        public string UniqueId => _uniqueId;

        /// <summary>
        /// Тип <see cref="IPipe{TRequest, TResponse}"/>
        /// </summary>
        public Type? PipeType { get; protected set; }

        /// <summary>
        /// Следующий узел.
        /// </summary>
        public PipeNode? Next { get; set; }

        /// <summary>
        /// Узлы, входящие в состав узла, кроме <see cref="Next"/>.
        /// </summary>
        public virtual IEnumerable<PipeNode> Children 
        {
            get 
            {
                if(Next != null)
                    yield return Next;

                yield break;
            }
        }
    }
}
