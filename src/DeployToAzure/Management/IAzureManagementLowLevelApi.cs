namespace DeployToAzure.Management
{
    public interface IAzureManagementLowLevelApi
    {
        AzureDeploymentCheckOutcome CheckDeploymentStatus(DeploymentSlotUri deploymentUri);
        RequestUri BeginSuspend(DeploymentSlotUri deploymentUri);
        RequestUri BeginDelete(DeploymentSlotUri deploymentUri);
        RequestUri BeginCreate(DeploymentSlotUri deploymentUri, IDeploymentConfiguration configuration);
        AzureRequestStatus CheckRequestStatus(RequestUri requestUri);
    }
}