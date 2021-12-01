using Microsoft.Extensions.Primitives;

namespace FakeLocalApimProxy
{
    public class Proxy : IMiddleware
    {
        private const string RedirectionSectionName = "Redirections";
        private List<Redirection> _redirections = new();

        public Proxy(IConfiguration config)
        {
            _redirections = config.GetSection(RedirectionSectionName).Get<List<Redirection>>();

            Console.WriteLine($"Found {_redirections.Count} Redirections in config.");
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var request = context.Request;
            (bool matches, Redirection redirection) = MatchesRedirection(request);
            if (!matches)
            {
                Console.WriteLine($"No Redirection found for request {request.ToRequstString()}");
                await next.Invoke(context);
                return;
            }

            Console.WriteLine($"Forwarding request to {redirection.Uri}");

            var forwardedResponse = await ForwardRequest(request, redirection);

            await CopyResponseMessageToResponse(forwardedResponse, context.Response);
            
            Console.WriteLine("Returned forwarded response.");

            return;
        }

        private (bool Matches, Redirection Redirection) MatchesRedirection(HttpRequest request)
        {
            foreach (var redirection in _redirections)
            {
                if (request.Path.StartsWithSegments(redirection.Match))
                {
                    return (true, redirection);
                }
            }
            return (false, Redirection.Empty());
        }

        private async Task<HttpResponseMessage> ForwardRequest(HttpRequest request, Redirection redirection)
        {
            var requestToForward = DuplicateRequest(request, new Uri(redirection.Uri));
            
            var client = new HttpClient();
            var forwardedResponse = await client.SendAsync(requestToForward);
            return forwardedResponse;            
        }

        private async Task CopyResponseMessageToResponse(HttpResponseMessage forwardedResponse, HttpResponse response)
        {
            foreach (var forwardedHeader in forwardedResponse.Headers)
            {
                response.Headers.Add(forwardedHeader.Key, new StringValues(forwardedHeader.Value.ToArray()));
            }
            //response.Body = forwardedResponse.Content.ReadAsStream();
            await forwardedResponse.Content.CopyToAsync(response.Body, null, CancellationToken.None);
        }

        private HttpRequestMessage DuplicateRequest(HttpRequest request, Uri uri)
        {
            var requestMessage = new HttpRequestMessage();
            var requestMethod = request.Method;
            if (!HttpMethods.IsGet(requestMethod) &&
                !HttpMethods.IsHead(requestMethod) &&
                !HttpMethods.IsDelete(requestMethod) &&
                !HttpMethods.IsTrace(requestMethod))
            {
                var streamContent = new StreamContent(request.Body);
                requestMessage.Content = streamContent;
            }
            
            foreach (var header in request.Headers)
            {
                if (!requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()) && requestMessage.Content != null)
                {
                    requestMessage.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                }
            }

            requestMessage.Headers.Host = uri.Authority;
            requestMessage.RequestUri = uri;
            requestMessage.Method = new HttpMethod(request.Method);

            return requestMessage;
        }        
    }
}
