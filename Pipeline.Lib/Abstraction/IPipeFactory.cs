using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pipeline.Lib.Abstraction
{
    public interface IPipeFactory
    {
        IPipe<TRequest, TResponse> Create<TRequest, TResponse>(Type pipeType)
            where TRequest : class
            where TResponse : class;
    }
}
