using System;
using System.IO;
using System.IO.Packaging;
using System.Security.Cryptography;

namespace DeployToAzure
{
    public class PartStream: IDisposable 
    {
        public readonly PackageRelationship Rel;
        public readonly PackagePart Part;
        public readonly Stream Stream;

        public PartStream(PackageRelationship rel, PackagePart part, Stream stream)
        {
            Rel = rel;
            Part = part;
            Stream = stream;
        }

        public void Dispose()
        {
            Stream.Dispose();
        }

        public byte[] ComputeHash()
        {
            var sha2 = new SHA256Managed();
            Stream.Position = 0;
            return sha2.ComputeHash(Stream);
        }
    }
}