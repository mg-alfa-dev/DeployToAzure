using System;
using System.Diagnostics;
using System.IO;
using DeployToAzure.Management;
using DeployToAzure.Utility;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using System.Security.Cryptography.X509Certificates;

namespace DeployToAzure
{
    static class Program
    {
        static int Main(string[] args)
        {
            var consoleTraceListener = new OurConsoleTraceListener();
            OurTrace.AddListener(consoleTraceListener);
            OurTrace.Source.Switch.Level = SourceLevels.All;

            if(args.Length != 1)
            {
                Usage();
                return 0;
            }

            OurTrace.TraceInfo("Using configuration file at: " + args[0]);
            dynamic arguments = new DynamicXml(File.ReadAllText(args[0]));

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

            var packageUrl = string.Format("https://{0}.blob.core.windows.net/deployment-package/{1}.cspkg", storageAccountName, Guid.NewGuid());
            var certificate = new X509Certificate2(certFileName, certPassword);

            UploadBlob(packageFileName, packageUrl, storageAccountName, storageAccountKey);

            var http = new Http(certificate);
            var azureDeploymentDeploymentLowLevelApi = new AzureManagementLowLevelApi(http);
            var managementApiWithRetries = new AzureManagementApiWithRetries(azureDeploymentDeploymentLowLevelApi, 20, TimeSpan.FromSeconds(15));

            var deploymentSlotUri = new DeploymentSlotUri(subscriptionId, serviceName, deploymentSlot);

            var serviceConfigurationString = File.ReadAllText(serviceConfigurationPath);

            var configuration = new DeploymentConfiguration
            {
                DeploymentLabel = deploymentLabel,
                DeploymentName = deploymentName,
                PackageUrl = packageUrl,
                ServiceConfiguration = serviceConfigurationString,
            };

            var deploymentSlotManager = new AzureDeploymentSlot(managementApiWithRetries, deploymentSlotUri);
            try
            {
                deploymentSlotManager.CreateOrReplaceDeployment(configuration);
                DeleteBlob(packageUrl, storageAccountName, storageAccountKey);
            }
            catch(Exception ex)
            {
                OurTrace.TraceError(string.Format("exception!\n{0}", ex));
                return -1;
            }
            return 0;
        }

        private static void Usage()
        {
            Console.WriteLine("Usage: DeployToAzure <parameters file name>");
            Console.WriteLine("  Parameters file is a XML file with the following contents:");
            Console.WriteLine("  <Params>");
            Console.WriteLine("    <SubscriptionId>(your azure subscription id)</SubscriptionId>");
            Console.WriteLine("    <ServiceName>(your azure hosted service name)</ServiceName>");
            Console.WriteLine("    <DeploymentSlot>(production | staging)</DeploymentSlot>");
            Console.WriteLine("    <StorageAccountName>(your storage account name to upload the package to)</StorageAccountName>");
            Console.WriteLine("    <StorageAccountKey>(your storage account key)</StorageAccountKey>");
            Console.WriteLine("    <CertFileName>(your admin certificate pfx file with path)</CertFileName>");
            Console.WriteLine("    <CertPassword>(the password for your admin cert)</CertPassword>");
            Console.WriteLine("    <PackageFileName>(path to your cspkg file)</PackageFileName>");
            Console.WriteLine("    <ServiceConfigurationPath>(path to your cspkg file)</ServiceConfigurationPath>");
            Console.WriteLine("    <DeploymentLabel>(deployment label)</DeploymentLabel>");
            Console.WriteLine("    <DeploymentName>(deployment name)</DeploymentName>");
            Console.WriteLine("  </Params>");
        }

        private static void DeleteBlob(string packageUrl, string storageAccountName, string storageAccountKey)
        {
            OurTrace.TraceInfo(string.Format("Deleting blob {0}", packageUrl));

            var packageUri = new Uri(packageUrl);
            var baseAddress = packageUri.GetComponents(UriComponents.SchemeAndServer, UriFormat.UriEscaped);
            var credentials = new StorageCredentialsAccountAndKey(storageAccountName, storageAccountKey);

            var cloudBlobClient = new CloudBlobClient(baseAddress, credentials);
            var blobRef = cloudBlobClient.GetBlockBlobReference(packageUrl);
            blobRef.DeleteIfExists();
        }

        private static void UploadBlob(string packageFileName, string packageUrl, string storageAccountName, string storageAccountKey)
        {
            OurTrace.TraceInfo(string.Format("Uploading blob from {0} to {1}", packageFileName, packageUrl));

            var packageUri = new Uri(packageUrl);
            var baseAddress = packageUri.GetComponents(UriComponents.SchemeAndServer, UriFormat.UriEscaped);
            var credentials = new StorageCredentialsAccountAndKey(storageAccountName, storageAccountKey);

            RetryPolicy myRetryPolicy = () =>
            {
                var shouldRetryInner = RetryPolicies.Retry(10, TimeSpan.Zero)();
                return (int rc, Exception ex, out TimeSpan d) =>
                {
                    var result = shouldRetryInner(rc, ex, out d);
                    if (result)
                        OurTrace.TraceWarning(string.Format("Retrying per retry policy (retry {0}, delay {1}) for exception:\n{2}", rc, ex, d));
                    return result;
                };
            };

            var cloudBlobClient = new CloudBlobClient(baseAddress, credentials)
            {
                RetryPolicy = myRetryPolicy,
                Timeout = TimeSpan.FromMinutes(15),
            };

            var blobRef = cloudBlobClient.GetBlockBlobReference(packageUrl);
            blobRef.Container.CreateIfNotExist();
            blobRef.UploadFile(packageFileName);
        }
    }
}
