using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.SimpleInjector;
using SimpleInjector;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
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
                        .Transport(t => t.UseSqlServer(connectionString, "MyMessages"))
                        .Start()
                );
                container.Verify();

                var publisher = container.GetInstance<IDateTimePublisher>();
                using (var timer = new Timer(_ => publisher.PublishDateTime().Wait(), null, TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(5.0)))
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
