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

            OurTrace.TraceInfo("Using parameters:");
            OurTrace.TraceInfo(string.Format("subscriptionId: {0}", subscriptionId));
            OurTrace.TraceInfo(string.Format("serviceName: {0}", serviceName));
            OurTrace.TraceInfo(string.Format("deploymentSlot: {0}", deploymentSlot));
            OurTrace.TraceInfo(string.Format("storageAccountName: {0}", storageAccountName));
            OurTrace.TraceInfo(string.Format("storageAccountKey: {0}", storageAccountKey));
            OurTrace.TraceInfo(string.Format("certFileName: {0}", certFileName));
            OurTrace.TraceInfo(string.Format("certPassword: {0}", certPassword));
            OurTrace.TraceInfo(string.Format("packageFileName: {0}", packageFileName));
            OurTrace.TraceInfo(string.Format("serviceConfigurationPath: {0}", serviceConfigurationPath));
            OurTrace.TraceInfo(string.Format("deploymentLabel: {0}", deploymentLabel));
            OurTrace.TraceInfo(string.Format("deploymentName: {0}", deploymentName));
            OurTrace.TraceInfo(string.Format("roleName: {0}", roleName));
            OurTrace.TraceInfo(string.Format("force: {0}", force));
            OurTrace.TraceInfo(string.Format("maxRetries: {0}", maxRetries));
            OurTrace.TraceInfo(string.Format("retryIntervalInSeconds: {0}", retryIntervalInSeconds));

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
            };
        }
    }
}