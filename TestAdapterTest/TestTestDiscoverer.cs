using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using CatchTestAdapter;
using System.Collections.Generic;

using TestAdapterTest.Mocks;
using System.IO;

namespace TestAdapterTest
{
    [TestClass]
    public class TestTestDiscoverer
    {
        // The path to the reference catch executable.
        const string referenceExe = @"ReferenceCatchProject.exe";
        const string referenceExePath = @"..\..\..\x64\Debug\ReferenceCatchProject.exe";

        // Tests that all the tests in the reference project are found.
        [TestMethod]
        [DeploymentItem( referenceExePath )]
		public void DiscoversAllTests()
		{
            // Initialize a mock sink to keep track of the discovered tests.
            MockTestCaseDiscoverySink testSink = new MockTestCaseDiscoverySink();

            // Discover tests from the reference project.
            TestDiscoverer discoverer = new TestDiscoverer();
            discoverer.DiscoverTests( new List<String>() { referenceExe },
                new MockDiscoveryContext(),
                new MockMessageLogger(),
                testSink );

            // There is a known number of test cases in the reference project.
            Assert.AreEqual( testSink.Tests.Count, 3 );
        }


    }
}
