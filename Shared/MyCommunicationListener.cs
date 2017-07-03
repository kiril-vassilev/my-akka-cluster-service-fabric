using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Configuration;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Shared.Actors;

namespace Shared
{
    public class MyCommunicationListener : ICommunicationListener
    {
        private const string AkkaSection = @"
                akka {
                    actor {
                      provider = ""Akka.Cluster.ClusterActorRefProvider, Akka.Cluster""
                    }

                    remote {
                        dot-netty.tcp {
                        hostname = ""localhost""
                        port = 0
                        }
                    }
                    cluster {
                        seed-nodes = [
                            ""akka.tcp://ClusterSystem@localhost:2551"",
                            ""akka.tcp://ClusterSystem@localhost:2552""]

                        #auto-down-unreachable-after = 30s
                    }
                }";


        private readonly StatelessServiceContext _statelessServiceContext;
        private readonly IServiceEventSource _currentEventSource;
        private readonly int _seadNo;

        private ActorSystem actorSystem;


        public MyCommunicationListener(StatelessServiceContext context, IServiceEventSource eventSource, int seadNo)
        {
            _statelessServiceContext = context;
            _currentEventSource = eventSource;
            _seadNo = seadNo;
        }

        public Task<string> OpenAsync(CancellationToken cancellationToken)
        {
            var endpoint = _statelessServiceContext.CodePackageActivationContext.GetEndpoint( _seadNo == 0? "ServiceEndpoint" : "ServiceEndpoint" + _seadNo);

            var config =
                ConfigurationFactory
                    .ParseString("akka.remote.dot-netty.tcp.port=" + endpoint.Port)
                    .WithFallback(AkkaSection);


            actorSystem = ActorSystem.Create("ClusterSystem", config);
            
            actorSystem.ActorOf(Props.Create(() => new SimpleClusterActor(_statelessServiceContext, _currentEventSource)), "simpleClusterActor");

            var url = $"akka.tcp://{actorSystem.Name}@{_statelessServiceContext.NodeContext.IPAddressOrFQDN}:{endpoint.Port}";

            return Task.FromResult(url);
        }


        public Task CloseAsync(CancellationToken cancellationToken)
        {
            actorSystem?.Terminate().Wait(cancellationToken);
            actorSystem?.Dispose();

            return Task.FromResult(0);
        }

        public void Abort()
        {
            actorSystem?.Dispose();
        }
    }
}
