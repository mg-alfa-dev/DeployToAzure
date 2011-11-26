using System;
using DeployToAzure.Utility;
using NUnit.Framework;

namespace DeployToAzure.Tests.Utility
{
    [TestFixture]
    public class SpinLoopTests
    {
        [Test]
        public void DoUntil_SpinsUntilCancelled()
        {
            var executeCount = 0;
            using (SpinLoop.ForTests(sleepMilliseconds => executeCount++))
            {
                SpinLoop.DoUntil(
                    () =>
                        {
                            if (executeCount > 4) throw new Exception("Oops!");
                        },
                    () => executeCount == 3, 
                    2500);

                Assert.That(executeCount, Is.EqualTo(3), "should have iterated exactly 3 times");
            }
        }

        [Test]
        public void WaitUntil_SpinsUntilCancelled()
        {
            var executeCount = 3;
            using (SpinLoop.ForTests(sleepMilliseconds => --executeCount))
                SpinLoop.WaitUntil(() => executeCount == 0, 2500);

            Assert.That(executeCount, Is.EqualTo(0));
        }

        [Test]
        public void DoUntil_DoesntSleepIfSuccessful()
        {
            var sleepCount = 0;
            var executeCount = 0;
            using(SpinLoop.ForTests(sleepMilliseconds => sleepCount++))
            {
                SpinLoop.DoUntil(() => executeCount++, () => true, 2500);
            }
            Assert.That(executeCount, Is.EqualTo(1), "execute count");
            Assert.That(sleepCount, Is.EqualTo(0), "sleep count");
        }
    }
}
