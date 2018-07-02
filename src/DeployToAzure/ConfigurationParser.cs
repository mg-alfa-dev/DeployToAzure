using System.IO;
using DeployToAzure.Management;
using DeployToAzure.Utility;

namespace DeployToAzure
{
    public static class ConfigurationParser
    {
        public static DeploymentConfiguration ParseConfiguration(string filename)
        {
            OurTrace.TraceInfo("Using configuration file at: " + filename);
            dynamic arguments = new DynamicXml(File.ReadAllText(filename));

            string subscriptionId = arguments.SubscriptionId.Value;
            string serviceName = arguments.ServiceName.Value;
            string deploymentSlot = arguments.DeploymentSlot.Value;
            string storageAccountName = arguments.StorageAccountName.Value;
            string storageAccountKey = arguments.StorageAccountKey.Value;
            string certFileName = arguments.CertFileName.Value;
            string certPassword = arguments.CertPassword.Value;
            string packageFileName = arguments.PackageFileName.Value;
            string serviceConfigurationPath = arguments.ServiceConfigurationPath.Value;
            string deploymentLabel = arguments.DeploymentLabel.Value;
            string deploymentName = arguments.DeploymentName.Value;
            string roleName = arguments.RoleName.Value;
            string force = arguments.Force.Value ?? "false";
            string maxRetries = arguments.MaxRetries.Value ?? "20";
            string retryIntervalInSeconds = arguments.RetryIntervalInSeconds.Value ?? "15";
            string blobPathToDeploy = arguments.BlobPathToDeploy.Value;
            string changeVMSize = arguments.ChangeVMSize.Value;
            string changeWebRoleVMSize = arguments.ChangeWebRoleVMSize.Value;
            string changeWorkerRoleVMSize = arguments.ChangeWorkerRoleVMSize.Value;

            OurTrace.TraceInfo("Using parameters:");
            OurTrace.TraceInfo($"subscriptionId: {subscriptionId}");
            OurTrace.TraceInfo($"serviceName: {serviceName}");
            OurTrace.TraceInfo($"deploymentSlot: {deploymentSlot}");
            OurTrace.TraceInfo($"storageAccountName: {storageAccountName}");
            OurTrace.TraceInfo($"storageAccountKey: {storageAccountKey}");
            OurTrace.TraceInfo($"certFileName: {certFileName}");
            OurTrace.TraceInfo($"certPassword: {certPassword}");
            OurTrace.TraceInfo($"packageFileName: {packageFileName}");
            OurTrace.TraceInfo($"serviceConfigurationPath: {serviceConfigurationPath}");
            OurTrace.TraceInfo($"deploymentLabel: {deploymentLabel}");
            OurTrace.TraceInfo($"deploymentName: {deploymentName}");
            OurTrace.TraceInfo($"roleName: {roleName}");
            OurTrace.TraceInfo($"force: {force}");
            OurTrace.TraceInfo($"maxRetries: {maxRetries}");
            OurTrace.TraceInfo($"retryIntervalInSeconds: {retryIntervalInSeconds}");
            OurTrace.TraceInfo($"blobPathToDeploy: {blobPathToDeploy}");
            OurTrace.TraceInfo($"changeVMSize: {changeVMSize}");
            OurTrace.TraceInfo($"changeWebRoleVMSize: {changeWebRoleVMSize}");
            OurTrace.TraceInfo($"changeWorkerRoleVMSize: {changeWorkerRoleVMSize}");

            var serviceConfigurationString = File.ReadAllText(serviceConfigurationPath);

            return new DeploymentConfiguration
            {
                DeploymentLabel = deploymentLabel,
                DeploymentName = deploymentName,
                DeploymentSlotUri = new DeploymentSlotUri(subscriptionId, serviceName, deploymentSlot),
                ServiceConfiguration = serviceConfigurationString,
                PackageFileName = packageFileName,
                StorageAccountName = storageAccountName,
                StorageAccountKey = storageAccountKey,
                CertFileName = certFileName,
                CertPassword = certPassword,
                RoleName = roleName,
                Force = bool.Parse(force),
                MaxRetries = int.Parse(maxRetries),
                RetryIntervalInSeconds = int.Parse(retryIntervalInSeconds),
                BlobPathToDeploy = blobPathToDeploy,
                ChangeVMSize = changeVMSize,
                ChangeWebRoleVMSize = changeWebRoleVMSize,
                ChangeWorkerRoleVMSize = changeWorkerRoleVMSize,
            };
        }
    }
}