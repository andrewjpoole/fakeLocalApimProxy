using System.Text;

namespace FakeLocalApimProxy
{
    public static class HttpRequesteExtensions 
    {
        public static string ToRequstString(this HttpRequest request)
        {
            return $"{request.Scheme}://{request.Host}{request.Path}{request.QueryString.Value}";
        }

        public static string LogResponseHeaders(this HttpResponseMessage response)
        {
            var headers = new StringBuilder("headers:");
            foreach (var (key, value) in response.Headers)
            {
                headers.AppendLine($"{key}:{string.Join(",", value)}");
            }
            return headers.ToString();
        }
    }
}
