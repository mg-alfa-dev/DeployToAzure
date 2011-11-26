using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace DeployToAzure.Tests.TestUtilities
{
    public abstract class ScriptedTestDouble
    {
        public readonly List<Action> Script = new List<Action>();
        private int _state;

        protected void RunScript()
        {
            if (_state >= Script.Count)
                throw new RunningPastEndOfScriptException("You've run past the end of the script on your test double!");
            Script[_state++]();
        }
    }

    [Serializable]
    public class RunningPastEndOfScriptException : Exception
    {
        public RunningPastEndOfScriptException()
        {
        }

        public RunningPastEndOfScriptException(string message) : base(message)
        {
        }

        public RunningPastEndOfScriptException(string message, Exception inner) : base(message, inner)
        {
        }

        protected RunningPastEndOfScriptException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}