using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared
{
    public interface IServiceEventSource
    {
        void ServiceMessage(StatelessServiceContext serviceContext, string message, params object[] args);
    }
}
