using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using System;
using System.Collections.Generic;

namespace TestAdapterTest.Mocks
{
    class MockFrameworkHandle : IFrameworkHandle
    {
        // Collects the reported test results.
        public IList<TestResult> Results { get; } = new List<TestResult>();

        // A record of a message sent during the test run.
        public struct TestMessage {
            public TestMessageLevel Level;
            public string Content;
        }

        /// <summary>
        /// The messages sent from the tests.
        /// </summary>
        public IList<TestMessage> Messages { get; } = new List<TestMessage>();

        public bool EnableShutdownAfterTestRun { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public int LaunchProcessWithDebuggerAttached( string filePath, string workingDirectory, string arguments, IDictionary<string, string> environmentVariables )
        {
            Catch.TestAdapter.ProcessRunner.RunProcess(null, filePath, arguments, workingDirectory, false);
            return 0;
        }

        public void RecordAttachments( IList<AttachmentSet> attachmentSets )
        {
            throw new NotImplementedException();
        }

        public void RecordEnd( TestCase testCase, TestOutcome outcome )
        {
        }

        public void RecordResult( TestResult testResult )
        {
            this.Results.Add( testResult );
        }

        public void RecordStart( TestCase testCase )
        {
        }

        public void SendMessage( TestMessageLevel testMessageLevel, string message )
        {
            this.Messages.Add( new TestMessage() { Level = testMessageLevel, Content = message } );
        }
    }
}
