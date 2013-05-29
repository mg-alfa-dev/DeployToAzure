using System.IO;
using System.IO.Packaging;

namespace DeployToAzure
{
    public static class PackagingExtensions
    {
        public static PartStream GetPartStream(this Package package, string relationship)
        {
            var rel = package.GetRelationship(relationship);
            var part = package.GetPart(rel.TargetUri);
            var stm = part.GetStream(FileMode.Open, FileAccess.ReadWrite);

            return new PartStream(rel, part, stm);
        }

        public static PackageManifest GetManifest(this Package package)
        {
            var stm = GetPartStream(package, "MANIFEST");
            return new PackageManifest(stm);
        }

        public static ServiceDefinition GetServiceDefinition(this Package package)
        {
            var partStm = package.GetPartStream("SERVICEDESCRIPTION");
            try
            {
                var manifest = package.GetManifest();
                return new ServiceDefinition(manifest, partStm);
            }
            catch
            {
                partStm.Dispose();
                throw;
            }
        }
    }
}