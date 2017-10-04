namespace BuildLight.Common.Services
{
    public enum VisualizationStates
    {
        None,
        Running,
        Queued,
        Failed,
        Succeeded
    }

    public enum AnimatedVisualizationStates
    {
        None,
        Failed,
        Succeeded,
        Building,
        FailedBuilding,
        SuccessBuilding,
        //FailedBuildingFailed,
        //FailedBuildingSuccess,
        //SuccessBuildingFailed,
        //SuccessBuildingSuccess,
        FailedAlert,
        StartingUp,
    }
}
