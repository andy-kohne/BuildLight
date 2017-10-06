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
    public interface IBuildMonitorService
    {
        event EventHandler<ServiceEventArgs> ServiceEvent;
        event EventHandler<BuildStatusEventArgs> BuildStatusEvent;

        Task MonitorAsync(CancellationToken cancellationToken);
    }

    public class BuildMonitorService : IBuildMonitorService
    {
        public event EventHandler<ServiceEventArgs> ServiceEvent;
        public event EventHandler<BuildStatusEventArgs> BuildStatusEvent;

        private readonly Settings _settings;
        private readonly ITeamCityApiClient _teamCityApiClient;

        private Dictionary<Project, ProjectSettings> _projects;
        private Dictionary<string, Status> _projectStatuses;

        public BuildMonitorService(ITeamCityApiClient teamCityApiClient, Settings settings)
        {
            _settings = settings;
            _teamCityApiClient = teamCityApiClient;
        }

        public async Task MonitorAsync(CancellationToken cancellationToken)
        {
            try
            {
                var keepRunning = true;
                await RaiseEventAsync(ServiceEvent, new ServiceEventArgs(ServiceEventCode.Starting), cancellationToken);
                while (keepRunning)
                {
                    if (cancellationToken.IsCancellationRequested) return;
                    try
                    {
                        await RaiseEventAsync(ServiceEvent, new ServiceEventArgs(ServiceEventCode.BeginningQuery),
                            cancellationToken);

                        if (_projects == null)
                        {
                            _projects = (await _teamCityApiClient.GetProjectsAsync(cancellationToken))
                                .Where(p => _settings.Projects.Any(s =>
                                    string.Equals(s.Name, p.Name, StringComparison.OrdinalIgnoreCase)))
                                .ToDictionary(p => p,
                                    p => _settings.Projects.Single(s =>
                                        string.Equals(s.Name, p.Name, StringComparison.OrdinalIgnoreCase)));
                            _projectStatuses = _projects.Keys.ToDictionary(o => o.Name, o => Status.Unknown);
                        }

                        await GetCurrentStatusAsync(cancellationToken);
                    }
                    catch (AuthenticationException)
                    {
                        await RaiseEventAsync(ServiceEvent, new ServiceEventArgs(ServiceEventCode.AuthenticationError),
                            cancellationToken);
                        keepRunning = false;
                    }
                    catch (Exception)
                    {
                        await RaiseEventAsync(ServiceEvent, new ServiceEventArgs(ServiceEventCode.QueryError),
                            cancellationToken);
                    }
                    if (!keepRunning) continue;
                    await RaiseEventAsync(ServiceEvent, new ServiceEventArgs(ServiceEventCode.CompletedQuery),
                        cancellationToken);
                    await Task.Delay(_settings.PollingTimespan, cancellationToken);
                }
                await RaiseEventAsync(ServiceEvent, new ServiceEventArgs(ServiceEventCode.Ending), cancellationToken);
            }
            catch (TaskCanceledException)
            {
                await RaiseEventAsync(ServiceEvent, new ServiceEventArgs(ServiceEventCode.Ending), CancellationToken.None);
            }
        }

        private async Task GetCurrentStatusAsync(CancellationToken cancellationToken)
        {
            foreach (var p in _projects.Keys)
            {
                var projectsStatus = await _teamCityApiClient.GetCurrentProjectStatusAsync(p, _projects[p].IgnoredBuildConfigs, cancellationToken);
                if (_projectStatuses[p.Name] != projectsStatus)
                {
                    _projectStatuses[p.Name] = projectsStatus;
                    await RaiseEventAsync(BuildStatusEvent, new BuildStatusEventArgs(p, projectsStatus), cancellationToken);
                }
            }
        }

        private async Task RaiseEventAsync<T>(EventHandler<T> handler, T args, CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                var handlerCopy = handler;
                handlerCopy?.Invoke(this, args);
            }, cancellationToken);
        }
    }
}
