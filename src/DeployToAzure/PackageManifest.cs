using System;
using System.Linq;
using System.Xml.Linq;

namespace DeployToAzure
{
    public class PackageManifest: IDisposable
    {
        public readonly PartStream ManifestStream;

        public PackageManifest(PartStream manifestStream)
        {
            ManifestStream = manifestStream;
        }

        public void SetHash(Uri targetUri, byte[] sha2Hash)
        {
            var hashText = string.Join("", sha2Hash.Select(x => x.ToString("X2")));
            var manifestXml = XDocument.Load(ManifestStream.Stream);
            var uriString = targetUri.OriginalString;
            var targetItem = manifestXml
                .Elements("PackageManifest")
                .Elements("Contents")
                .Elements("Item")
                .Attributes("uri")
                .Where(x => x.Value == uriString)
                .Select(x => x.Parent)
                .FirstOrDefault();

            if(targetItem == null)
                throw new InvalidOperationException(string.Format("item not found with targetUri: {0}", targetUri));

            targetItem.SetAttributeValue("hash", hashText);
            ManifestStream.Stream.Position = 0;
            manifestXml.Save(ManifestStream.Stream);
            ManifestStream.Stream.SetLength(ManifestStream.Stream.Position);
        }

        public void Dispose()
        {
            ManifestStream.Dispose();
        }
    }
}