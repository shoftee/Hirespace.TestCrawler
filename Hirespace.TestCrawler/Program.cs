using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Hirespace.TestCrawler
{
    internal class Program
    {
        private const int ConcurrencyLevel = 4;
        private static readonly CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();
        private static readonly Uri CrawlUri = new Uri("https://hirespace.com");

        private static void Main(string[] args)
        {
            Trace.Listeners.Add(new ConsoleTraceListener());

            Console.CancelKeyPress += OnCancelKeyPress;

            var crawlerRunner = new CrawlerRunner(CrawlUri, uri => uri.Authority == "hirespace.com");

            var token = CancellationTokenSource.Token;
            var tasks = new Task[ConcurrencyLevel];
            for (int i = 0; i < ConcurrencyLevel; i++)
            {
                tasks[i] = Task.Run(async () => await crawlerRunner.Run(token), token);
            }

            try
            {
                Task.WaitAll(tasks);
            }
            catch (AggregateException aggregateException)
            {
                aggregateException.Handle(HandleOperationCanceled);
            }
        }

        private static bool HandleOperationCanceled(Exception exception)
        {
            return exception is OperationCanceledException operationCanceled
                && operationCanceled.CancellationToken == CancellationTokenSource.Token;
        }

        private static void OnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            CancellationTokenSource.Cancel();
            e.Cancel = true; // Do not terminate immediately.
            Trace.WriteLine("Terminating...");
        }
    }
}
