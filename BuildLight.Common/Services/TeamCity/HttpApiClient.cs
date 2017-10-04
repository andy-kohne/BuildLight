//using System.Net.Http;
//using System.Net.Http.Headers;
//using System.Threading;
//using System.Threading.Tasks;

//namespace BuildLight.UWP.Services.TeamCity
//{
//    public interface IHttpApiClient
//    {
//        Task<HttpResponseMessage> GetAsync(string requestUri, CancellationToken cancellationToken);
//        HttpRequestHeaders DefaultRequestHeaders { get; }
//    }

//    public class HttpApiClient : IHttpApiClient
//    {
//        private readonly HttpClient _httpClient;

//        public HttpApiClient(HttpMessageHandler handler = null) 
//        {
//            _httpClient = new HttpClient(handler);
//        }

//        public async Task<HttpResponseMessage> GetAsync(string requestUri, CancellationToken cancellationToken)
//        {
//            return await _httpClient.GetAsync(requestUri, cancellationToken);
//        }

//        public HttpRequestHeaders DefaultRequestHeaders => _httpClient.DefaultRequestHeaders;
//    }
//}