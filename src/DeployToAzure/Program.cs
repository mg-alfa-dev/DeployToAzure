﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DeployToAzure.Management;
using DeployToAzure.Utility;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Auth;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage.DataMovement;

namespace DeployToAzure
{
    static class Program
    {
        static async Task<int> Main(string[] args)
        {
            var consoleTraceListener = new OurConsoleTraceListener();
            OurTrace.AddListener(consoleTraceListener);
            OurTrace.Source.Switch.Level = SourceLevels.All;

            if(args.Length < 1)
            {
                Usage();
                return 0;
            }

            var dependenciesOnly = false;
            var tryToUseUpgradeDeployment = false;
            var fallbackToReplaceDeployment = false;
            var doNotRedeploy = false;
            if (args.Length > 1)
            {
                if (args.Contains("--upload-dependencies-only"))
                    dependenciesOnly = true;
                if (args.Contains("--try-to-use-upgrade-deployment"))
                    tryToUseUpgradeDeployment = true;
                if (args.Contains("--fallback-to-replace-deployment"))
                    fallbackToReplaceDeployment = true;
                if (args.Contains("--delete"))
                    doNotRedeploy = true;
            }

            var configuration = ConfigurationParser.ParseConfiguration(args[0]);
            var certificate = new X509Certificate2(configuration.CertFileName, configuration.CertPassword);
            var http = new Http(certificate);
            var azureDeploymentDeploymentLowLevelApi = new AzureManagementLowLevelApi(http);
            var managementApiWithRetries = new AzureManagementApiWithRetries(azureDeploymentDeploymentLowLevelApi, configuration.MaxRetries, TimeSpan.FromSeconds(configuration.RetryIntervalInSeconds));

            try
            {
                if (dependenciesOnly)
                {
                    if (configuration.StorageAccountKey == null || configuration.StorageAccountName == null)
                    {
                        OurTrace.TraceError("StorageAccountKey and StorageAccountName required for dependency upload.");
                    }
                    await DeployBlobs(configuration.BlobPathToDeploy, configuration.StorageAccountName, configuration.StorageAccountKey);
                    return 0;
                }

                var deploymentSlotManager = new AzureDeploymentSlot(managementApiWithRetries, configuration.DeploymentSlotUri);
                if (doNotRedeploy)
                {
                    deploymentSlotManager.DeleteDeployment();
                    return 0;
                }

                var subscriptionId = configuration.DeploymentSlotUri.SubscriptionId;
                if (configuration.StorageAccountKey == null || configuration.StorageAccountName == null)
                {
                    OurTrace.TraceInfo("Attempting to guess account name and key based on certificate.");

                    if (string.IsNullOrWhiteSpace(configuration.StorageAccountName))
                    {
                        OurTrace.TraceInfo("Looking up storage accounts for the subscription.");
                        var storageAccounts = azureDeploymentDeploymentLowLevelApi.ListStorageAccounts(subscriptionId);
                        configuration.StorageAccountName = storageAccounts.FirstOrDefault();

                        if (string.IsNullOrWhiteSpace(configuration.StorageAccountName))
                        {
                            OurTrace.TraceError("Couldn't find any suitable storage accounts.");
                            throw new InvalidOperationException("No suitable storage accounts.");
                        }

                        if (!string.IsNullOrWhiteSpace(configuration.BlobPathToDeploy))
                        {
                            // don't allow BlobPathToDeploy if we're guessing the storage account.
                            OurTrace.TraceInfo("Ignoring BlobPathToDeploy because we're guessing the storage account.");
                            configuration.BlobPathToDeploy = null;
                        }
                    }

                    if (string.IsNullOrWhiteSpace(configuration.StorageAccountKey))
                    {
                        OurTrace.TraceInfo($"Looking up storage keys for account: {configuration.StorageAccountName}");
                        var storageKeys = azureDeploymentDeploymentLowLevelApi.GetStorageAccountKeys(subscriptionId, configuration.StorageAccountName);

                        configuration.StorageAccountKey = storageKeys.FirstOrDefault();

                        if (string.IsNullOrWhiteSpace(configuration.StorageAccountKey))
                        {
                            OurTrace.TraceError($"Couldn't find any keys for storage account: {configuration.StorageAccountName}");
                            throw new InvalidOperationException("No suitable storage account keys.");
                        }
                    }
                }

                var csPkg = ConfigureDeploymentPackage(configuration, configuration.PackageFileName);

                await Task.WhenAll(
                    UploadBlob(csPkg, configuration.PackageUrl, configuration.StorageAccountName, configuration.StorageAccountKey),
                    DeployBlobs(configuration.BlobPathToDeploy, configuration.StorageAccountName, configuration.StorageAccountKey)
                );


                if (tryToUseUpgradeDeployment && managementApiWithRetries.DoesDeploymentExist(configuration.DeploymentSlotUri))
                {
                    try
                    {
                        deploymentSlotManager.UpgradeDeployment(configuration);
                    }
                    catch (BadRequestException ex)
                    {
                        OurTrace.TraceError($"Upgrade failed with message: {ex}\r\n, **** {(fallbackToReplaceDeployment ? "falling back to replace." : "exiting.")}");
                        // retry using CreateOrReplaceDeployment, since we might have tried to do something that isn't allowed with UpgradeDeployment.
                        if (fallbackToReplaceDeployment)
                            deploymentSlotManager.CreateOrReplaceDeployment(configuration);
                        else
                            throw;
                    }
                }
                else
                {
                    deploymentSlotManager.CreateOrReplaceDeployment(configuration);
                }

                DeleteBlob(configuration.PackageUrl, configuration.StorageAccountName, configuration.StorageAccountKey);
            }
            catch (Exception ex)
            {
                OurTrace.TraceError($"exception!\n{ex}");
                return -1;
            }
            return 0;
        }

        private static string ConfigureDeploymentPackage(DeploymentConfiguration configuration, string csPkg)
        {
            var hasSpecificVmSizes = !string.IsNullOrWhiteSpace(configuration.ChangeWebRoleVMSize) &&
                                     !string.IsNullOrWhiteSpace(configuration.ChangeWorkerRoleVMSize);
            var hasAllVmSize = !string.IsNullOrWhiteSpace(configuration.ChangeVMSize);

            if (!hasAllVmSize && !hasSpecificVmSizes)
            {
                return csPkg;
            }

            csPkg = Path.GetTempFileName();
            File.Copy(configuration.PackageFileName, csPkg, true);

            if (hasSpecificVmSizes)
            {
                OurTrace.TraceInfo("Role specific VM Sizes specified. We will use these.");
                ChangeVmSize(csPkg, configuration.ChangeWebRoleVMSize, configuration.ChangeWorkerRoleVMSize);
                return csPkg;
            }

            OurTrace.TraceInfo("Only generic role sizes specified. Using them for all roles.");
            ChangeVmSize(csPkg, configuration.ChangeVMSize);
            return csPkg;
        }

        private static void ChangeVmSize(string tempPackageFilePath, string webRoleVmSize, string workerRoleVmSize)
        {
            if (!ValidateVmSize(webRoleVmSize) && !ValidateVmSize(workerRoleVmSize))
            {
                OurTrace.TraceError($"Invalid vmsizes: webRoleVM -> ({webRoleVmSize}), workerRoleVM -> ({workerRoleVmSize}) - should be one of ({string.Join(" | ", _ValidVmSizeValues)})");
                Environment.Exit(-2);
            }

            VMSizeChanger.ChangeVmSize(tempPackageFilePath, webRoleVmSize, workerRoleVmSize);
        }

        private static void ChangeVmSize(string tempPackageFilePath, string newVmSize)
        {
            if (!ValidateVmSize(newVmSize))
            {
                OurTrace.TraceError($"Invalid vmsize: ({newVmSize}) - should be ({string.Join(" | ", _ValidVmSizeValues)})");
                Environment.Exit(-2);
            }

            VMSizeChanger.ChangeVmSize(tempPackageFilePath, newVmSize);
        }

        private static bool ValidateVmSize(string newVmSize)
        {
            return _ValidVmSizeValues.Contains(newVmSize);
        }

        private static void Usage()
        {
            Console.WriteLine("Usage: DeployToAzure <parameters file name> [--upload-dependencies-only] [--delete] [--try-to-use-upgrade-deployment] [--fallback-to-replace-deployment]");
            Console.WriteLine("  Upload Dependencies Only will only deploy the <BlobPathToDeploy> blobs to the storage account.");
            Console.WriteLine("  Delete parameter will cause deployment to be deleted and not redeployed");
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
            Console.WriteLine("    <MaxRetries>(number of times to retry any operation)</MaxRetries>");
            Console.WriteLine("    <RetryIntervalInSeconds>(time to wait between retries of operations (in seconds))</RetryIntervalInSeconds>");
            Console.WriteLine("    <BlobPathToDeploy>(path to blobs that also need deployment (optional))</BlobPathToDeploy>");
            Console.WriteLine("    <ChangeVmSize>(vm size to change to (optional). Mutually exclusive from params 'ChangeWebRoleVMSize' and 'ChangeWorkerRoleVMSize')</ChangeVmSize>");
            Console.WriteLine("    <ChangeWebRoleVMSize>(vm size to change to (optional))</ChangeWebRoleVMSize>");
            Console.WriteLine("    <ChangeWorkerRoleVMSize>(vm size to change to (optional))</ChangeWorkerRoleVMSize>");
            Console.WriteLine("  </Params>");
        }

        private static void DeleteBlob(string packageUrl, string storageAccountName, string storageAccountKey)
        {
            OurTrace.TraceInfo($"Deleting blob {packageUrl}");

            var packageUri = new Uri(packageUrl);
            var credentials = new StorageCredentials(storageAccountName, storageAccountKey);

            var blobRef = new CloudBlockBlob(packageUri, credentials);
            blobRef.DeleteIfExists();
        }

        private static Task UploadBlob(string packageFileName, string packageUrl, string storageAccountName, string storageAccountKey)
        {
            OurTrace.TraceInfo($"Uploading blob from {packageFileName} to {packageUrl}");

            var packageUri = new Uri(packageUrl);
            var credentials = new StorageCredentials(storageAccountName, storageAccountKey);
            var blobRef = new CloudBlockBlob(packageUri, credentials);

            blobRef.ServiceClient.DefaultRequestOptions.ServerTimeout = TimeSpan.FromMinutes(15);
            blobRef.ServiceClient.DefaultRequestOptions.MaximumExecutionTime = blobRef.ServiceClient.DefaultRequestOptions.ServerTimeout;
            blobRef.Container.CreateIfNotExists();

            return TransferManager.UploadAsync(packageFileName, blobRef);
        }

        private static async Task DeployBlobs(string blobPathToDeploy, string storageAccountName, string storageAccountKey)
        {
            OurTrace.TraceInfo($"Deploying blobs from {blobPathToDeploy} to {storageAccountName}");

            var credentials = new StorageCredentials(storageAccountName, storageAccountKey);
            var storageAccount = new CloudStorageAccount(credentials, true);
            var client = storageAccount.CreateCloudBlobClient();
            client.DefaultRequestOptions.ServerTimeout = TimeSpan.FromMinutes(15);
            client.DefaultRequestOptions.MaximumExecutionTime = client.DefaultRequestOptions.ServerTimeout; 
            var folders = Directory.GetDirectories(blobPathToDeploy);

            await Task.WhenAll(folders.Select(Path.GetFileName).Select(folder =>
            {
                OurTrace.TraceInfo($"Creating container: {folder}");
                return client.GetContainerReference(folder).CreateIfNotExistsAsync();
            }));

            var files = from folder in folders
                        from file in Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories)
                        let result = Tuple.Create(Path.GetFileName(folder), file.Remove(0, folder.Length + 1), file)
                        select result;

            await Task.WhenAll(files.Select(f =>
            {
                var container = client.GetContainerReference(f.Item1);
                    var blob = container.GetBlockBlobReference(f.Item2);
                    var uploadOptions = new UploadOptions { DestinationAccessCondition = AccessCondition.GenerateEmptyCondition() };
                    var context = new SingleTransferContext { ShouldOverwriteCallbackAsync = (x, y) => Task.FromResult(true) };
                    OurTrace.TraceInfo($"Uploading Blob: {f.Item3} => {f.Item1}:{f.Item2}");
                    return TransferManager.UploadAsync(f.Item3, blob, uploadOptions, context);
            }));
        }

        private static readonly HashSet<string> _ValidVmSizeValues = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase)
        {
            "ExtraSmall",
            "Small",
            "Medium",
            "Large",
            "ExtraLarge",
            "Standard_A5",
            "Standard_A6",
            "Standard_A7",
            "Standard_A8",
            "Standard_A9",
            "Standard_A10",
            "Standard_A11",
            "Standard_D1",
            "Standard_D2",
            "Standard_D3",
            "Standard_D4",
            "Standard_D11",
            "Standard_D12",
            "Standard_D13",
            "Standard_D14",
            "Standard_D1_v2",
            "Standard_D2_v2",
            "Standard_D3_v2",
            "Standard_D4_v2",
            "Standard_D5_v2",
            "Standard_D11_v2",
            "Standard_D12_v2",
            "Standard_D13_v2",
            "Standard_D14_v2",
            "Standard_DS1",
            "Standard_DS2",
            "Standard_DS3",
            "Standard_DS4",
            "Standard_DS11",
            "Standard_DS12",
            "Standard_DS13",
            "Standard_DS14",
            "Standard_G1",
            "Standard_G2",
            "Standard_G3",
            "Standard_G4",
            "Standard_G5",
            "Standard_GS1",
            "Standard_GS2",
            "Standard_GS3",
            "Standard_GS4",
            "Standard_GS5",
        };
    }
}
