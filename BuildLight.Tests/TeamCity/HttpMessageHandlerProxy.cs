using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace BuildLight.Tests.TeamCity
{
    public interface IFakeMessageHandler
    {
        Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken);
    }

    public class HttpMessageHandlerProxy : HttpMessageHandler
    {
        private readonly IFakeMessageHandler _fakeHandler;

        public HttpMessageHandlerProxy(IFakeMessageHandler fakeHandler)
        {
            _fakeHandler = fakeHandler;
        }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _fakeHandler.SendAsync(request, cancellationToken);
        }
    }
}