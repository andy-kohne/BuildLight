namespace BuildLight.Common.Data
{
    public enum BuildStatus
    {
        Unknown,
        Success,
        Failure
    }

    public enum BuildState
    {
        Unknown,
        Finished,
        Running,
        Queued
    }

    public class BuildInstance
    {
        public int? Id { get; set; }
        public string BuildTypeId { get; set; }
        public string Number { get; set; }
        public string Status { get; set; }
        public string State { get; set; }
        public string Href { get; set; }
        public string WebUrl { get; set; }
    }

    public class BuildType
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string ProjectName { get; set; }
        public string ProjectId { get; set; }
        public string Href { get; set; }
        public string WebUrl { get; set; }
        public bool? Paused { get; set; }
        public BuildInstances Builds { get; set; }
    }

    public class BuildTypes
    {
        public int Count { get; set; }
        public BuildType[] BuildType { get; set; }    
    }

    public class BuildInstances
    {
        public BuildInstance[] Build { get; set; }
    }
    public class Project
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string ParentProjectId { get; set; }
        public string Href { get; set; }
        public string WebUrl { get; set; }
        public bool? Archived { get; set; }

        public ParentProject ParentProject { get; set; }
        public BuildTypes BuildTypes { get; set; }
        public VcsRoot[] VcsRoot { get; set; }
    }

    public class VcsRoot
    {
        public string HRef { get; set; }
    }

    public class ParentProject
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Href { get; set; }
        public string WebUrl { get; set; }
    }

    public class ProjectsResponse
    {
        public int Count { get; set; }
        public string Href { get; set; }
        public Project[] Project { get; set; }
    }

    public class BuildsResponse
    {
        public BuildInstance[] Build { get; set; }
        public int Count { get; set; }
        public string Href { get; set; }
        public string NextHref { get; set; }

    }
}
