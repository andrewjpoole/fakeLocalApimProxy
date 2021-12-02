using System.Text;

namespace FakeLocalApimProxy
{
    public static class HttpRequesteExtensions 
    {
        public static string ToRequstString(this HttpRequest request)
        {
            return $"{request.Scheme}://{request.Host}{request.Path}{request.QueryString.Value}";
        }

        public static string LogResponse(this HttpResponseMessage response)
        {
            var logOutput = new StringBuilder();
            logOutput.AppendLine($"statusCode:");
            logOutput.AppendLine($"   {(int)response.StatusCode} {response.StatusCode}");
            logOutput.AppendLine("headers:");
            foreach (var (key, value) in response.Headers)
            {
                logOutput.AppendLine($"   {key}:{string.Join(",", value)}");
            }

            logOutput.AppendLine("body:");
            logOutput.AppendLine($"   {response.Content.ReadAsStringAsync().Result}");
            

            return logOutput.ToString();
        }
    }
}
