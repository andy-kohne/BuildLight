using BuildLight.Common.Data;
using BuildLight.Common.Models;
using BuildLight.Common.Services;
using BuildLight.Common.Services.BuildMonitor;
using BuildLight.Common.Services.TeamCity;
using Moq;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace BuildLight.Tests
{
    public class VisualizationServiceTests
    {
        private readonly Mock<IPwmController> _mockPwmController;
        private readonly Dictionary<int, Mock<IPwmPin>> _pinMocks;

        public VisualizationServiceTests()
        {
            _mockPwmController = new Mock<IPwmController>(MockBehavior.Strict);

            _pinMocks = new Dictionary<int, Mock<IPwmPin>>
            {
                {1, new Mock<IPwmPin>(MockBehavior.Strict)},
                {2, new Mock<IPwmPin>(MockBehavior.Strict)},
                {3, new Mock<IPwmPin>(MockBehavior.Strict)}
            };
        }

        public VisualizationConfig[] Config =>
            new[]
            {
                new VisualizationConfig
                {
                    AssociatedProjects = new[] {"Really Cool Project"},
                    HardwareOutput = new RgbOutputPinSet
                    {
                        RedPin = 1,
                        BluePin = 2,
                        GreenPin = 3
                    }
                }
            };

        [Fact]
        public void Test_Service()
        {
            _mockPwmController
                .Setup(o => o.OpenPin(It.Is<int>(i => i >= 1 && i <= 3)))
                .Returns<int>(p => _pinMocks[p].Object);

            foreach (var p in _pinMocks.Values)
            {
                p.Setup(o => o.IsStarted).Returns(false).Verifiable();
                p.Setup(o => o.Start()).Verifiable();
            }

            var svc = new VisualizationService(Config, _mockPwmController.Object);

            Assert.NotNull(svc);

            _mockPwmController.VerifyAll();
            foreach (var p in _pinMocks.Values)
            {
                p.VerifyAll();
            }
        }

        [Fact]
        public void Test_BuildEventHanlder()
        {
            var svc = new VisualizationService(Config, null);
            Assert.NotNull(svc);
            Assert.Equal(VisualizationStates.None, svc._visualizations.Single().OverallState);

            svc.HandleBuildEvent(this, new BuildStatusEventArgs(new Project { Name = "Really Cool Project" }, Status.Success));
            Assert.Equal(VisualizationStates.Succeeded, svc._visualizations.Single().OverallState);

            svc.HandleBuildEvent(this, new BuildStatusEventArgs(new Project { Name = "Really Cool Project" }, Status.Running));
            Assert.Equal(VisualizationStates.Running, svc._visualizations.Single().OverallState);
            Assert.Equal(AnimatedVisualizationStates.SuccessBuilding, svc._visualizations.Single().AnimatedVisualizationState);

            svc.HandleBuildEvent(this, new BuildStatusEventArgs(new Project { Name = "Really Cool Project" }, Status.Failure));
            Assert.Equal(VisualizationStates.Failed, svc._visualizations.Single().OverallState);
            Assert.Equal(AnimatedVisualizationStates.Failed, svc._visualizations.Single().AnimatedVisualizationState);

            svc.HandleBuildEvent(this, new BuildStatusEventArgs(new Project { Name = "Really Cool Project" }, Status.Running));
            Assert.Equal(VisualizationStates.Running, svc._visualizations.Single().OverallState);
            Assert.Equal(AnimatedVisualizationStates.FailedBuilding, svc._visualizations.Single().AnimatedVisualizationState);
        }
    }
}
