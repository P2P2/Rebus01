using System;
using System.Data.SqlClient;
using System.Reflection;
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
                Threshold = Level.Debug
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
                log.Debug($"Received {message.Value}");
                return Task.CompletedTask;
            }
        }
    }
}
