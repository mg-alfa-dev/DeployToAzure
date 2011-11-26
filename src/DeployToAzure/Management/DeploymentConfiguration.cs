using System;
using System.Text;

namespace DeployToAzure.Management
{
    public interface IDeploymentConfiguration
    {
        string ToXmlString();
    }

    public class DeploymentConfiguration : IDeploymentConfiguration
    {
        public string PackageUrl;
        public string DeploymentLabel;
        public string DeploymentName;

        public string ServiceConfiguration;

        public string ToXmlString()
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
    }
}