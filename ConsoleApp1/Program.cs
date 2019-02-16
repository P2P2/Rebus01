using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Handlers;

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

            using (var activator = new BuiltinHandlerActivator())
            {
                activator.Register(() => new PrintDateTime());

                var b = activator.Bus;

                Configure.With(activator)
                    .Transport(t => t.UseSqlServer(connectionString, "MyMessages"))
                    .Start();

                using (var timer = new Timer(_ => activator.Bus.SendLocal(DateTime.Now).Wait(), null, TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(5.0)))
                {
                    Console.ReadLine();
                }
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
