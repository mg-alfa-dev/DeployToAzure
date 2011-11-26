using DeployToAzure.Management;
using DeployToAzure.Tests.TestUtilities;

namespace DeployToAzure.Tests.Management
{
    public class ScriptedHttpFake: ScriptedTestDouble, IHttp
    {
        public HttpResponse NextResponse;

        public string LastPostUri;
        public string LastPostContent;

        public string LastDeleteUri;

        public string LastGetUri;

        public HttpResponse Delete(string uri)
        {
            LastDeleteUri = uri;
            RunScript();
            return NextResponse;
        }

        public HttpResponse Post(string uri, string content)
        {
            LastPostUri = uri;
            LastPostContent = content;
            RunScript();
            return NextResponse;
        }

        public HttpResponse Get(string uri)
        {
            LastGetUri = uri;
            RunScript();
            return NextResponse;
        }
    }
}