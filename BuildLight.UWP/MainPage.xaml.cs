using BuildLight.Common.Extensions;
using BuildLight.Common.Models;
using BuildLight.Common.Services;
using BuildLight.Common.Services.BuildMonitor;
using BuildLight.Common.Services.TeamCity;
using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml.Controls;

namespace BuildLight.UWP
{
    public sealed partial class MainPage : Page
    {
        readonly BuildMonitorService _buildMonitorService;
        readonly VisualizationService _visualizationService;
        readonly CancellationToken _cancellationToken;

        public MainPage()
        {
            InitializeComponent();
            _cancellationToken = new CancellationToken();

            var settings = GetSettingsAsync().Result;
            var tcApiClient = new TeamCityApiClient(settings.Host, settings.UserName, settings.Password);

            _visualizationService = new VisualizationService(settings.Visualizations, _cancellationToken);

            _buildMonitorService = new BuildMonitorService(tcApiClient, settings, _cancellationToken);
            _buildMonitorService.BuildStatusEvent += _visualizationService.HandleBuildEvent;
        }

        private async Task<Settings> GetSettingsAsync()
        {
            var file = await ApplicationData.Current.LocalFolder.GetFileAsync("settings.json");
            var text = await FileIO.ReadTextAsync(file);
            return text.ConvertJsonTo<Settings>();
        }
    }
}
