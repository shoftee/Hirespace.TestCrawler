using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Hirespace.TestCrawler
{
    internal class Crawler : IDisposable
    {
        private static readonly CrawlerResult[] EmptyCrawlerResults = new CrawlerResult[0];

        private readonly HttpClient _httpClient;

        public Crawler()
        {
            _httpClient = new HttpClient();
        }

        public async Task<IEnumerable<CrawlerResult>> Crawl(Uri uri, CancellationToken cancellationToken)
        {
            var response = await _httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            var contentType = response.Content.Headers.ContentType;
            if (!contentType.MediaType.StartsWith("text/html"))
            {
                // Ignore non-HTML content.
                return EmptyCrawlerResults;
            }

            var html = await response.Content.ReadAsStringAsync();
            return CrawlHtml(uri, html);
        }

        private static readonly Regex SimpleRelativePathPattern =
            new Regex(
                @"href=""(?<Path>(/([\w-._~!$&'()*+,;=:]|(%[a-f0-9]{2}))*)+)""",
                RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);

        private static IEnumerable<CrawlerResult> CrawlHtml(Uri originalUri, string html)
        {
            var baseUriString = originalUri.GetLeftPart(UriPartial.Authority);
            var baseUri = new Uri(baseUriString, UriKind.Absolute);
            var matches = SimpleRelativePathPattern.Matches(html);
            foreach (Match match in matches)
            {
                var path = match.Groups["Path"].Value;
                var resultUri = new Uri(baseUri, path);
                yield return new CrawlerResult(resultUri);
            }
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}
