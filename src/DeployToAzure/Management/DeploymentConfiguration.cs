using System;
using System.Security;
using System.Text;

namespace DeployToAzure.Management
{
    public interface IDeploymentConfiguration
    {
        string MakeCreateDeploymentMessage();
    }

    public enum UpgradeMode
    {
        Auto, Manual
    }

    public class DeploymentConfiguration : IDeploymentConfiguration
    {
        public UpgradeMode UpgradeMode;
        public string PackageUrl;
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
    <Mode>{0}</Mode>
    <PackageUrl>{1}</PackageUrl>
    <Configuration>{2}</Configuration>
    <Label>{3}</Label>
    <Force>{4}</Force>
</UpgradeDeployment>";

            var label = Convert.ToBase64String(Encoding.ASCII.GetBytes(DeploymentLabel));
            return string.Format(format, UpgradeMode, PackageUrl, 
                Convert.ToBase64String(Encoding.ASCII.GetBytes(ServiceConfiguration)), 
                label, Force.ToString().ToLower());
        }
    }
}