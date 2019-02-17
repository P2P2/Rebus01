using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Layout;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Routing.TypeBased;
using Rebus.SimpleInjector;
using Shared01;
using SimpleInjector;

namespace Server01
{
    class Program
    {
        static void Main(string[] args)
        {
            var consoleLayout = new PatternLayout(@"SERVER %date{HH:mm:ss.fff} [%thread] %message%newline%exception");
            consoleLayout.ActivateOptions();

            var consoleAppender = new ConsoleAppender
            {
                Layout = consoleLayout,
                Threshold = Level.Info
            };
            consoleAppender.ActivateOptions();

            BasicConfigurator.Configure(consoleAppender);

            var connectionString = new SqlConnectionStringBuilder
            {
                DataSource = @"(localdb)\MSSQLLocalDb",
                InitialCatalog = @"RebusTest",
                IntegratedSecurity = true,
            }.ToString();

            using (var container = new Container())
            {
                container.Collection.Register<IHandleMessages<MyMessage>>(typeof(Server));
                container.Collection.Register<IHandleMessages<SampleForTiming>>(new TimingServer());
                container.ConfigureRebus(
                    c => c
                        .Logging(l => l.Log4Net())
                        .Transport(t => t.UseSqlServer(connectionString, "Shared01"))
                        .Routing(r => r.TypeBased()
                            .Map<MyMessage>("Shared01"))
                        .Start()
                );
                container.Verify();

                //var bus = container.GetInstance<IBus>();
                //await bus.Subscribe<MyMessage>();

                Console.ReadLine();
            }
        }

        public class Server : IHandleMessages<MyMessage>
        {
            private static readonly log4net.ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

            public Task Handle(MyMessage message)
            {
                log.Info($"Received {message.Value}");
                return Task.CompletedTask;
            }
        }

        public class TimingServer : IHandleMessages<SampleForTiming>
        {
            private static readonly log4net.ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
            private readonly Stopwatch _sw = new Stopwatch();
            private int _count = 0;

            public Task Handle(SampleForTiming message)
            {
                if (message.StartTiming)
                {
                    _sw.Reset();
                    _sw.Start();
                    _count = 0;
                }

                Interlocked.Increment(ref _count);

                if (message.StopTiming)
                {
                    _sw.Stop();
                    log.Info($"Received {_count:n0} messages in {_sw.ElapsedMilliseconds:n1}ms, {1000.0 / _sw.ElapsedMilliseconds * _count:n0} msgs/sec");
                }

                return Task.CompletedTask;
            }
        }
    }
}
