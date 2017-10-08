using BuildLight.Common.Data;
using BuildLight.Common.Services.TeamCity;
using Moq;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace BuildLight.Tests.TeamCity
{
    public class TeamCityApiClientTests
    {
        private readonly Mock<IFakeMessageHandler> _mockHandler;
        private readonly ITeamCityApiClient _client;

        public TeamCityApiClientTests()
        {
            _mockHandler = new Mock<IFakeMessageHandler>(MockBehavior.Strict);
            _client = new TeamCityApiClient("http://host.com", "user", "pass", new HttpMessageHandlerProxy(_mockHandler.Object));
        }

        private string GetEmbeddedText(string name)
        {
            var assembly = GetType().GetTypeInfo().Assembly;

            using (var resource = assembly.GetManifestResourceStream(name))
            using (var sr = new StreamReader(resource))
            {
                return sr.ReadToEnd();
            }
        }

        [Fact]
        public async Task Test_ThrowsAuthenticationException()
        {
            _mockHandler
                .Setup(o => o.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.Unauthorized }).Verifiable();

            await Assert.ThrowsAsync<AuthenticationException>(async () => await _client.GetProjectsAsync(CancellationToken.None));
        }

        [Fact]
        public async Task Test_ThrowsOnBadResponses()
        {
            _mockHandler
                .Setup(o => o.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.NotFound, Content = new StringContent("") }).Verifiable();

            await Assert.ThrowsAsync<HttpRequestException>(async () => await _client.GetProjectsAsync(CancellationToken.None));
        }

        [Fact]
        public async Task Test_GetProjectsAsync()
        {
            _mockHandler
                .Setup(o => o.SendAsync(It.Is<HttpRequestMessage>(r => r.RequestUri.ToString().Equals("http://host.com/httpAuth/app/rest/projects")), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HttpResponseMessage {Content = new StringContent(GetEmbeddedText("BuildLight.Tests.TeamCity.HttpResponse.ProjectList.json"))}).Verifiable();

            _mockHandler
                .Setup(o => o.SendAsync(It.Is<HttpRequestMessage>(r => r.RequestUri.ToString().Equals("http://host.com/app/rest/projects/id:_Root")), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HttpResponseMessage { Content = new StringContent(GetEmbeddedText("BuildLight.Tests.TeamCity.HttpResponse.Project_root.json")) }).Verifiable();

            _mockHandler
                .Setup(o => o.SendAsync(It.Is<HttpRequestMessage>(r => r.RequestUri.ToString().Equals("http://host.com/app/rest/projects/id:reallycoolproject")), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HttpResponseMessage { Content = new StringContent(GetEmbeddedText("BuildLight.Tests.TeamCity.HttpResponse.Project_reallycoolproject.json")) }).Verifiable();

            var projects = (await _client.GetProjectsAsync(CancellationToken.None)).ToList();

            Assert.Equal(2, projects.Count);
            Assert.Collection(projects, Assert.NotNull, Assert.NotNull);

            _mockHandler.VerifyAll();

            var root = projects[0];
            var project = projects[1];

            Assert.Equal(project.ParentProjectId, root.Id);
            Assert.Equal("Really Cool Project", project.Name);
        }

        [Fact]
        public async Task Test_GetCurrentProjectStatusAsync()
        {
            var project = new Project
            {
                Id = "reallycoolproject",
               BuildTypes = new BuildTypes
               {
                   BuildType = new []
                   {
                       new BuildType
                       {
                           Id = "buildwebsite",
                           Name = "Build Website"
                       },
                       new BuildType
                       {
                           Id = "buildapi",
                           Name = "Build Api"
                       },
                       new BuildType
                       {
                           Id = "buildservice",
                           Name = "Build Service"
                       },
                   }
               }
            };

            _mockHandler
                .Setup(o => o.SendAsync(It.Is<HttpRequestMessage>(r => r.RequestUri.ToString().Equals("http://host.com/app/rest/buildQueue?locator=project:reallycoolproject")), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HttpResponseMessage { Content = new StringContent(GetEmbeddedText("BuildLight.Tests.TeamCity.HttpResponse.BuildQueue.json")) }).Verifiable();

            _mockHandler
                .Setup(o => o.SendAsync(It.Is<HttpRequestMessage>(r => r.RequestUri.ToString().Equals("http://host.com/app/rest/buildTypes?locator=affectedProject:(id:reallycoolproject)&fields=buildType(id,name,builds($locator(running:any,canceled:false,count:3),build(number,status,statusText,state)))")), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HttpResponseMessage { Content = new StringContent(GetEmbeddedText("BuildLight.Tests.TeamCity.HttpResponse.Builds.json")) }).Verifiable();


            var result = await _client.GetCurrentProjectStatusAsync(project, new string[] { }, CancellationToken.None);


            Assert.Equal(Status.Running, result);
            _mockHandler.VerifyAll();
        }
    }
}
