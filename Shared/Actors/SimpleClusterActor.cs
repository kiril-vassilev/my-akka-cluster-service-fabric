using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Fabric;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Cluster;
using Akka.Event;

namespace Shared.Actors
{
    public class SimpleClusterActor : UntypedActor
    {
        //protected ILoggingAdapter Log = Context.GetLogger();
        protected Akka.Cluster.Cluster Cluster = Akka.Cluster.Cluster.Get(Context.System);

        private readonly StatelessServiceContext _statelessServiceContext;
        private readonly IServiceEventSource _currentEventSource;
        


        public SimpleClusterActor(StatelessServiceContext context, IServiceEventSource eventSource)
        {
            _statelessServiceContext = context;
            _currentEventSource = eventSource;

            
        }


        protected override void PreStart()
        {
            Cluster.Subscribe(Self, ClusterEvent.InitialStateAsEvents,
                new[] {typeof(ClusterEvent.IMemberEvent), typeof(ClusterEvent.UnreachableMember)});
        }

        protected override void PostStop()
        {
            Cluster.Unsubscribe(Self);
        }

        protected override void OnReceive(object message)
        {
            var up = message as ClusterEvent.MemberUp;
            if (up != null)
            {
                var mem = up;
                _currentEventSource.ServiceMessage(_statelessServiceContext, "Member is Up: {0}", mem.Member);

            }
            else if (message is ClusterEvent.MemberJoined)
            {
                var mem = (ClusterEvent.MemberJoined) message;
                _currentEventSource.ServiceMessage(_statelessServiceContext, "Member joined the cluster: {0}", mem.Member);
            }
            else if (message is ClusterEvent.UnreachableMember)
            {
                var unreachable = (ClusterEvent.UnreachableMember) message;
                _currentEventSource.ServiceMessage(_statelessServiceContext, "Member detected as unreachable: {0}", unreachable.Member);
            }
            else if (message is ClusterEvent.MemberRemoved)
            {
                var removed = (ClusterEvent.MemberRemoved) message;
                _currentEventSource.ServiceMessage(_statelessServiceContext, "Member is Removed: {0}", removed.Member);
            }
            else if (message is ClusterEvent.IMemberEvent)
            {
                //IGNORE                
            }
            else if (message is ClusterEvent.LeaderChanged)
            {
                var state = (ClusterEvent.LeaderChanged) message;
                _currentEventSource.ServiceMessage(_statelessServiceContext, "The leader is: {0}", state.Leader.ToString());
            }
            else
            {
                Unhandled(message);
            }
        }
    }
}

