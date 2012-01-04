using System;

namespace DeployToAzure.Management
{
    public interface IAzureDeploymentSlot
    {
        void CreateOrReplaceDeployment(IDeploymentConfiguration configuration);
        void DeleteDeployment();
    }

    public class AzureDeploymentSlot : IAzureDeploymentSlot
    {
        private readonly IAzureManagementApiWithRetries _apiWithRetries;
        private readonly DeploymentSlotUri _deploymentSlotUri;

        public AzureDeploymentSlot(IAzureManagementApiWithRetries azureManagementApi, DeploymentSlotUri deploymentSlotUri)
        {
            _apiWithRetries = azureManagementApi;
            _deploymentSlotUri = deploymentSlotUri;
        }

        public void CreateOrReplaceDeployment(IDeploymentConfiguration configuration)
        {
            DeleteDeployment();
            _apiWithRetries.Create(_deploymentSlotUri, configuration);
            _apiWithRetries.WaitForDeploymentStatus(_deploymentSlotUri, AzureDeploymentCheckOutcome.Running);
        }

        public void DeleteDeployment()
        {
            if (!_apiWithRetries.DoesDeploymentExist(_deploymentSlotUri)) 
                return;

            _apiWithRetries.Suspend(_deploymentSlotUri);
            _apiWithRetries.WaitForDeploymentStatus(_deploymentSlotUri, AzureDeploymentCheckOutcome.Suspended);
            _apiWithRetries.Delete(_deploymentSlotUri);
            _apiWithRetries.WaitForDeploymentStatus(_deploymentSlotUri, AzureDeploymentCheckOutcome.NotFound);
        }

        public void UpgradeDeployment(DeploymentConfiguration configuration)
        {
            _apiWithRetries.Upgrade(_deploymentSlotUri, configuration);
            _apiWithRetries.WaitForDeploymentStatus(_deploymentSlotUri, AzureDeploymentCheckOutcome.Running);
        }
    }
}