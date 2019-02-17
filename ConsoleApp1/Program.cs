using System;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Layout;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.SimpleInjector;
using SimpleInjector;

namespace ConsoleApp1
{
    class Program
    {
        static void Main()
        {
            var consoleLayout = new PatternLayout(@"%date{HH:mm:ss.fff} [%thread] %message%newline%exception");
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
                container.Collection.Register<IHandleMessages<DateTime>>(typeof(PrintDateTime));
                container.Register<IDateTimePublisher, Publisher>(Lifestyle.Singleton);
                container.ConfigureRebus(
                    c => c
                        .Logging(l => l.Log4Net())
                        .Transport(t => t.UseSqlServer(connectionString, "MyMessages"))
                        .Start()
                );
                container.Verify();

                var publisher = container.GetInstance<IDateTimePublisher>();
                using (new Timer(_ => publisher.PublishDateTime().Wait(), null, TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(5.0)))
                {
                    Console.ReadLine();
                }
            }
        }

        public interface IDateTimePublisher
        {
            Task PublishDateTime();
        }

        public class Publisher : IDateTimePublisher
        {
            private readonly IBus _bus;

            public Publisher(IBus bus)
            {
                _bus = bus;
            }

            public Task PublishDateTime()
            {
                return _bus.SendLocal(DateTime.Now);
            }
        }

        public class PrintDateTime : IHandleMessages<DateTime>
        {
            public Task Handle(DateTime message)
            {
                Console.WriteLine($"\n{message:G}");
                return Task.CompletedTask;
            }
        }
    }
}
