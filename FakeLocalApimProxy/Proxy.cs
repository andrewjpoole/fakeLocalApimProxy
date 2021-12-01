using Microsoft.Extensions.Primitives;

namespace FakeLocalApimProxy
{
    public class Proxy : IMiddleware
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private const string RedirectionSectionName = "Redirections";
        private readonly List<Redirection> _redirections;

        public Proxy(IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
            _redirections = config.GetSection(RedirectionSectionName).Get<List<Redirection>>();

            Console.WriteLine($"Found {_redirections.Count} Redirections in config.");
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            if (context.Request.Path == "/")
            {
                await next.Invoke(context);
                return;
            }

            var request = context.Request;
            var redirectedUri = RequestPathMatchesAConfiguredRedirection(request);
            if (string.IsNullOrEmpty(redirectedUri))
            {
                var message = $"No Redirection found for request {request.ToRequstString()}";
                Console.WriteLine(message);
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync(message);
                await context.Response.CompleteAsync();
                return;
            }

            Console.WriteLine($"Forwarding request to {redirectedUri}");

            var forwardedResponse = await ForwardRequest(request, redirectedUri);

            Console.WriteLine(forwardedResponse.LogResponseHeaders());

            await CopyResponseMessageToResponse(forwardedResponse, context.Response);
            
            Console.WriteLine($"Returned forwarded response.");
        }

        private string RequestPathMatchesAConfiguredRedirection(HttpRequest request)
        {
            foreach (var redirection in _redirections)
            {
                if (request.Path.StartsWithSegments(redirection.StartingSegment))
                {
                    return redirection.Uri;
                }
            }
            return string.Empty;
        }

        private async Task<HttpResponseMessage> ForwardRequest(HttpRequest request, string redirectedUri)
        {
            var requestToForward = DuplicateRequest(request, new Uri(redirectedUri));

            var httpClient = _httpClientFactory.CreateClient();
            var forwardedResponse = await httpClient.SendAsync(requestToForward);
            return forwardedResponse;            
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
            
            foreach (var (key, value) in request.Headers)
            {
                if (!requestMessage.Headers.TryAddWithoutValidation(key, value.ToArray()) && requestMessage.Content != null)
                {
                    requestMessage.Content?.Headers.TryAddWithoutValidation(key, value.ToArray());
                }
            }

            requestMessage.Headers.Host = uri.Authority;
            requestMessage.RequestUri = uri;
            requestMessage.Method = new HttpMethod(request.Method);

            return requestMessage;
        }

        private async Task CopyResponseMessageToResponse(HttpResponseMessage forwardedResponse, HttpResponse response)
        {
            response.StatusCode = (int)forwardedResponse.StatusCode;
            foreach (var (key, value) in forwardedResponse.Headers)
            {
                response.Headers.Add(key, new StringValues(value.ToArray()));
            }
            await forwardedResponse.Content.CopyToAsync(response.Body, null, CancellationToken.None);
            await response.CompleteAsync();
        }
    }
}
