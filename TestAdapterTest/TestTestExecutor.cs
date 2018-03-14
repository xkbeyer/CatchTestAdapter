using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;


using CatchTestAdapter;

using TestAdapterTest.Mocks;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace TestAdapterTest
{
    [TestClass]
    [DeploymentItem( Common.ReferenceExePath )]
    public class TestTestExecutor
    {
        [TestMethod]
        public void TestExecutesAllTests()
        {
            // Set up a fake testing context.
            var framework = new MockFrameworkHandle();

            // Execute all tests.
            TestExecutor executor = new TestExecutor();
            executor.RunTests( Common.ReferenceExeList, new MockRunContext(), framework );

            // Make sure we got results for all.
            Assert.AreEqual( Common.ReferenceTestCount, framework.Results.Count );
        }

        [TestMethod]
        public void FailureIsFailure()
        {
            // Set up a fake testing context.
            var framework = new MockFrameworkHandle();

            // Execute all tests.
            TestExecutor executor = new TestExecutor();
            executor.RunTests( Common.ReferenceExeList, new MockRunContext(), framework );

            // Map the tests by name.
            Dictionary<string, TestResult> resultsByName = new Dictionary<string, TestResult>();
            foreach( var result in framework.Results )
            {
                resultsByName[ result.TestCase.FullyQualifiedName ] = result;
            }

            // Check that the failure failed and success succeeded.
            TestResult failure = resultsByName[ "Has failure" ];
            Assert.AreEqual( TestOutcome.Failed, failure.Outcome );
            Assert.IsTrue( failure.ErrorStackTrace.Contains( "33" ) ); // Failure line number.

            TestResult success = resultsByName[ "With tags" ];
            Assert.AreEqual( TestOutcome.Passed, success.Outcome );
        }

        [TestMethod]
        public void ForcedFailureHasMessage()
        {
            // Set up a fake testing context.
            var framework = new MockFrameworkHandle();

            // Execute all tests.
            TestExecutor executor = new TestExecutor();
            executor.RunTests( Common.ReferenceExeList, new MockRunContext(), framework );

            // Map the tests by name.
            Dictionary<string, TestResult> resultsByName = new Dictionary<string, TestResult>();
            foreach ( var result in framework.Results )
            {
                resultsByName[ result.TestCase.FullyQualifiedName ] = result;
            }

            // Check that the test with a forced failure has the user-given message in the output.
            TestResult forcedFailure = resultsByName[ "Has forced failure" ];
            Assert.AreEqual( TestOutcome.Failed, forcedFailure.Outcome );
            Assert.IsTrue( forcedFailure.ErrorMessage.Contains( "This message should be in the failure report." ) );
        }
    }
}
