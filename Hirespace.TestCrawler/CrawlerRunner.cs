using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Hirespace.TestCrawler
{
    internal class CrawlerRunner
    {
        private readonly Func<Uri, bool> _validateUri;

        private readonly ConcurrentHashSet<CrawlerResult> _results;
        private readonly ConcurrentQueue<Uri> _queue;

        public TimeSpan TimeoutInterval { get; set; }
        public TimeSpan IdleInterval { get; set; }

        public CrawlerRunner(Uri initialUri, Func<Uri, bool> validateUri)
        {
            _validateUri = validateUri;
            _results = new ConcurrentHashSet<CrawlerResult>();

            _queue = new ConcurrentQueue<Uri>();
            _queue.Enqueue(initialUri);

            TimeoutInterval = TimeSpan.FromSeconds(5);
            IdleInterval = TimeSpan.FromSeconds(2);
        }

        public async Task Run(CancellationToken cancellationToken)
        {
            using (var crawler = new Crawler())
            {
                int idleCount = 0;
                while (!cancellationToken.IsCancellationRequested)
                {
                    var hasUri = _queue.TryDequeue(out var uri);
                    if (hasUri == false)
                    {
                        if (idleCount < 3)
                        {
                            idleCount++;
                            await Task.Delay(IdleInterval, cancellationToken);
                            continue;
                        }
                        else
                        {
                            Trace.WriteLine("Idle for too long, completing...");
                            break;
                        }
                    }

                    Trace.WriteLine($"Crawling '{uri}' ({_queue.Count} left)...");

                    try
                    {
                        var timeoutCancellationTokenSource = new CancellationTokenSource(TimeoutInterval);
                        var results = await crawler.Crawl(uri, timeoutCancellationTokenSource.Token);

                        int validResults = 0;
                        foreach (var result in results)
                        {
                            if (!_validateUri.Invoke(result.Uri))
                            {
                                // Ignore any URIs that the validation function deems invalid.
                                continue;
                            }

                            if (!_results.Add(result))
                            {
                                // Ignore duplicates.
                                continue;
                            }

                            _queue.Enqueue(result.Uri);
                            validResults++;
                        }

                        Trace.WriteLineIf(validResults > 0, $"Found {validResults} more.");
                    }
                    catch (OperationCanceledException)
                    {
                        Trace.WriteLine("Request timed out.");
                    }
                }
            }
        }
    }
}
