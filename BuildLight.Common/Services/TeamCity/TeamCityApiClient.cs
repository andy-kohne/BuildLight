using BuildLight.Common.Data;
using BuildLight.Common.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BuildLight.Common.Services.TeamCity
{
    public interface ITeamCityApiClient
    {
        Task<IEnumerable<Project>> GetProjectsAsync(CancellationToken cancellationToken);
        Task<Status> GetCurrentProjectStatusAsync(Project project, string[] ignoredBuildConfigs, CancellationToken cancellationToken);
    }

    public class TeamCityApiClient : ITeamCityApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _host;

        public TeamCityApiClient(string host, string userName, string password, HttpMessageHandler httpMessageHandler = null)
        {
            _host = host;
            var authenticationHeader = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{userName}:{password}")));

            _httpClient = httpMessageHandler == null ? new HttpClient() : new HttpClient(httpMessageHandler);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Authorization = authenticationHeader;
        }

        private async Task<T> GetAsync<T>(string url, CancellationToken cancellationToken)
        {
            var result = default(T);

            var responseBody = await GetAsync(_httpClient, url, cancellationToken);
            if (!string.IsNullOrWhiteSpace(responseBody))
            {
                result = responseBody.ConvertJsonTo<T>();
            }

            return result;
        }

        private static async Task<string> GetAsync(HttpClient apiClient, string apiUrl, CancellationToken cancellationToken)
        {
            using (var response = apiClient.GetAsync(apiUrl, cancellationToken).Result)
            {
                if (response.StatusCode == HttpStatusCode.Unauthorized) throw new AuthenticationException();

                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
        }

        private string ProjectsUrl => $"{_host}/httpAuth/app/rest/projects";

        public async Task<IEnumerable<Project>> GetProjectsAsync(CancellationToken cancellationToken)
        {
            var projects = new List<Project>();
            var resp = await GetAsync<ProjectsResponse>(ProjectsUrl, cancellationToken);
            foreach (var p in resp.Project)
            {
                projects.Add(await GetAsync<Project>($"{_host}{p.Href}", cancellationToken));
            }
            return projects;
        }

        public async Task<Status> GetCurrentProjectStatusAsync(Project project, string[] ignoredBuildConfigs, CancellationToken cancellationToken)
        {
            var queue =
                (await GetAsync<BuildsResponse>($"{_host}/app/rest/buildQueue?locator=project:{project.Id}", cancellationToken))
                .Build?.Select(
                    q => new BuildTypeStatus { BuildTypeId = q.BuildTypeId, State = q.State.ConvertToBuildState(), Status = q.Status.ConvertToBuildStatus() }) ?? new List<BuildTypeStatus>();

            var builds =
                (await GetAsync<BuildTypes>(
                    $"{_host}/app/rest/buildTypes?locator=affectedProject:(id:{project.Id})&fields=buildType(id,name,builds($locator(running:any,canceled:false,count:3),build(number,status,statusText,state)))", cancellationToken))
                .BuildType?
                .SelectMany(bt => bt.Builds.Build.Select(bb => new BuildTypeStatus
                {
                    BuildTypeId = bt.Id,
                    Status = bb.Status.ConvertToBuildStatus(),
                    State = bb.State.ConvertToBuildState(),
                    Number = bb.Number
                }))
                 ?? new List<BuildTypeStatus>();

            var statuses = queue.Concat(builds)
                                .Where(o => project.BuildTypes.BuildType
                                                   .Where(bt => !ignoredBuildConfigs.Contains(bt.Name))
                                                   .Any(bt => string.Equals(bt.Id, o.BuildTypeId, StringComparison.OrdinalIgnoreCase)))
                                .GroupBy(o => o.BuildTypeId)
                                .Select(o => o.OrderByDescending(b => b.State == BuildState.Running)
                                              .ThenByDescending(b => b.State == BuildState.Queued)
                                              .ThenByDescending(b => b.Number)
                                              .FirstOrDefault(b => b.State != BuildState.Queued)
                                        )
                                .ToArray();

            var queued = statuses.Any(o => o.State == BuildState.Queued);
            var running = statuses.Any(o => o.State == BuildState.Running);
            var success = statuses.Where(o => o.State == BuildState.Finished).All(o => o.Status == BuildStatus.Success);

            return running
                ? Status.Running
                : queued
                    ? Status.Queued
                    : success
                        ? Status.Success
                        : Status.Failure;
        }

        private class BuildTypeStatus
        {
            public string BuildTypeId { get; set; }
            public string Number { get; set; }
            public BuildStatus Status { get; set; }
            public BuildState State { get; set; }
        }
    }
}
