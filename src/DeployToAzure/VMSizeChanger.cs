using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;

namespace DeployToAzure
{
    public static class VMSizeChanger
    {
        public static void ChangeVmSize(string tempPackageFilePath, string newVMSize)
        {
            _ExecuteChangeVMSize(tempPackageFilePath, new[]
            {
                new VmSizeChange
                {
                    Name = "WebRole",
                    Value = newVMSize
                },
                new VmSizeChange
                {
                    Name = "WorkerRole",
                    Value = newVMSize
                }
            });
        }

        public static void ChangeVmSize(string tempPackageFilePath, string webRoleVmSize, string workerRoleVmSize)
        {
            _ExecuteChangeVMSize(tempPackageFilePath, new[]
            {
                new VmSizeChange
                {
                    Name = "WebRole",
                    Value = webRoleVmSize
                },
                new VmSizeChange
                {
                    Name = "WorkerRole",
                    Value = workerRoleVmSize
                }
            });
        }

        private static void _ExecuteChangeVMSize(string tempPackageFilePath, IEnumerable<VmSizeChange> vmChanges)
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

                foreach (var vmSizeChange in vmChanges)
                {
                    csdefPart.ChangeVmSize(vmSizeChange.Name, vmSizeChange.Value);
                }

                serviceDescPackage.Flush();

                rootManifest.SetHash(serviceDescPart.Rel.TargetUri, serviceDescPart.ComputeHash());
                package.Flush();
            }
        }
    }

    public class VmSizeChange
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }
}