using System.IO;
using DeployToAzure.Management;
using NUnit.Framework;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnaccessedField.Global
// ReSharper disable InconsistentNaming
// ReSharper disable PossibleNullReferenceException
// ReSharper disable MemberCanBeProtected.Global
// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable ConvertToConstant.Local
// ReSharper disable FieldCanBeMadeReadOnly.Local

namespace DeployToAzure.Tests.ConfigurationParserTests
{
    [TestFixture]
    public class ConfigurationParserTests
    {
        private string configurationFileName;
        private DeploymentConfiguration configuration;
        private string serviceConfigurationPath;

        [TestFixtureSetUp]
        public void SetUp()
        {
            configurationFileName = Path.GetTempFileName();
            serviceConfigurationPath = Path.GetTempFileName();
            using (var writer = new StreamWriter(configurationFileName))
            {
                writer.WriteLine("  <Params>");
                writer.WriteLine("    <SubscriptionId>subscription id</SubscriptionId>");
                writer.WriteLine("    <ServiceName>service name</ServiceName>");
                writer.WriteLine("    <DeploymentSlot>deployment slot</DeploymentSlot>");
                writer.WriteLine("    <StorageAccountName>storage account name</StorageAccountName>");
                writer.WriteLine("    <StorageAccountKey>storage account key</StorageAccountKey>");
                writer.WriteLine("    <CertFileName>cert file name</CertFileName>");
                writer.WriteLine("    <CertPassword>cert password</CertPassword>");
                writer.WriteLine("    <PackageFileName>package file name</PackageFileName>");
                writer.WriteLine("    <ServiceConfigurationPath>{0}</ServiceConfigurationPath>",serviceConfigurationPath);
                writer.WriteLine("    <DeploymentLabel>deployment label</DeploymentLabel>");
                writer.WriteLine("    <DeploymentName>deployment name</DeploymentName>");
                writer.WriteLine("    <RoleName>role name</RoleName>");
                writer.WriteLine("    <Force>true</Force>");
                writer.WriteLine("  </Params>");
            }

            using (var serviceConfig = new StreamWriter(serviceConfigurationPath))
                serviceConfig.Write("service config contents and stuff");

            configuration = ConfigurationParser.ParseConfiguration(configurationFileName);
        }

        [TestFixtureTearDown]
        public void TearDown()
        {
            if (File.Exists(configurationFileName))
                File.Delete(configurationFileName);
            if (File.Exists(serviceConfigurationPath))
                File.Delete(serviceConfigurationPath);
        }

        [Test]
        public void it_parses_the_cert_file_name()
        {
            Assert.That(configuration.CertFileName, Is.EqualTo("cert file name"));
        } 

        [Test]
        public void it_parses_the_deployment_label()
        {
            Assert.That(configuration.DeploymentLabel, Is.EqualTo("deployment label"));
        }

        [Test]
        public void it_parses_the_deployment_name()
        {
            Assert.That(configuration.DeploymentName, Is.EqualTo("deployment name"));
        }

        [Test]
        public void it_parses_the_deployment_slot_uri()
        {
            Assert.That(configuration.DeploymentSlotUri, Is.EqualTo(new DeploymentSlotUri("subscription id", "service name", "deployment slot")));
        }

        [Test]
        public void it_parses_the_package_url()
        {
           Assert.That(configuration.PackageUrl, Is.StringMatching(@"^https://storage account name.blob.core.windows.net/deployment-package/[a-f0-9\\-]{36}\.cspkg$"));
        }

        [Test]
        public void it_parses_the_service_configuration_file_contents()
        {
           Assert.That(configuration.ServiceConfiguration, Is.EqualTo("service config contents and stuff"));
        }

        [Test]
        public void it_parses_the_package_file_name()
        {
           Assert.That(configuration.PackageFileName, Is.EqualTo("package file name"));
        }
        
        [Test]
        public void it_parses_the_storage_account_key()
        {
           Assert.That(configuration.StorageAccountKey, Is.EqualTo("storage account key"));
        }
      
        [Test]
        public void it_parses_the_cert_password()
        {
           Assert.That(configuration.CertPassword, Is.EqualTo("cert password"));
        }

        [Test]
        public void it_parses_the_role_name()
        {
           Assert.That(configuration.RoleName, Is.EqualTo("role name"));
        }

        [Test]
        public void it_parses_the_force_setting()
        {
            Assert.That(configuration.Force);
        }
    }
}
