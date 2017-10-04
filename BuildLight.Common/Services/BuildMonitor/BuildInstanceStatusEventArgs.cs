using BuildLight.Common.Data;
using BuildLight.Common.Services.TeamCity;
using System;

namespace BuildLight.Common.Services.BuildMonitor
{
    public class BuildInstanceStatusEventArgs : EventArgs
    {
        public Project Project { get; set; }
        public BuildInstance BuildInstance { get; set; }
        public Status BuildConfigStatus { get; set; }

        public BuildInstanceStatusEventArgs(Project project, BuildInstance buildInstance, Status buildConfigStatus)
        {
            Project = project;
            BuildInstance = buildInstance;
            BuildConfigStatus = buildConfigStatus;
        }
    }
}