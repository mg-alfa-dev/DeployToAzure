using DeployToAzure.Utility;
using NUnit.Framework;

namespace DeployToAzure.Tests.Utility
{
    [TestFixture]
    public class CompensatingActionTests
    {
        [Test]
        public void CallingDisposePerformsCompensatingAction()
        {
            var wasCalled = false;
            var objectUnderTest = new DisposeAction(() => wasCalled = true);
            objectUnderTest.Dispose();
            Assert.That(wasCalled);
        }
    }
}