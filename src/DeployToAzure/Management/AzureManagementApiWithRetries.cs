using System;
using DeployToAzure.Utility;

namespace DeployToAzure.Management
{
    public interface IAzureManagementApiWithRetries
    {
        bool DoesDeploymentExist(DeploymentSlotUri deploymentUri);
        void WaitForDeploymentStatus(DeploymentSlotUri deploymentUri, AzureDeploymentCheckOutcome status);

        void Create(DeploymentSlotUri deploymentUri, IDeploymentConfiguration configuration);
        void Suspend(DeploymentSlotUri deploymentUri);
        void Delete(DeploymentSlotUri deploymentUri);
        void Upgrade(DeploymentSlotUri deploymentSlotUri, DeploymentConfiguration configuration);
    }

    public class AzureManagementApiWithRetries: IAzureManagementApiWithRetries
    {
        private readonly IAzureManagementLowLevelApi _managementLowLevelApi;
        private readonly int _maxRetries;
        private readonly TimeSpan _sleepIntervalMilliseconds;

        public AzureManagementApiWithRetries(IAzureManagementLowLevelApi managementLowLevelApi, int maxRetries, TimeSpan sleepIntervalMilliseconds)
        {
            _managementLowLevelApi = managementLowLevelApi;
            _maxRetries = maxRetries;
            _sleepIntervalMilliseconds = sleepIntervalMilliseconds;
        }

        public bool DoesDeploymentExist(DeploymentSlotUri deploymentUri)
        {
            var outcome = AzureDeploymentCheckOutcome.None;
            
            var retry = new RetryLogic(_maxRetries, _sleepIntervalMilliseconds);
            retry.Execute(
                () => outcome = _managementLowLevelApi.CheckDeploymentStatus(deploymentUri),
                ex => RetryOrRethrow.Rethrow,
                () => outcome != AzureDeploymentCheckOutcome.Failed);

            switch (outcome)
            {
                case AzureDeploymentCheckOutcome.Running:
                case AzureDeploymentCheckOutcome.Suspended:
                    return true;
                case AzureDeploymentCheckOutcome.NotFound:
                    return false;
                default:
                    FailFast.WithMessage("Unexpected AzureDeploymentCheckOutcome: " + outcome);
                    // ReSharper disable HeuristicUnreachableCode
                    return false;
                    // ReSharper restore HeuristicUnreachableCode
            }
        }

        public void WaitForDeploymentStatus(DeploymentSlotUri deploymentUri, AzureDeploymentCheckOutcome status)
        {
            FailFast.Unless(
                status != AzureDeploymentCheckOutcome.Failed,
                "can't wait on Failed!");

            var outcome = AzureDeploymentCheckOutcome.None;

            var retry = new RetryLogic(_maxRetries, _sleepIntervalMilliseconds);
            retry.Execute(
                () => outcome = _managementLowLevelApi.CheckDeploymentStatus(deploymentUri),
                ex => RetryOrRethrow.Rethrow,
                () => outcome == status);
        }

        public void Create(DeploymentSlotUri deploymentUri, IDeploymentConfiguration configuration)
        {
            ExecuteAsynchronousAction(() => _managementLowLevelApi.BeginCreate(deploymentUri, configuration));
        }

        public void Suspend(DeploymentSlotUri deploymentUri)
        {
            ExecuteAsynchronousAction(() => _managementLowLevelApi.BeginSuspend(deploymentUri));
        }

        public void Delete(DeploymentSlotUri deploymentUri)
        {
            ExecuteAsynchronousAction(() => _managementLowLevelApi.BeginDelete(deploymentUri));
        }

        public void Upgrade(DeploymentSlotUri deploymentSlotUri, DeploymentConfiguration configuration)
        {
            ExecuteAsynchronousAction(() => _managementLowLevelApi.BeginUpgrade(deploymentSlotUri, configuration));
        }

        private void ExecuteAsynchronousAction(Func<RequestUri> action)
        {
            var status = AzureRequestStatus.InProgress;
            var retry = new RetryLogic(_maxRetries, _sleepIntervalMilliseconds);
            retry.Execute(
                () =>
                {
                    RequestUri requestUri = null;
                    retry.Execute(
                        () => requestUri = action(),
                        ex => ex is UnhandledHttpException ? RetryOrRethrow.Retry : RetryOrRethrow.Rethrow);
                    if (requestUri != null)
                        retry.Execute(
                            () => status = _managementLowLevelApi.CheckRequestStatus(requestUri),
                            ex => ex is UnhandledHttpException ? RetryOrRethrow.Retry : RetryOrRethrow.Rethrow,
                            () => status != AzureRequestStatus.InProgress);
                },
                ex => RetryOrRethrow.Rethrow,
                () => status == AzureRequestStatus.Succeeded);
        }
    }
}