﻿using System;
using System.IO;
using System.Threading.Tasks;
using Lykke.Common;
using Microsoft.AspNetCore.Hosting;

namespace Lykke.Job.TradesConverter
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine($"{AppEnvironment.Name} version {AppEnvironment.Version}");
#if DEBUG
            Console.WriteLine("Is DEBUG");
#else
            Console.WriteLine("Is RELEASE");
#endif
            Console.WriteLine($"ENV_INFO: {Environment.GetEnvironmentVariable("ENV_INFO")}");

            try
            {
                var hostBuilder = new WebHostBuilder()
                    .UseKestrel()
                    .UseUrls("http://*:5000")
                    .UseContentRoot(Directory.GetCurrentDirectory())
                    .UseStartup<Startup>();
#if !DEBUG
                hostBuilder = hostBuilder.UseApplicationInsights();
#endif
                var webHost = hostBuilder.Build();

                webHost.Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fatal error:");
                Console.WriteLine(ex);

                // Lets devops to see startup error in console between restarts in the Kubernetes
                var delay = TimeSpan.FromMinutes(1);

                Console.WriteLine();
                Console.WriteLine($"Process will be terminated in {delay}. Press any key to terminate immediately.");

                Task.WhenAny(
                        Task.Delay(delay),
                        Task.Run(() =>
                        {
                            Console.ReadKey(true);
                        }))
                    .Wait();
            }

            Console.WriteLine("Terminated");
        }
    }
}
