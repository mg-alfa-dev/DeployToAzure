using System.IO;
using System.IO.Packaging;

namespace DeployToAzure
{
    public static class VMSizeChanger
    {
        public static void ChangeVMSize(string tempPackageFilePath, string newVMSize)
        {
            using (var tracker = new DisposeTracker())
            {
                var package = Package.Open(tempPackageFilePath, FileMode.Open, FileAccess.ReadWrite);
                tracker.Track(package);

                var serviceDescPart = package.GetPartStream("SERVICEDESCRIPTION");
                tracker.Track(serviceDescPart);

                var rootManifest = package.GetManifest();
                tracker.Track(rootManifest);

                var serviceDescPackage = Package.Open(serviceDescPart.Stream, FileMode.Open, FileAccess.ReadWrite);
                tracker.Track(serviceDescPackage);

                var csdefPart = serviceDescPackage.GetServiceDefinition();
                tracker.Track(csdefPart);

                csdefPart.ChangeVMSizes(newVMSize);
                serviceDescPackage.Flush();

                rootManifest.SetHash(serviceDescPart.Rel.TargetUri, serviceDescPart.ComputeHash());
                package.Flush();
            }
        }
    }
}