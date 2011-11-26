using DeployToAzure.Management;
using DeployToAzure.Tests.TestUtilities;

namespace DeployToAzure.Tests.Management
{
    public class ScriptedAzureManagementLowLevelApiFake : ScriptedTestDouble, IAzureManagementLowLevelApi
    {
        public AzureDeploymentCheckOutcome NextDeploymentCheckOutcome;

        public int CheckStatusCounter;
        public DeploymentSlotUri CheckStatusDeploymentUri;

        public DeploymentSlotUri BeginSuspendDeploymentUri;

        public DeploymentSlotUri BeginDeleteDeploymentUri;

        public DeploymentSlotUri BeginCreateDeploymentUri;

        public IDeploymentConfiguration BeginCreateConfiguration;

        public RequestUri LastCheckRequestStatusRequestUri;
        
        public AzureRequestStatus NextRequestStatus;
        public string NextRequestId;
        
        public AzureDeploymentCheckOutcome CheckDeploymentStatus(DeploymentSlotUri deploymentUri)
        {
            CheckStatusCounter++;
            CheckStatusDeploymentUri = deploymentUri;
            RunScript();
            return NextDeploymentCheckOutcome;
        }

        public RequestUri BeginSuspend(DeploymentSlotUri deploymentUri)
        {
            BeginSuspendDeploymentUri = deploymentUri;
            RunScript();
            return deploymentUri.ToRequestUri(NextRequestId);
        }

        public RequestUri BeginDelete(DeploymentSlotUri deploymentUri)
        {
            BeginDeleteDeploymentUri = deploymentUri;
            RunScript();
            return deploymentUri.ToRequestUri(NextRequestId);
        }

        public RequestUri BeginCreate(DeploymentSlotUri deploymentUri, IDeploymentConfiguration configuration)
        {
            BeginCreateDeploymentUri = deploymentUri;
            BeginCreateConfiguration = configuration;
            RunScript();
            return deploymentUri.ToRequestUri(NextRequestId);
        }

        public AzureRequestStatus CheckRequestStatus(RequestUri requestUri)
        {
            LastCheckRequestStatusRequestUri = requestUri;
            RunScript();
            return NextRequestStatus;
        }
    }
}