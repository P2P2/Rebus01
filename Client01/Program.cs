using System.Data.SqlClient;
using System.Reflection;
using System.Threading.Tasks;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Layout;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Routing.TypeBased;
using Rebus.SimpleInjector;
using Shared01;
using SimpleInjector;

namespace Client01
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var consoleLayout = new PatternLayout(@"CLIENT %date{HH:mm:ss.fff} [%thread] %message%newline%exception");
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
                container.Register<IClient, Client>(Lifestyle.Singleton);
                container.ConfigureRebus(
                    c => c
                        .Logging(l => l.Log4Net())
                        .Transport(t => t.UseSqlServerAsOneWayClient(connectionString))
                        .Routing(r => r.TypeBased()
                            .Map<MyMessage>("Shared01"))
                        .Start()
                );
                container.Verify();

                var client = container.GetInstance<IClient>();
                await client.Run();
            }
        }

        public interface IClient
        {
            Task Run();
        }

        public class Client : IClient
        {
            private static readonly log4net.ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
            private readonly IBus _bus;

            public Client(IBus bus)
            {
                _bus = bus;
            }

            public async Task Run()
            {
                for (var i = 0; i < 10; ++i)
                {
                    log.Debug($"Sending {i}");
                    await _bus.Send(new MyMessage {Value = i});
                    await Task.Delay(i * 100);
                }
            }
        }
    }
}
