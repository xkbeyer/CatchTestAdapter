using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;


using CatchTestAdapter;

using TestAdapterTest.Mocks;

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
    }
}
