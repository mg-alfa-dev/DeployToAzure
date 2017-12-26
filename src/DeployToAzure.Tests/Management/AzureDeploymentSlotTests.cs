using System;
using DeployToAzure.Management;
using NUnit.Framework;
using Rhino.Mocks;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable InconsistentNaming
namespace DeployToAzure.Tests.Management
{
    [TestFixture]
    public class AzureDeploymentSlotTests
    {
        public readonly DeploymentSlotUri TestDeploymentUri = new DeploymentSlotUri(subscriptionId: "subid", serviceName: "svcname", slot: "production");

        [Test]
        public void DeleteDeployment_CallsAzureApiCorrectlyWhenDeploymentExists()
        {
            var sim = new SimulatedAzureManagementApi
            {
                ExpectedDeploymentUri = TestDeploymentUri,
                CurrentState = AzureDeploymentCheckOutcome.Running,
            };

            IAzureDeploymentSlot deploymentSlot = new AzureDeploymentSlot(
                sim,
                TestDeploymentUri);

            deploymentSlot.DeleteDeployment();
            Assert.That(sim.CurrentState, Is.EqualTo(AzureDeploymentCheckOutcome.NotFound), "deployment was deleted");
            Assert.That(sim.WaitCompleted, Is.True, "waited for delete to finish");
        }

        [Test]
        public void DeleteDeployment_CallsAzureApiCorrectlyWhenDeploymentDoesntExist()
        {
            var sim = new SimulatedAzureManagementApi
            {
                ExpectedDeploymentUri = TestDeploymentUri,
                CurrentState = AzureDeploymentCheckOutcome.NotFound,
            };

            IAzureDeploymentSlot deploymentSlot = new AzureDeploymentSlot(
                sim,
                TestDeploymentUri);

            deploymentSlot.DeleteDeployment();
            Assert.That(sim.CurrentState, Is.EqualTo(AzureDeploymentCheckOutcome.NotFound), "deployment was deleted");
            Assert.That(sim.WaitCompleted, Is.True, "waited for delete to finish");
        }

        [Test]
        public void CreateDeployment_CallsAzureApiCorrectlyWhenDeploymentDoesntExist()
        {
            var sim = new SimulatedAzureManagementApi
            {
                ExpectedDeploymentUri = TestDeploymentUri,
                CurrentState = AzureDeploymentCheckOutcome.NotFound,
            };

            IAzureDeploymentSlot deploymentSlot = new AzureDeploymentSlot(
                sim,
                TestDeploymentUri);

            var configuration = MockRepository.GenerateStub<IDeploymentConfiguration>();
            deploymentSlot.CreateOrReplaceDeployment(configuration);
            
            Assert.That(sim.LastDeploymentConfiguration, Is.SameAs(configuration));

            Assert.That(sim.CurrentState, Is.EqualTo(AzureDeploymentCheckOutcome.Running), "deployment was started");
            Assert.That(sim.WaitCompleted, Is.True, "waited for delete to finish");
        }

        [Test] 
        public void CreateDeployment_CallsAzureApiCorrectlyWhenDeploymentAlreadyExists()
        {
            var sim = new SimulatedAzureManagementApi
            {
                ExpectedDeploymentUri = TestDeploymentUri,
                CurrentState = AzureDeploymentCheckOutcome.Running,
            };

            IAzureDeploymentSlot deploymentSlot = new AzureDeploymentSlot(
                sim,
                TestDeploymentUri);

            var configuration = MockRepository.GenerateStub<IDeploymentConfiguration>();
            deploymentSlot.CreateOrReplaceDeployment(configuration);

            Assert.That(sim.LastDeploymentConfiguration, Is.SameAs(configuration));

            Assert.That(sim.CurrentState, Is.EqualTo(AzureDeploymentCheckOutcome.Running), "deployment was started");
            Assert.That(sim.WaitCompleted, Is.True, "waited for delete to finish");
            Assert.That(sim.DeletedAtLeastOnce, Is.True, "was the existing service deleted?");
        }

        private class SimulatedAzureManagementApi : IAzureManagementApiWithRetries
        {
            public DeploymentSlotUri ExpectedDeploymentUri;
            public IDeploymentConfiguration LastDeploymentConfiguration;
            public AzureDeploymentCheckOutcome CurrentState;
            public bool WaitCompleted = true; // always start with true.
            public bool DeletedAtLeastOnce;

            public bool DoesDeploymentExist(DeploymentSlotUri deploymentUri)
            {
                Assert.That(deploymentUri, Is.EqualTo(ExpectedDeploymentUri));
                return CurrentState == AzureDeploymentCheckOutcome.Running
                       || CurrentState == AzureDeploymentCheckOutcome.Suspended
                       || CurrentState == AzureDeploymentCheckOutcome.RunningTransitioning;
            }

            public void WaitForDeploymentStatus(DeploymentSlotUri deploymentUri, AzureDeploymentCheckOutcome status)
            {
                Assert.That(WaitCompleted, Is.False);

                Assert.That(deploymentUri, Is.EqualTo(ExpectedDeploymentUri));
                Assert.That(status, Is.EqualTo(CurrentState));
                
                WaitCompleted = true;
            }

            public void Create(DeploymentSlotUri deploymentUri, IDeploymentConfiguration configuration)
            {
                Assert.That(WaitCompleted, Is.True);

                LastDeploymentConfiguration = configuration;
                
                Assert.That(deploymentUri, Is.EqualTo(ExpectedDeploymentUri));
                Assert.That(CurrentState, Is.EqualTo(AzureDeploymentCheckOutcome.NotFound));

                WaitCompleted = false;
                CurrentState = AzureDeploymentCheckOutcome.Running;
            }

            public void Start(DeploymentSlotUri deploymentUri)
            {
                if (CurrentState != AzureDeploymentCheckOutcome.Running)
                {
                    Assert.That(WaitCompleted, Is.True);

                    Assert.That(deploymentUri, Is.EqualTo(ExpectedDeploymentUri));
                    Assert.That(CurrentState, Is.EqualTo(AzureDeploymentCheckOutcome.Suspended));
                }

                WaitCompleted = false;
                CurrentState = AzureDeploymentCheckOutcome.Running;
            }

            public void Suspend(DeploymentSlotUri deploymentUri)
            {
                if (CurrentState != AzureDeploymentCheckOutcome.Suspended)
                {
                    Assert.That(WaitCompleted, Is.True);

                    Assert.That(deploymentUri, Is.EqualTo(ExpectedDeploymentUri));
                    Assert.That(CurrentState, Is.EqualTo(AzureDeploymentCheckOutcome.Running));
                }

                WaitCompleted = false;
                CurrentState = AzureDeploymentCheckOutcome.Suspended;
            }

            public void Delete(DeploymentSlotUri deploymentUri)
            {
                Assert.That(WaitCompleted, Is.True);

                Assert.That(deploymentUri, Is.EqualTo(ExpectedDeploymentUri));
                Assert.That(CurrentState, Is.EqualTo(AzureDeploymentCheckOutcome.Suspended));

                WaitCompleted = false;
                CurrentState = AzureDeploymentCheckOutcome.NotFound;
                DeletedAtLeastOnce = true;
            }

            public void Upgrade(DeploymentSlotUri deploymentSlotUri, DeploymentConfiguration configuration)
            {
                throw new NotImplementedException();
            }
        }
    }
}
