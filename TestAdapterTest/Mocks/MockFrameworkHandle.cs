using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            CatchTestAdapter.ProcessRunner.RunProcess( filePath, arguments, workingDirectory );
            return 0;
        }

        public void RecordAttachments( IList<AttachmentSet> attachmentSets )
        {
            throw new NotImplementedException();
        }

        public void RecordEnd( TestCase testCase, TestOutcome outcome )
        {
            throw new NotImplementedException();
        }

        public void RecordResult( TestResult testResult )
        {
            this.Results.Add( testResult );
        }

        public void RecordStart( TestCase testCase )
        {
            throw new NotImplementedException();
        }

        public void SendMessage( TestMessageLevel testMessageLevel, string message )
        {
            this.Messages.Add( new TestMessage() { Level = testMessageLevel, Content = message } );
        }
    }
}
