using System;
using DeployToAzure.Utility;
using NUnit.Framework;

namespace DeployToAzure.Tests.Utility
{
    [TestFixture]
    public class RetryLogicTests
    {
        [Test]
        public void DefaultSleepImplementationRunsForInterval_WithExceptions()
        {
            var sleepTime = 0;
            var expectedInnerMessage = "foo";
            using(SpinLoop.ForTests(sleepMilliseconds => { sleepTime += sleepMilliseconds; }))
            {
                var retryLogic = new RetryLogic(1, 500);
                try
                {
                    retryLogic.Execute(
                        () => { throw new Exception(expectedInnerMessage); },
                        ex => RetryOrRethrow.Retry);
                }
                catch (Exception ex)
                {
                    Assert.That(sleepTime, Is.EqualTo(500));
                    Assert.That(ex, Is.TypeOf<MaxRetriesExceededException>());
                    Assert.That(ex.InnerException, Is.Not.Null);
                    Assert.That(ex.InnerException.Message, Is.EqualTo(expectedInnerMessage));
                }
            }
        }

        [Test]
        public void DefaultSleepImplementationRunsForInterval_RetriesWithNoException()
        {
            var sleepTime = 0;
            using (SpinLoop.ForTests(sleepMilliseconds => { sleepTime += sleepMilliseconds; }))
            {
                var retryLogic = new RetryLogic(1, 500);
                try
                {
                    retryLogic.Execute(
                        () => {},
                        ex => RetryOrRethrow.Retry,
                        () => false);
                }
                catch (Exception ex)
                {
                    Assert.That(sleepTime, Is.EqualTo(500));
                    Assert.That(ex, Is.TypeOf<MaxRetriesExceededException>());
                    Assert.That(ex.InnerException, Is.Null);
                }
            }
        }

        [Test]
        public void DefaultSleepImplementationRunsForInterval_RethrowsWithNoException()
        {
            var sleepTime = 0;
            using (SpinLoop.ForTests(sleepMilliseconds => { sleepTime += sleepMilliseconds; }))
            {
                var retryLogic = new RetryLogic(1, 500);
                try
                {
                    retryLogic.Execute(
                        () => { },
                        ex => RetryOrRethrow.Rethrow,
                        () => false);
                }
                catch (Exception ex)
                {
                    Assert.That(ex, Is.TypeOf<MaxRetriesExceededException>());
                    Assert.That(ex.InnerException, Is.Null);
                    Assert.That(sleepTime, Is.EqualTo(500));
                }
            }
        }

        [Test]
        public void WhenSuccessfulOnFirstTry_ItDoesntCallExceptionFilter_AndDoesntSleep_AndCallsOperation()
        {
            var sleepCount = 0;
            using(SpinLoop.ForTests(sleepTime => sleepCount++))
            {
                var retryLogic = new RetryLogic(1, 500);
                var wasCalled = false;
                retryLogic.Execute(
                    () => { wasCalled = true; },
                    ex =>
                    {
                        Assert.Fail("shouldn't get called");
                        return RetryOrRethrow.Rethrow;
                    });

                Assert.That(wasCalled);
                Assert.That(sleepCount, Is.EqualTo(0));
            }
        }

        [Test]
        public void RetriesWhenRequestedOnException()
        {
            var sleepCount = 0;
            using (SpinLoop.ForTests(sleepTime => sleepCount++))
            {
                var retryLogic = new RetryLogic(1, 500);
                var callCount = 0;
                retryLogic.Execute(
                    () =>
                    {
                        callCount++;
                        if (callCount == 1)
                            throw new InvalidOperationException("foo");
                    },
                    ex => RetryOrRethrow.Retry);
                Assert.That(callCount, Is.EqualTo(2), "call count");
                Assert.That(sleepCount, Is.EqualTo(1), "sleep count");
            }
        }

        [Test]
        public void RetriesTheExpectedNumberOfTimes()
        {
            var sleepCount = 0;
            using(SpinLoop.ForTests(sleepTime => sleepCount++))
            {
                var retryLogic = new RetryLogic(3, 500);
                var callCount = 0;
                Assert.That(() => retryLogic.Execute(
                    () =>
                    {
                        callCount++;
                        throw new Exception("foo");
                    },
                    ex => RetryOrRethrow.Retry),
                    Throws.Exception.TypeOf<MaxRetriesExceededException>().With.InnerException.Message.EqualTo("foo"));

                Assert.That(callCount, Is.EqualTo(4), "call count");
                Assert.That(sleepCount, Is.EqualTo(3), "sleep count");
            }
        }

        [Test]
        public void RethrowsIfRequested()
        {
            var sleepCount = 0;
            using(SpinLoop.ForTests(sleepTime => sleepCount++))
            {
                var retryLogic = new RetryLogic(3, 500);
                var callCount = 0;
                Assert.That(
                    () => retryLogic.Execute(
                        () =>
                        {
                            callCount++;
                            throw new Exception("foo");
                        },
                        ex => RetryOrRethrow.Rethrow),
                    Throws.Exception.With.Message.EqualTo("foo"));
                Assert.That(callCount, Is.EqualTo(1));
                Assert.That(sleepCount, Is.EqualTo(0));
            }
        }

        [Test]
        public void RetriesIfSuccessTestReturnsFalse()
        {
            var sleepCount = 0;
            using(SpinLoop.ForTests(sleepTime => sleepCount++))
            {
                var retryLogic = new RetryLogic(3, 500);
                var callCount = 0;
                retryLogic.Execute(
                    () => callCount++,
                    ex => RetryOrRethrow.Retry,
                    () => callCount != 1);

                Assert.That(callCount, Is.EqualTo(2), "should have retried once (after the first call)");
            }
        }

        [Test]
        public void RetriesDoesntSleepIfSucceedsOnFirstTry()
        {
            var sleepCount = 0;
            using (SpinLoop.ForTests(sleepTime => sleepCount++))
            {
                var retryLogic = new RetryLogic(3, 500);
                var callCount = 0;
                retryLogic.Execute(
                    () => callCount++,
                    ex => RetryOrRethrow.Retry,
                    () => true);

                Assert.That(callCount, Is.EqualTo(1), "should have retried once (after the first call)");
                Assert.That(sleepCount, Is.EqualTo(0), "shouldn't sleep");
            }
        }
    }
}