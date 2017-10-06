using BuildLight.Common.Models;
using BuildLight.Common.Services.BuildMonitor;
using BuildLight.Common.Services.TeamCity;
using Moq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BuildLight.Common.Data;
using Xunit;

namespace BuildLight.Tests
{
    public class BuildMonitorServiceTests
    {
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Mock<ITeamCityApiClient> _mockTeamCityApiClient;
        private readonly BuildMonitorService _buildMonitorService;

        public BuildMonitorServiceTests()
        {
            var settings = new Settings
            {
                Projects = new[] { new ProjectSettings { Name = "Really Cool Project", IgnoredBuildConfigs = new[] { "unnecessary build config" } } },
                PollingSeconds = 1
            };

            _cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(2));
            _mockTeamCityApiClient = new Mock<ITeamCityApiClient>(MockBehavior.Strict);
            _buildMonitorService = new BuildMonitorService(_mockTeamCityApiClient.Object, settings);
        }

        [Fact]
        public async Task Test_ContinuesOnError()
        {
            var count = 0;

            _mockTeamCityApiClient
                .Setup(o => o.GetProjectsAsync(It.IsAny<CancellationToken>()))
                .Callback(() =>
                {
                    count++;
                    if (count >= 3)
                        _cancellationTokenSource.CancelAfter(500);
                })
                .Throws(new HttpRequestException());

            var events = new List<ServiceEventArgs>();
            _buildMonitorService.ServiceEvent += (sender, args) => events.Add(args);

            await _buildMonitorService.MonitorAsync(_cancellationTokenSource.Token);

            _mockTeamCityApiClient.VerifyAll();
            Assert.Equal(3, count);
            Assert.True(events.Count > 5);
            Assert.Contains(events, args => args.EventCode == ServiceEventCode.Starting);
            Assert.Contains(events, args => args.EventCode == ServiceEventCode.BeginningQuery);
            Assert.Contains(events, args => args.EventCode == ServiceEventCode.QueryError);
            Assert.Contains(events, args => args.EventCode == ServiceEventCode.CompletedQuery);
            Assert.Contains(events, args => args.EventCode == ServiceEventCode.Ending);
        }


        [Fact]
        public async Task Test_AuthenticationErrorAtStartup()
        {
            _mockTeamCityApiClient.Setup(o => o.GetProjectsAsync(It.IsAny<CancellationToken>()))
                .Throws(new AuthenticationException());

            var events = new List<ServiceEventArgs>();
            _buildMonitorService.ServiceEvent += (sender, args) => events.Add(args);

            await _buildMonitorService.MonitorAsync(_cancellationTokenSource.Token);

            _mockTeamCityApiClient.VerifyAll();
            Assert.Equal(4, events.Count);
            Assert.Contains(events, args => args.EventCode == ServiceEventCode.Starting);
            Assert.Contains(events, args => args.EventCode == ServiceEventCode.BeginningQuery);
            Assert.Contains(events, args => args.EventCode == ServiceEventCode.AuthenticationError);
            Assert.Contains(events, args => args.EventCode == ServiceEventCode.Ending);
        }
        [Fact]
        public async Task Test_GetsProjectAndBuildData_FiresBuildStatus()
        {
            _mockTeamCityApiClient
                .Setup(o => o.GetProjectsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Project> { new Project { Name = "Really Cool Project" }, new Project { Name = "Not So Cool Project" } });

            _mockTeamCityApiClient
                .Setup(o => o.GetCurrentProjectStatusAsync(It.Is<Project>(p => p.Name == "Really Cool Project"), It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Status.Success);

            var serviceEvents = new List<ServiceEventArgs>();
            var buildEvents = new List<BuildStatusEventArgs>();

            _buildMonitorService.ServiceEvent += (sender, args) => serviceEvents.Add(args);
            _buildMonitorService.BuildStatusEvent += (sender, args) =>
            {
                buildEvents.Add(args);
                _cancellationTokenSource.CancelAfter(500);
            };

            await _buildMonitorService.MonitorAsync(_cancellationTokenSource.Token);

            _mockTeamCityApiClient.VerifyAll();
            Assert.Contains(serviceEvents, args => args.EventCode == ServiceEventCode.Starting);
            Assert.Contains(serviceEvents, args => args.EventCode == ServiceEventCode.BeginningQuery);
            Assert.Contains(serviceEvents, args => args.EventCode == ServiceEventCode.CompletedQuery);
            Assert.Contains(serviceEvents, args => args.EventCode == ServiceEventCode.Ending);
            Assert.DoesNotContain(serviceEvents, args => args.EventCode == ServiceEventCode.AuthenticationError);
            Assert.DoesNotContain(serviceEvents, args => args.EventCode == ServiceEventCode.QueryError);

            Assert.Contains(buildEvents, args => args.Project.Name == "Really Cool Project" && args.BuildStatus == Status.Success);
        }

        [Fact]
        public async Task Test_FiresBuildStatusEvents()
        {
            var serviceEvents = new List<ServiceEventArgs>();
            var buildEvents = new List<BuildStatusEventArgs>();

            _buildMonitorService.ServiceEvent += (sender, args) => serviceEvents.Add(args);
            _buildMonitorService.BuildStatusEvent += (sender, args) => buildEvents.Add(args);

            _mockTeamCityApiClient
                .Setup(o => o.GetProjectsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Project> { new Project { Name = "Really Cool Project" }, new Project { Name = "Not So Cool Project" } });

            _mockTeamCityApiClient
                .Setup(o => o.GetCurrentProjectStatusAsync(It.Is<Project>(p => p.Name == "Really Cool Project"),
                    It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(buildEvents.Count == 0
                    ? Status.Success
                    : buildEvents.Count == 1
                        ? Status.Queued
                        : buildEvents.Count == 2
                            ? Status.Running
                            : Status.Success))
                .Callback(() =>
                {
                    if (buildEvents.Count >= 4)
                        _cancellationTokenSource.CancelAfter(500);
                });


            await _buildMonitorService.MonitorAsync(_cancellationTokenSource.Token);

            _mockTeamCityApiClient.VerifyAll();
            Assert.Contains(serviceEvents, args => args.EventCode == ServiceEventCode.Starting);
            Assert.Contains(serviceEvents, args => args.EventCode == ServiceEventCode.BeginningQuery);
            Assert.Contains(serviceEvents, args => args.EventCode == ServiceEventCode.CompletedQuery);
            Assert.Contains(serviceEvents, args => args.EventCode == ServiceEventCode.Ending);
            Assert.DoesNotContain(serviceEvents, args => args.EventCode == ServiceEventCode.AuthenticationError);
            Assert.DoesNotContain(serviceEvents, args => args.EventCode == ServiceEventCode.QueryError);

            Assert.Equal(4, buildEvents.Count);
            Assert.Equal(Status.Success, buildEvents[0].BuildStatus);
            Assert.Equal(Status.Queued, buildEvents[1].BuildStatus);
            Assert.Equal(Status.Running, buildEvents[2].BuildStatus);
            Assert.Equal(Status.Success, buildEvents[3].BuildStatus);
        }

    }
}
