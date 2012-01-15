using System;
using System.Diagnostics;
using System.Linq;
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

            if(args.Length < 1)
            {
                Usage();
                return 0;
            }

            var tryToUseUpgradeDeployment = false;
            var fallbackToReplaceDeployment = false;
            if (args.Length > 1)
            {
                if (args.Contains("--try-to-use-upgrade-deployment"))
                    tryToUseUpgradeDeployment = true;
                if (args.Contains("--fallback-to-replace-deployment"))
                    fallbackToReplaceDeployment = true;
            }

            var configuration = ConfigurationParser.ParseConfiguration(args[0]);
            var certificate = new X509Certificate2(configuration.CertFileName, configuration.CertPassword);
            var http = new Http(certificate);
            var azureDeploymentDeploymentLowLevelApi = new AzureManagementLowLevelApi(http);
            var managementApiWithRetries = new AzureManagementApiWithRetries(azureDeploymentDeploymentLowLevelApi, configuration.MaxRetries, TimeSpan.FromSeconds(configuration.RetryIntervalInSeconds));

            UploadBlob(configuration.PackageFileName, configuration.PackageUrl, configuration.StorageAccountName, configuration.StorageAccountKey);

            var deploymentSlotManager = new AzureDeploymentSlot(managementApiWithRetries, configuration.DeploymentSlotUri);
            try
            {
                if (tryToUseUpgradeDeployment && managementApiWithRetries.DoesDeploymentExist(configuration.DeploymentSlotUri))
                {
                    try
                    {
                        deploymentSlotManager.UpgradeDeployment(configuration);
                    }
                    catch(BadRequestException ex)
                    {
                        OurTrace.TraceError(string.Format("Upgrade failed with message: {0}\r\n, **** {1}", ex, fallbackToReplaceDeployment ? "falling back to replace." : "exiting."));
                        // retry using CreateOrReplaceDeployment, since we might have tried to do something that isn't allowed with UpgradeDeployment.
                        if (fallbackToReplaceDeployment)
                            deploymentSlotManager.CreateOrReplaceDeployment(configuration);
                        else
                            throw;
                    }
                }
                else
                    deploymentSlotManager.CreateOrReplaceDeployment(configuration);

                DeleteBlob(configuration.PackageUrl, configuration.StorageAccountName, configuration.StorageAccountKey);
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
            Console.WriteLine("    <RoleName>(role name)</RoleName>");
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
