using BuildLight.Common.Data;
using BuildLight.Common.Models;
using BuildLight.Common.Services.TeamCity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BuildLight.Common.Services.BuildMonitor
{
    public class BuildMonitorService
    {
        public event EventHandler<ServiceEventArgs> ServiceEvent;
        public event EventHandler<BuildStatusEventArgs> BuildStatusEvent;

        private readonly Settings _settings;
        private readonly CancellationToken _cancellationToken;
        private readonly ITeamCityApiClient _teamCityApiClient;

        private Dictionary<Project, ProjectSettings> _projects;
        private Dictionary<string, Status> _projectStatuses;

        public BuildMonitorService(ITeamCityApiClient teamCityApiClient, Settings settings, CancellationToken cancellationToken)
        {
            _settings = settings;
            _teamCityApiClient = teamCityApiClient;
            _cancellationToken = cancellationToken;

            Task.Run(async () => { await MonitorAsync(); }, _cancellationToken);
        }

        private async Task MonitorAsync()
        {
            var keepRunning = true;
            await RaiseEventAsync(ServiceEvent, new ServiceEventArgs(ServiceEventCode.Starting));
            while (keepRunning)
            {
                if (_cancellationToken.IsCancellationRequested) return;
                try
                {
                    await RaiseEventAsync(ServiceEvent, new ServiceEventArgs(ServiceEventCode.BeginningQuery));

                    if (_projects == null)
                    {
                        _projects = (await _teamCityApiClient.GetProjectsAsync(_cancellationToken))
                                    .Where(p => _settings.Projects.Any(s => string.Equals(s.Name, p.Name, StringComparison.OrdinalIgnoreCase)))
                                    .ToDictionary(p => p, p => _settings.Projects.Single(s => string.Equals(s.Name, p.Name, StringComparison.OrdinalIgnoreCase)));
                        _projectStatuses = _projects.Keys.ToDictionary(o => o.Name, o => Status.Unknown);
                    }

                    await GetCurrentStatusAsync();
                }
                catch (AuthenticationException)
                {
                    keepRunning = false;
                }
                catch (Exception)
                {
                    await RaiseEventAsync(ServiceEvent, new ServiceEventArgs(ServiceEventCode.QueryError));
                }
                if (!keepRunning) continue;
                await RaiseEventAsync(ServiceEvent, new ServiceEventArgs(ServiceEventCode.CompletedQuery));
                await Task.Delay(_settings.PollingTimespan, _cancellationToken);
            }
            await RaiseEventAsync(ServiceEvent, new ServiceEventArgs(ServiceEventCode.Ending));
        }

        private async Task GetCurrentStatusAsync()
        {
            foreach (var p in _projects.Keys)
            {
                var projectsStatus = await _teamCityApiClient.GetCurrentProjectStatusAsync(p, _projects[p].IgnoredBuildConfigs, _cancellationToken);
                if (_projectStatuses[p.Name] != projectsStatus)
                {
                    _projectStatuses[p.Name] = projectsStatus;
                    await RaiseEventAsync(BuildStatusEvent, new BuildStatusEventArgs(p, projectsStatus));
                }
            }
        }

        private async Task RaiseEventAsync<T>(EventHandler<T> handler, T args)
        {
            await Task.Run(() =>
            {
                var handlerCopy = handler;
                handlerCopy?.Invoke(this, args);
            }, _cancellationToken);
        }

    }
}
