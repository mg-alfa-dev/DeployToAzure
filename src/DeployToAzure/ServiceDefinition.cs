using System;
using System.Linq;
using System.Xml.Linq;

namespace DeployToAzure
{
    public class ServiceDefinition: IDisposable
    {
        public readonly PackageManifest Manifest;
        public readonly PartStream PartStream;

        public ServiceDefinition(PackageManifest manifest, PartStream partStream)
        {
            Manifest = manifest;
            PartStream = partStream;
        }

        public void Dispose()
        {
            Manifest.Dispose();
            PartStream.Dispose();
        }

        public void ChangeVmSize(string roleName, string newVmSize)
        {
            PartStream.Stream.Position = 0;
            var rootElement = XDocument.Load(PartStream.Stream);
            var element = rootElement.Elements().Elements().First(x => string.Equals(x.Name.LocalName, roleName, StringComparison.OrdinalIgnoreCase));

            element.SetAttributeValue("vmsize", newVmSize);

            PartStream.Stream.Position = 0;
            rootElement.Save(PartStream.Stream);
            PartStream.Stream.SetLength(PartStream.Stream.Position);

            Manifest.SetHash(PartStream.Rel.TargetUri, PartStream.ComputeHash());
        }
   }
}