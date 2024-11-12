using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace frontend.Services
{
    public class AuthorizationMessageHandler : DelegatingHandler
    {
        private readonly LocalStorageService _localStorage;

        public AuthorizationMessageHandler(LocalStorageService localStorage)
        {
            _localStorage = localStorage;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = await _localStorage.GetItem("token");
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            return await base.SendAsync(request, cancellationToken);
        }
    }
}