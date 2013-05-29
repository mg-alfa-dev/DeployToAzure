using System;
using System.IO;
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

        public void ChangeVMSizes(string newVMSize)
        {
            var rootElement = XDocument.Load(PartStream.Stream);
            foreach (var element in rootElement.Elements().Elements())
                element.SetAttributeValue("vmsize", newVMSize);

            PartStream.Stream.Position = 0;
            rootElement.Save(PartStream.Stream);
            PartStream.Stream.SetLength(PartStream.Stream.Position);

            Manifest.SetHash(PartStream.Rel.TargetUri, PartStream.ComputeHash());
        }
    }
}