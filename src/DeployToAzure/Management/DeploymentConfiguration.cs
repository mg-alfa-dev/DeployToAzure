using System;
using System.Text;

namespace DeployToAzure.Management
{
    public interface IDeploymentConfiguration
    {
        string MakeCreateDeploymentMessage();
    }

    public class DeploymentConfiguration : IDeploymentConfiguration
    {
        private readonly Guid _PackageGuid = Guid.NewGuid();

        public string PackageUrl
        {
            get
            {
                return string.Format("https://{0}.blob.core.windows.net/deployment-package/{1}.cspkg",
                    StorageAccountName,
                    _PackageGuid);
            }
        }

        public string DeploymentLabel;
        public string DeploymentName;
        public string RoleName;
        public bool Force;

        public string ServiceConfiguration;
        public string PackageFileName;
        public string StorageAccountName;
        public string StorageAccountKey;
        public string CertFileName;
        public string CertPassword;
        public DeploymentSlotUri DeploymentSlotUri;
        public int MaxRetries;
        public int RetryIntervalInSeconds;
        public string BlobPathToDeploy;

        public string MakeCreateDeploymentMessage()
        {
            const string format = @"<?xml version=""1.0"" encoding=""utf-8""?>
<CreateDeployment xmlns=""http://schemas.microsoft.com/windowsazure"">
    <Name>{0}</Name>
    <PackageUrl>{1}</PackageUrl>
    <Label>{2}</Label>
    <Configuration>{3}</Configuration>
    <StartDeployment>true</StartDeployment>
</CreateDeployment>";

            var label = Convert.ToBase64String(Encoding.ASCII.GetBytes(DeploymentLabel));
            return string.Format(format, DeploymentName, PackageUrl, label, Convert.ToBase64String(Encoding.ASCII.GetBytes(ServiceConfiguration)));
        }

        public string MakeUpgradeDeploymentMessage()
        {
            const string format =
                @"<?xml version=""1.0"" encoding=""utf-8""?>
<UpgradeDeployment xmlns=""http://schemas.microsoft.com/windowsazure"">
    <Mode>Auto</Mode>
    <PackageUrl>{0}</PackageUrl>
    <Configuration>{1}</Configuration>
    <Label>{2}</Label>
    <Force>{3}</Force>
</UpgradeDeployment>";

            var label = Convert.ToBase64String(Encoding.ASCII.GetBytes(DeploymentLabel));
            return string.Format(format, PackageUrl, 
                Convert.ToBase64String(Encoding.ASCII.GetBytes(ServiceConfiguration)), 
                label, Force.ToString().ToLower());
        }
    }
}