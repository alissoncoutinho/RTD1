namespace Barragem.Helper
{
    public static class StringHelper
    {
        public static string ToHttp(this string url)
        {
            if (string.IsNullOrEmpty(url))
                return url;
            if (url.Contains("http:") || url.Contains("https:"))
                return url;
            return $"http://{url}";
        }
    }
}