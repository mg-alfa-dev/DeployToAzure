using System;
using DeployToAzure.Utility;
using NUnit.Framework;

namespace DeployToAzure.Tests.Utility
{
    [TestFixture]
    public class ExceptionExtensionsTests
    {
        [Test]
        public void ShouldWrite()
        {
            var inner1Ex = new NullReferenceException("inner1Message");
            var inner2Ex = new InvalidOperationException("inner2Message", inner1Ex);
            var outerEx = new Exception("outerMessage", inner2Ex);

            var expectedOutput = @"----------
outerMessage
----------
Debug information:
----------
Exception: outerMessage
[StackTrace:Exception]
----------
inner InvalidOperationException: inner2Message
[StackTrace:InvalidOperationException]
----------
inner NullReferenceException: inner1Message
[StackTrace:NullReferenceException]
----------
";

            Func<Exception, string> stackTraceFormatter = ex => string.Format("[StackTrace:{0}]", ex.GetType().Name);

            var actual = outerEx.ToLogString(stackTraceFormatter);

            Assert.That(expectedOutput, Is.EqualTo(actual));
        }
    }
}
