using BuildLight.Common.Data;
using BuildLight.Common.Services.TeamCity;
using System;

namespace BuildLight.Common.Services.BuildMonitor
{
    public class BuildStatusEventArgs : EventArgs
    {
        public Project Project { get; set; }
        public Status BuildStatus { get; set; }

        public BuildStatusEventArgs(Project project, Status buildStatus)
        {
            Project = project;
            BuildStatus = buildStatus;
        }
    }
}