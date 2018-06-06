using System;

namespace Hirespace.TestCrawler
{
    internal class CrawlerResult : IEquatable<CrawlerResult>
    {
        public Uri Uri { get; }

        public CrawlerResult(Uri uri)
        {
            var trimmedUriString = uri.GetLeftPart(UriPartial.Path);
            Uri = new Uri(trimmedUriString, UriKind.Absolute);
        }

        public bool Equals(CrawlerResult other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Uri.Equals(other.Uri);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((CrawlerResult) obj);
        }

        public override int GetHashCode()
        {
            return Uri.GetHashCode();
        }
    }
}
