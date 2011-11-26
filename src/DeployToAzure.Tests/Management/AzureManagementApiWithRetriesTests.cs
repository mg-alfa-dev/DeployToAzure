using System;
using DeployToAzure.Management;
using DeployToAzure.Utility;
using NUnit.Framework;
using Rhino.Mocks;

// ReSharper disable InconsistentNaming
namespace DeployToAzure.Tests.Management
{
    [TestFixture]
    public class AzureManagementApiWithRetriesTests
    {
        private static readonly DeploymentSlotUri FooUri = new DeploymentSlotUri(subscriptionId: "subscriptionId", serviceName: "serviceName", slot: "slot");

        [Test]
        public void DoesDeploymentExist_ReturnsTrueIfRunning()
        {
            var api = new ScriptedAzureManagementLowLevelApiFake();

            api.Script.Add(() => api.NextDeploymentCheckOutcome = AzureDeploymentCheckOutcome.Running);

            var azureManagement = new AzureManagementApiWithRetries(api, 2, TimeSpan.FromMilliseconds(30000));

            Assert.That(azureManagement.DoesDeploymentExist(FooUri), Is.EqualTo(true), "return value");
            Assert.That(api.CheckStatusDeploymentUri, Is.EqualTo(FooUri), "deployment uri");
        }

        [Test]
        public void DoesDeploymentExist_ReturnsTrueIfSuspended()
        {
            var api = new ScriptedAzureManagementLowLevelApiFake();

            api.Script.Add(() => api.NextDeploymentCheckOutcome = AzureDeploymentCheckOutcome.Suspended);

            var azureManagement = new AzureManagementApiWithRetries(api, 2, TimeSpan.FromMilliseconds(30000));

            Assert.That(azureManagement.DoesDeploymentExist(FooUri), Is.EqualTo(true), "return value");
            Assert.That(api.CheckStatusDeploymentUri, Is.EqualTo(FooUri), "deployment uri");
        }

        [Test]
        public void DoesDeploymentExist_ReturnsFalseIfNotFound()
        {
            var api = new ScriptedAzureManagementLowLevelApiFake();

            api.Script.Add(() => api.NextDeploymentCheckOutcome = AzureDeploymentCheckOutcome.NotFound);

            var azureManagement = new AzureManagementApiWithRetries(api, 2, TimeSpan.FromMilliseconds(30000));

            Assert.That(azureManagement.DoesDeploymentExist(FooUri), Is.EqualTo(false), "return value");
            Assert.That(api.CheckStatusDeploymentUri, Is.EqualTo(FooUri), "deployment uri");
        }

        [Test]
        public void DoesDeploymentExist_RetriesOnFailed()
        {
            using (SpinLoop.ForTests(i => { }))
            {
                var api = new ScriptedAzureManagementLowLevelApiFake();

                api.Script.Add(() => api.NextDeploymentCheckOutcome = AzureDeploymentCheckOutcome.Failed);
                api.Script.Add(() => api.NextDeploymentCheckOutcome = AzureDeploymentCheckOutcome.Running);

                var azureManagement = new AzureManagementApiWithRetries(api, 2, TimeSpan.FromMilliseconds(30000));

                Assert.That(azureManagement.DoesDeploymentExist(FooUri), Is.EqualTo(true), "return value");
                Assert.That(api.CheckStatusCounter, Is.EqualTo(2), "check status called the appropriate number of times");
            }
        }

        [Test]
        public void DoesDeploymentExist_ThrowsRetryExceptionIfFailedTooManyTimes()
        {
            var retryElapsedTime = 0;
            using(SpinLoop.ForTests(i => retryElapsedTime += i))
            {
                var api = new ScriptedAzureManagementLowLevelApiFake();

                api.Script.Add(() => api.NextDeploymentCheckOutcome = AzureDeploymentCheckOutcome.Failed);
                api.Script.Add(() => api.NextDeploymentCheckOutcome = AzureDeploymentCheckOutcome.Failed);
                api.Script.Add(() => api.NextDeploymentCheckOutcome = AzureDeploymentCheckOutcome.Failed);

                var azureManagement = new AzureManagementApiWithRetries(
                    api, 2, TimeSpan.FromMilliseconds(20));

                Assert.That(
                    () => azureManagement.DoesDeploymentExist(FooUri),
                    Throws.TypeOf<MaxRetriesExceededException>());

                Assert.That(retryElapsedTime, Is.EqualTo(40));
            }
        }

        [Test]
        public void DoesDeploymentExist_RethrowsIfUnexpectedExceptionThrown()
        {
            using (SpinLoop.ForTests(i => { }))
            {
                var api = new ScriptedAzureManagementLowLevelApiFake();

                api.Script.Add(() => api.NextDeploymentCheckOutcome = AzureDeploymentCheckOutcome.Failed);
                api.Script.Add(() => { throw new ArgumentException(); });

                var azureManagement = new AzureManagementApiWithRetries(
                    api, 2, TimeSpan.FromMilliseconds(30000));

                Assert.That(
                    () => azureManagement.DoesDeploymentExist(FooUri),
                    Throws.ArgumentException);
            }
        }

        [Test]
        public void Suspend_CallsBeginSuspend()
        {
            var api = new ScriptedAzureManagementLowLevelApiFake();

            var wasCalled = false;
            api.Script.Add(() => wasCalled = true);
            api.Script.Add(() => api.NextRequestStatus = AzureRequestStatus.Succeeded);

            var azureManagement = new AzureManagementApiWithRetries(
                api, 2, TimeSpan.FromMilliseconds(30000));
            
            azureManagement.Suspend(FooUri);

            Assert.That(wasCalled, "was called");
            Assert.That(api.BeginSuspendDeploymentUri, Is.EqualTo(FooUri), "expected URI");
        }

        [Test]
        public void Suspend_RetriesOnExpectedExceptionAndThrowsOnUnexpected()
        {
            using (SpinLoop.ForTests(i => { }))
            {
                var api = new ScriptedAzureManagementLowLevelApiFake();

                api.Script.Add(() => { throw new UnhandledHttpException(); });
                api.Script.Add(() => { throw new ArgumentException(); });

                var azureManagement = new AzureManagementApiWithRetries(
                    api, 2, TimeSpan.FromMilliseconds(30000));

                Assert.That(
                    () => azureManagement.Suspend(FooUri), 
                    Throws.ArgumentException);
            }
        }

        [Test]
        public void Suspend_RetriesTheRightNumberOfTimesThenGivesUpAndThrowsIfException()
        {
            var sleepCount = 0;
            using (SpinLoop.ForTests(i => { sleepCount++; }))
            {
                var api = new ScriptedAzureManagementLowLevelApiFake();

                api.Script.Add(() => { throw new UnhandledHttpException(); });
                api.Script.Add(() => { throw new UnhandledHttpException(); });
                api.Script.Add(() => { throw new UnhandledHttpException(); });

                var azureManagement = new AzureManagementApiWithRetries(
                    api, 2, TimeSpan.FromMilliseconds(30000));

                Assert.That(
                    () => azureManagement.Suspend(FooUri), 
                    Throws.TypeOf<MaxRetriesExceededException>().With.InnerException.TypeOf<UnhandledHttpException>());
                Assert.That(sleepCount, Is.EqualTo(2));
            }
        }

        [Test]
        public void Suspend_RetriesTheSuspendWhenRequestStatusReturnsFailed()
        {
            using (SpinLoop.ForTests(i => { }))
            {
                var api = new ScriptedAzureManagementLowLevelApiFake();
                var createCallCount = 0;
                var requestId = 12345;
                Action incrCallCount = () =>
                {
                    createCallCount++;
                    api.NextRequestId = (++requestId).ToString();
                };

                api.Script.Add(incrCallCount);
                api.Script.Add(() => api.NextRequestStatus = AzureRequestStatus.InProgress);
                api.Script.Add(() => api.NextRequestStatus = AzureRequestStatus.Failed);
                api.Script.Add(incrCallCount);
                api.Script.Add(() => api.NextRequestStatus = AzureRequestStatus.Succeeded);

                var azureManagement = new AzureManagementApiWithRetries(
                    api, 2, TimeSpan.FromMilliseconds(30000));

                azureManagement.Suspend(FooUri);
                Assert.That(createCallCount, Is.EqualTo(2));
                var expectedUri = FooUri.ToRequestUri(api.NextRequestId);
                Assert.That(api.LastCheckRequestStatusRequestUri, Is.EqualTo(expectedUri));
            }
        }

        [Test]
        public void Create_CallsCreateWithExpectedArgs()
        {
            var api = new ScriptedAzureManagementLowLevelApiFake();

            var wasCalled = false;
            api.Script.Add(() => wasCalled = true);
            api.Script.Add(() => api.NextRequestStatus = AzureRequestStatus.Succeeded);

            var azureManagement = new AzureManagementApiWithRetries(api, 2, TimeSpan.FromMilliseconds(30000));

            var config = MockRepository.GenerateStub<IDeploymentConfiguration>();
            azureManagement.Create(FooUri, config);

            Assert.That(wasCalled, "was called");
            Assert.That(api.BeginCreateDeploymentUri, Is.EqualTo(FooUri), "deployment uri");
            Assert.That(api.BeginCreateConfiguration, Is.SameAs(config));
        }

        [Test]
        public void Create_RetriesOnExpectedExceptionAndThrowsOnUnexpected()
        {
            using (SpinLoop.ForTests(i => { }))
            {
                var api = new ScriptedAzureManagementLowLevelApiFake();

                api.Script.Add(() => { throw new UnhandledHttpException(); });
                api.Script.Add(() => { throw new ArgumentException(); });
                api.Script.Add(() => api.NextRequestStatus = AzureRequestStatus.Succeeded);

                var azureManagement = new AzureManagementApiWithRetries(
                    api, 2, TimeSpan.FromMilliseconds(30000));

                var config = MockRepository.GenerateStub<IDeploymentConfiguration>();
                Assert.That(
                    () => azureManagement.Create(FooUri, config),
                    Throws.ArgumentException);
            }
        }

        [Test]
        public void Create_RetriesTheRightNumberOfTimesThenGivesUpAndThrowsIfException()
        {
            var sleepCount = 0;
            using (SpinLoop.ForTests(i => { sleepCount++; }))
            {
                var api = new ScriptedAzureManagementLowLevelApiFake();

                api.Script.Add(() => { throw new UnhandledHttpException(); });
                api.Script.Add(() => { throw new UnhandledHttpException(); });
                api.Script.Add(() => { throw new UnhandledHttpException(); });
                api.Script.Add(() => api.NextRequestStatus = AzureRequestStatus.Succeeded);

                var azureManagement = new AzureManagementApiWithRetries(
                    api, 2, TimeSpan.FromMilliseconds(30000));

                var config = MockRepository.GenerateStub<IDeploymentConfiguration>();
                Assert.That(
                    () => azureManagement.Create(FooUri, config),
                    Throws.TypeOf<MaxRetriesExceededException>().With.InnerException.TypeOf<UnhandledHttpException>());
                Assert.That(sleepCount, Is.EqualTo(2));
            }
        }

        [Test]
        public void Create_RetriesTheCreateWhenRequestStatusReturnsFailed()
        {
            using (SpinLoop.ForTests(i => { }))
            {
                var api = new ScriptedAzureManagementLowLevelApiFake();
                var createCallCount = 0;
                var requestId = 12345;
                Action incrCallCount = () =>
                {
                    createCallCount++;
                    api.NextRequestId = (++requestId).ToString();
                };

                api.Script.Add(incrCallCount);
                api.Script.Add(() => api.NextRequestStatus = AzureRequestStatus.InProgress);
                api.Script.Add(() => api.NextRequestStatus = AzureRequestStatus.Failed);
                api.Script.Add(incrCallCount);
                api.Script.Add(() => api.NextRequestStatus = AzureRequestStatus.Succeeded);

                var azureManagement = new AzureManagementApiWithRetries( api, 2, TimeSpan.FromMilliseconds(30000));

                var config = MockRepository.GenerateStub<IDeploymentConfiguration>();
                azureManagement.Create(FooUri, config);
                Assert.That(createCallCount, Is.EqualTo(2));
                var expectedUri = FooUri.ToRequestUri(api.NextRequestId);
                Assert.That(api.LastCheckRequestStatusRequestUri, Is.EqualTo(expectedUri));
            }
        }

        [Test]
        public void Delete_CallsBeginDelete()
        {
            var api = new ScriptedAzureManagementLowLevelApiFake();

            var wasCalled = false;
            api.Script.Add(() => wasCalled = true);
            api.Script.Add(() => api.NextRequestStatus = AzureRequestStatus.Succeeded);

            var azureManagement = new AzureManagementApiWithRetries(
                api, 2, TimeSpan.FromMilliseconds(30000));

            azureManagement.Delete(FooUri);

            Assert.That(wasCalled, "was called");
            Assert.That(api.BeginDeleteDeploymentUri, Is.EqualTo(FooUri), "expected URI");
        }

        [Test]
        public void Delete_RetriesOnExpectedExceptionAndThrowsOnUnexpected()
        {
            using (SpinLoop.ForTests(i => { }))
            {
                var api = new ScriptedAzureManagementLowLevelApiFake();

                api.Script.Add(() => { throw new UnhandledHttpException(); });
                api.Script.Add(() => { throw new ArgumentException(); });

                var azureManagement = new AzureManagementApiWithRetries(
                    api, 2, TimeSpan.FromMilliseconds(30000));

                Assert.That(() => azureManagement.Delete(FooUri), Throws.ArgumentException);
            }
        }

        [Test]
        public void Delete_RetriesTheRightNumberOfTimesThenGivesUpAndThrowsIfException()
        {
            var sleepCount = 0;
            using (SpinLoop.ForTests(i => { sleepCount++; }))
            {
                var api = new ScriptedAzureManagementLowLevelApiFake();

                api.Script.Add(() => { throw new UnhandledHttpException(); });
                api.Script.Add(() => { throw new UnhandledHttpException(); });
                api.Script.Add(() => { throw new UnhandledHttpException(); });

                var azureManagement = new AzureManagementApiWithRetries(
                    api, 2, TimeSpan.FromMilliseconds(30000));

                Assert.That(
                    () => azureManagement.Delete(FooUri),
                    Throws.TypeOf<MaxRetriesExceededException>().With.InnerException.TypeOf<UnhandledHttpException>());
                Assert.That(sleepCount, Is.EqualTo(2));
            }
        }

        [Test]
        public void Delete_RetriesTheDeleteWhenRequestStatusReturnsFailed()
        {
            using (SpinLoop.ForTests(i => { }))
            {
                var api = new ScriptedAzureManagementLowLevelApiFake();
                var createCallCount = 0;
                var requestId = 12345;
                Action handleCall = () =>
                {
                    createCallCount++;
                    api.NextRequestId = (++requestId).ToString();
                };

                api.Script.Add(handleCall);
                api.Script.Add(() => api.NextRequestStatus = AzureRequestStatus.InProgress);
                api.Script.Add(() => api.NextRequestStatus = AzureRequestStatus.Failed);
                api.Script.Add(handleCall);
                api.Script.Add(() => api.NextRequestStatus = AzureRequestStatus.Succeeded);

                var azureManagement = new AzureManagementApiWithRetries(
                    api, 2, TimeSpan.FromMilliseconds(30000));

                azureManagement.Delete(FooUri);
                Assert.That(createCallCount, Is.EqualTo(2));

                var expectedUri = FooUri.ToRequestUri(api.NextRequestId);
                Assert.That(api.LastCheckRequestStatusRequestUri, Is.EqualTo(expectedUri));
            }
        }

        [Test]
        public void WaitForDeploymentStatus_CallsCheckDeploymentStatusWithCorrectUri()
        {
            var api = new ScriptedAzureManagementLowLevelApiFake();

            api.Script.Add(() => api.NextDeploymentCheckOutcome = AzureDeploymentCheckOutcome.Running);

            var azureManagement = new AzureManagementApiWithRetries(api, 2, TimeSpan.FromMilliseconds(30000));

            azureManagement.WaitForDeploymentStatus(FooUri, AzureDeploymentCheckOutcome.Running);

            Assert.That(api.CheckStatusDeploymentUri, Is.EqualTo(FooUri));
        }

        [Test]
        public void WaitForDeploymentStatus_ReturnsOnExpectedStatusAndRetriesOnUnexpectedStatus()
        {
            var retryCount = 0;
            using (SpinLoop.ForTests(i => retryCount++))
            {
                var api = new ScriptedAzureManagementLowLevelApiFake();

                api.Script.Add(() => api.NextDeploymentCheckOutcome = AzureDeploymentCheckOutcome.Running);
                api.Script.Add(() => api.NextDeploymentCheckOutcome = AzureDeploymentCheckOutcome.Suspended);

                var azureManagement = new AzureManagementApiWithRetries(
                    api, 2, TimeSpan.FromMilliseconds(30000));

                azureManagement.WaitForDeploymentStatus(FooUri, AzureDeploymentCheckOutcome.Suspended);

                Assert.That(retryCount, Is.EqualTo(1));
            }
        }

        [Test]
        public void WaitForDeploymentStatus_RethrowsIfUnexpectedExceptionThrown()
        {
            using (SpinLoop.ForTests(i => { }))
            {
                var api = new ScriptedAzureManagementLowLevelApiFake();

                api.Script.Add(() => api.NextDeploymentCheckOutcome = AzureDeploymentCheckOutcome.Running );
                api.Script.Add(() => { throw new ArgumentException(); });

                var azureManagement = new AzureManagementApiWithRetries(
                    api, 2, TimeSpan.FromMilliseconds(30000));

                

                Assert.That(
                    () => azureManagement.WaitForDeploymentStatus(FooUri, AzureDeploymentCheckOutcome.Suspended),
                    Throws.ArgumentException);
            }
        }
    }
}