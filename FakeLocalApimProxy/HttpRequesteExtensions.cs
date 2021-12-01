using System.Text;

namespace FakeLocalApimProxy
{
    public static class HttpRequesteExtensions 
    {
        public static string ToRequstString(this HttpRequest request)
        {
            return $"{request.Scheme}://{request.Host}{request.Path}{request.QueryString.Value}";
        }        
    }
}
