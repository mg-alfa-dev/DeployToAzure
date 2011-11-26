using System;
using System.Linq.Expressions;
using DeployToAzure.Utility;
using NUnit.Framework;

// ReSharper disable FieldCanBeMadeReadOnly.Local
// ReSharper disable ConvertToAutoPropertyWithPrivateSetter

namespace DeployToAzure.Tests.Utility
{
    [TestFixture]
    public class ExpressionHelperTests
    {
        private class TestTarget
        {
            private int _TargetField;

            public TestTarget(int value)
            {
                _TargetField = value;
            }

            public static Expression<Func<TestTarget, int>> GetFieldExpr()
            {
                return o => o._TargetField;
            }

            public int TargetProperty
            {
                get { return _TargetField; }
            }

            public string TargetPropertyName
            {
                get { return ExpressionHelpers.GetPropertyName(() => TargetProperty); }
            }

            public string TargetPropertyNameAsObject
            {
                get
                {
                    Expression<Func<object>> del = () => TargetProperty;
                    return del.GetPropertyName();
                }
            }

            public string TargetMethodName
            {
                get { return ExpressionHelpers.GetMethodName(() => TargetMethod(0)); }
            }

            public string TargetMethod(int foo)
            {
                return "hello";
            }
        }

        [SetUp]
        public void SetUp()
        {
            _Target = new TestTarget(13);
        }

        private TestTarget _Target;

        [Test]
        public void DoesntThrowWhenAskingForFieldInfo()
        {
            Assert.DoesNotThrow(() => TestTarget.GetFieldExpr().GetFieldInfo());
        }

        [Test]
        public void SettingFieldViaFieldInfoReflectsChange()
        {
            var fieldInfo = TestTarget.GetFieldExpr().GetFieldInfo();
            fieldInfo.SetValue(_Target, 1345);
            Assert.AreEqual(1345, _Target.TargetProperty);
        }

        [Test]
        public void GettingPropertyNameViaCallbackFuncWorksAsExpected()
        {
            var propertyName = ExpressionHelpers.GetPropertyName<TestTarget, int>(t => t.TargetProperty);
            Assert.That(propertyName, Is.EqualTo("TargetProperty"));
        }

        [Test]
        public void GettingPropertynameViaClosureWorksAsExpected()
        {
            Assert.That(_Target.TargetPropertyName, Is.EqualTo("TargetProperty"));
            Assert.That(_Target.TargetPropertyNameAsObject, Is.EqualTo("TargetProperty"));
        }

        [Test]
        public void GettingMethodNameViaClosureWorksAsExpected()
        {
            Assert.That(_Target.TargetMethodName, Is.EqualTo("TargetMethod"));
        }

        [Test]
        public void GettingMethodNameViaCallbackFuncWorksAsExpected()
        {
            var methodName = ExpressionHelpers.GetMethodName<TestTarget, string>(tt => tt.TargetMethod(0));
            Assert.That(methodName, Is.EqualTo("TargetMethod"));
        }

        [Test]
        public void GettingMethodInfoViaCallbackFuncWorksAsExpected()
        {
            var methodInfo = ExpressionHelpers.GetMethodInfo<TestTarget, string>(tt => tt.TargetMethod(0));
            Assert.That(methodInfo, Is.EqualTo(typeof(TestTarget).GetMethod("TargetMethod")));
        }
    }
}