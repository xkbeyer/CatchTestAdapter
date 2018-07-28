using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Catch.TestAdapter;
using System.Collections.Generic;

using TestAdapterTest.Mocks;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using TestAdapter.Settings;

namespace TestAdapterTest
{
    [TestClass]
    public class TestTestDiscoverer
    {

        // Tests that all the tests in the reference project are found.
        [TestMethod]
        [DeploymentItem( Common.ReferenceExePath )]
        public void DiscoversAllTests()
        {
            // Initialize a mock sink to keep track of the discovered tests.
            MockTestCaseDiscoverySink testSink = new MockTestCaseDiscoverySink();

            // Discover tests from the reference project.
            TestDiscoverer discoverer = new TestDiscoverer();
            discoverer.DiscoverTests( Common.ReferenceExeList,
                new MockDiscoveryContext(),
                new MockMessageLogger(),
                testSink );

            // There is a known number of test cases in the reference project.
            Assert.AreEqual( testSink.Tests.Count, Common.ReferenceTestCount );
        }

        // Tests that the test case lines are correct.
        [TestMethod]
        [DeploymentItem( Common.ReferenceExePath )]
        public void TestCaseLinesCorrect()
        {
            // Initialize a mock sink to keep track of the discovered tests.
            MockTestCaseDiscoverySink testSink = new MockTestCaseDiscoverySink();

            // Discover tests from the reference project.
            TestDiscoverer discoverer = new TestDiscoverer();
            discoverer.DiscoverTests( Common.ReferenceExeList,
                new MockDiscoveryContext(),
                new MockMessageLogger(),
                testSink );

            // The reference project test cases are on these lines.
            var linesOfCases = new Dictionary<string, int>();
            linesOfCases.Add( "No tags", 6 );
            linesOfCases.Add( "With tags", 16 );
            linesOfCases.Add( "Has failure", 24 );

            foreach( var test in testSink.Tests )
            {
                // Process only tests we have hard coded here.
                if( linesOfCases.ContainsKey( test.FullyQualifiedName ) )
                {
                    // Check that the line number is correct.
                    Assert.AreEqual( linesOfCases[ test.FullyQualifiedName ], test.LineNumber, test.FullyQualifiedName );

                    // Remove the case so we can check all were handled.
                    linesOfCases.Remove( test.FullyQualifiedName );
                }
            }

            // Make sure all the cases we wanted got checked.
            Assert.AreEqual( linesOfCases.Count, 0, String.Format( "Unhandled cases: {0}", linesOfCases.ToString() ) );
        }

        // Tests that tags are translated to traits.
        [TestMethod]
        [DeploymentItem( Common.ReferenceExePath )]
        public void TestTagsToTraits()
        {
            // Initialize a mock sink to keep track of the discovered tests.
            MockTestCaseDiscoverySink testSink = new MockTestCaseDiscoverySink();

            // Discover tests from the reference project.
            TestDiscoverer discoverer = new TestDiscoverer();
            discoverer.DiscoverTests( Common.ReferenceExeList,
                new MockDiscoveryContext(),
                new MockMessageLogger(),
                testSink );

            // Get the test with tags.
            TestCase tagsTest = testSink.Tests.Where( test => test.DisplayName == "With tags" ).First();

            // The tags should be present in the test as traits.
            string traits = tagsTest.Traits
                .Select( trait => trait.Name )
                .Aggregate("", ( acc, add ) => acc + "," + add );
            Assert.AreEqual( 2, tagsTest.Traits.Count() );
            Assert.IsTrue( tagsTest.Traits.Any( trait => trait.Name == "tag" ), traits );
            Assert.IsTrue( tagsTest.Traits.Any( trait => trait.Name == "neat" ), traits );
        }

        [TestMethod]
        [DeploymentItem( Common.ReferenceExePath )]
        // Tests that filters in runsettings are obeyed.
        public void FiltersTestExecutables()
        {
            // Initialize a mock sink to keep track of the discovered tests.
            MockTestCaseDiscoverySink testSink = new MockTestCaseDiscoverySink();

            // Configure a mock context.
            var context = new MockDiscoveryContext();
            var provider = new CatchSettingsProvider();
            provider.Settings = new CatchAdapterSettings();
            provider.Settings.TestExeInclude.Add( @"ReferenceCatchProject\.exe" );
            context.MockSettings.Provider = provider;

            // Discover tests from the reference project and from anon-existent exe.
            // The non-existent exe should get filtered out and cause no trouble.
            TestDiscoverer discoverer = new TestDiscoverer();
            List<string> exeList = new List<string>();
            exeList.AddRange( Common.ReferenceExeList );
            exeList.Add( "nonsense.exe" );
            discoverer.DiscoverTests( Common.ReferenceExeList,
                context,
                new MockMessageLogger(),
                testSink );

            // There is a known number of test cases in the reference project.
            Assert.AreEqual( testSink.Tests.Count, Common.ReferenceTestCount );

            // Clear the sink.
            testSink = new MockTestCaseDiscoverySink();

            // Filter all exes.
            provider.Settings.TestExeInclude.Clear();
            provider.Settings.TestExeInclude.Add( "laksjdlkjalsdjasljd" );

            // Discover again.
            discoverer.DiscoverTests( Common.ReferenceExeList,
                context,
                new MockMessageLogger(),
                testSink );

            // There should be no tests, as nothing matches the filter.
            Assert.AreEqual( testSink.Tests.Count, 0 );
        }

        // Tests that a non Catch exe returns no test cases.
        [TestMethod]
        public void DiscoversNoTests()
        {
            // Initialize a mock sink to keep track of the discovered tests.
            MockTestCaseDiscoverySink testSink = new MockTestCaseDiscoverySink();

            TestDiscoverer discoverer = new TestDiscoverer();
            var cd = System.IO.Directory.GetCurrentDirectory();
            // Unfortunately it doesn't get copied with the DeployItemAttribute, no idea why.
            System.IO.File.WriteAllText(@"nonecatchexe.cmd", @"@echo Non Catch Output line");
            // Returns an unexpected first line.
            discoverer.DiscoverTests(new List<String>(){ @"nonecatchexe.cmd" }, 
                new MockDiscoveryContext(),
                new MockMessageLogger(),
                testSink);

            // Zero test cases should be registered.
            Assert.AreEqual(0, testSink.Tests.Count);
        }
    }
}
