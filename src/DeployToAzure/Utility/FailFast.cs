using System;
using System.Runtime.Serialization;
using JetBrains.Annotations;

namespace DeployToAzure.Utility
{
    // ReSharper disable UnusedParameter.Global
    public static class FailFast
    {
        [Serializable]
        private class AssertionException : Exception
        {
            public AssertionException(string message) : base(message)
            {
            }

            protected AssertionException(
                SerializationInfo info,
                StreamingContext context) : base(info, context)
            {
            }
        }

        [AssertionMethod]
        public static void IfNull([AssertionCondition(AssertionConditionType.IS_NOT_NULL)] object argument, string name)
        {
            if(argument == null) 
                throw new AssertionException(String.Format("Value for {0} should not be null.", name));
        }

        [AssertionMethod]
        public static void Unless([AssertionCondition(AssertionConditionType.IS_TRUE)] bool assertion, string message)
        {
            if (!assertion) 
                throw new AssertionException(message);
        }

        [TerminatesProgram]
        public static void WithMessage(string message)
        {
            throw new AssertionException(message);
        }
    }
}
