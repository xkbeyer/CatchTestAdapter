using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;


using Catch.TestAdapter;

using TestAdapterTest.Mocks;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System.IO;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;

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
            Assert.AreEqual( Common.ReferenceTestResultCount, framework.Results.Count );
        }

        [TestMethod]
        public void TestExecutableWithSpaces()
        {
            // Copy the reference exe to a path with a space.
            string spaceDir = Directory.GetCurrentDirectory() + "\\C++ Space - Test";
            string spaceExe = spaceDir + "\\Test Space.exe";
            Directory.CreateDirectory( spaceDir );
            File.Copy( Common.ReferenceExePath, spaceExe );

            // Set up a fake testing context.
            var framework = new MockFrameworkHandle();

            // Execute all tests.
            TestExecutor executor = new TestExecutor();
            executor.RunTests( new List<string>() { spaceExe }, new MockRunContext(), framework );

            // Remove the copy.
            File.Delete( spaceExe );
            Directory.Delete( spaceDir, true );

            // Make sure we got results for all.
            Assert.AreEqual( Common.ReferenceTestResultCount, framework.Results.Count );
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

        [TestMethod]
        public void WarningAndInfoMessage()
        {
            // Set up a fake testing context.
            var framework = new MockFrameworkHandle();

            // Execute all tests.
            TestExecutor executor = new TestExecutor();
            executor.RunTests(Common.ReferenceExeList, new MockRunContext(), framework);

            // Map the tests by name.
            Dictionary<string, TestResult> resultsByName = new Dictionary<string, TestResult>();
            foreach (var result in framework.Results)
            {
                resultsByName[result.TestCase.FullyQualifiedName] = result;
            }

            TestResult testResult = resultsByName["Warn"];
            Assert.AreEqual(TestOutcome.Failed, testResult.Outcome);
            Assert.IsTrue(testResult.ErrorMessage.Contains("#1 - CHECK(false) with expansion: (false)"));
            Assert.AreEqual(1, testResult.Messages.Count);
            Assert.IsTrue(testResult.Messages[0].Text.Contains("WARN: This is a warning message"));

            testResult = resultsByName["Info"];
            Assert.AreEqual(TestOutcome.Failed, testResult.Outcome);
            Assert.IsTrue(testResult.ErrorMessage.Contains("#1 - CHECK(false) with expansion: (false)"));
            Assert.AreEqual(1, testResult.Messages.Count);
            Assert.IsTrue(testResult.Messages[0].Text.Contains("INFO: This is a info message"));

        }

        [TestMethod]
        public void BrokenXmlWithSingleTest()
        {
            var framework = new MockFrameworkHandle();
            var runcontext = new MockRunContext();
            var executor = new MockTestExecutor();
            IList<string> xml_output = new List<string>() { @"<?xml version=""1.0"" encoding=""UTF-8""?>",
@"<Catch name=""CatchUnitTest.exe"">",
@"  <Group name=""CatchUnitTest.exe"">",
@"    <TestCase name=""C++ assert"" filename=""ReferenceCatchProject\testrunnertest.cpp"" line=""45"">"
};
            IList<TestCase> tests = new List<TestCase>();
            tests.Add(new TestCase("C++ assert", new Uri(TestExecutor.ExecutorUriString), "ReferenceCatchProject") { CodeFilePath = "ReferenceCatchProject\testrunnertest.cpp", LineNumber = 45 });
            executor.MockComposeResults(xml_output, tests, framework);
            Assert.AreEqual(1, framework.Results.Count);
            Assert.AreEqual(TestOutcome.None, framework.Results[0].Outcome);
        }

        [TestMethod]
        public void BrokenXml()
        {
            var framework = new MockFrameworkHandle();
            var runcontext = new MockRunContext();
            var executor = new MockTestExecutor();
            IList<string> xml_output = new List<string>() { @"<?xml version=""1.0"" encoding=""UTF-8""?>",
@"<Catch name=""CatchUnitTest.exe"">",
@"  <Group name=""CatchUnitTest.exe"">",
@"    <TestCase name=""Simple test case"" tags=""[#testrunnertest][tag]"" filename=""ReferenceCatchProject\testrunnertest.cpp"" line=""15"">",
@"      <Expression success=""false"" type=""CHECK"" filename=""ReferenceCatchProject\testrunnertest.cpp"" line=""10"">",
@"        <Original>",
@"          3 == 4",
@"        </Original>",
@"        <Expanded>",
@"          3 == 4",
@"        </Expanded>",
@"      </Expression>",
@"      <Expression success=""false"" type=""REQUIRE"" filename=""ReferenceCatchProject\testrunnertest.cpp"" line=""11"">",
@"        <Original> ",
@"          9 == 0   ",
@"        </Original>",
@"        <Expanded> ",
@"          9 == 0   ",
@"        </Expanded>",
@"      </Expression>",
@"      <OverallResult success=""false"" durationInSeconds=""0.003571""/>",
@"    </TestCase>",
@"    <TestCase name=""Another test case"" tags=""[#testrunnertest][tag]"" filename=""ReferenceCatchProject\testrunnertest.cpp"" line=""23"">",
@"      <Expression success=""false"" type=""CHECK"" filename=""ReferenceCatchProject\testrunnertest.cpp"" line=""18"">",
@"        <Original> ",
@"          a == b   ",
@"        </Original>",
@"        <Expanded> ",
@"          5 == 6   ",
@"        </Expanded>",
@"      </Expression>",
@"      <Section name=""A Section test"" filename=""ReferenceCatchProject\testrunnertest.cpp"" line=""20"">",
@"        <OverallResults successes=""1"" failures=""0"" expectedFailures=""0"" durationInSeconds=""2.3e-05""/>",
@"      </Section>",
@"      <OverallResult success=""false"" durationInSeconds=""0.002557""/>",
@"    </TestCase>",
@"    <TestCase name=""Third test case"" tags=""[#testrunnertest][tag]"" filename=""ReferenceCatchProject\testrunnertest.cpp"" line=""37"">",
@"      <Failure filename=""ReferenceCatchProject\testrunnertest.cpp"" line=""24"">",
@"        Fail check",
@"      </Failure>",
@"      <OverallResult success=""false"" durationInSeconds=""0.001122""/>",
@"    </TestCase>",
@"    <TestCase name=""C++ assert"" tags=""[#testrunnertest]"" filename=""ReferenceCatchProject\testrunnertest.cpp"" line=""45"">",
};
            IList<TestCase> tests = new List<TestCase>();
            tests.Add(new TestCase("Simple test case", new Uri(TestExecutor.ExecutorUriString), "ReferenceCatchProject") { CodeFilePath = "ReferenceCatchProject\testrunnertest.cpp", LineNumber = 15 });
            tests.Add(new TestCase("Another test case", new Uri(TestExecutor.ExecutorUriString), "ReferenceCatchProject") { CodeFilePath = "ReferenceCatchProject\testrunnertest.cpp", LineNumber = 23 });
            tests.Add(new TestCase("Third test case", new Uri(TestExecutor.ExecutorUriString), "ReferenceCatchProject") { CodeFilePath = "ReferenceCatchProject\testrunnertest.cpp", LineNumber = 37 });
            tests.Add(new TestCase("C++ assert", new Uri(TestExecutor.ExecutorUriString), "ReferenceCatchProject") { CodeFilePath = "ReferenceCatchProject\testrunnertest.cpp", LineNumber = 45 });
            tests.Add(new TestCase("Last test case", new Uri(TestExecutor.ExecutorUriString), "ReferenceCatchProject") { CodeFilePath = "ReferenceCatchProject\testrunnertest.cpp", LineNumber = 54 });
            executor.MockComposeResults(xml_output, tests, framework);
            Assert.AreEqual(6, framework.Results.Count);
            for(int i = 0; i < 3; ++i)
            {
                Assert.AreNotEqual(TestOutcome.None, framework.Results[i].Outcome);
            }
            Assert.AreEqual(TestOutcome.None, framework.Results[4].Outcome);
            Assert.AreEqual(TestOutcome.None, framework.Results[5].Outcome);
        }

        [TestMethod]
        public void XmlReadSuccessOnlyTestCase()
        {
            var framework = new MockFrameworkHandle();
            var runcontext = new MockRunContext();
            var executor = new MockTestExecutor();
            IList<string> xml_output = new List<string>() { @"<?xml version=""1.0"" encoding=""UTF-8""?>",
@"<Catch name=""CatchUnitTest.exe"">",
@"  <Group name=""CatchUnitTest.exe"">",
@"    <TestCase name=""First fixture"" tags=""[fixture]"" filename=""ReferenceCatchProject\fixture_test.cpp"" line=""13"">",
@"      <OverallResult success=""true"" durationInSeconds=""0.00085""/>",
@"    </TestCase>",
@"  </Group>",
@"  <OverallResults successes=""3"" failures=""3"" expectedFailures=""0""/>",
@"</Catch>"
};
            IList<TestCase> tests = new List<TestCase>();
            tests.Add(new TestCase("First fixture", new Uri(TestExecutor.ExecutorUriString), "ReferenceCatchProject") { CodeFilePath = "ReferenceCatchProject\fixture_test.cpp", LineNumber = 13 });
            executor.MockComposeResults(xml_output, tests, framework);
            Assert.AreEqual(1, framework.Results.Count);
            Assert.AreEqual(TestOutcome.Passed, framework.Results[0].Outcome);
        }

        [TestMethod]
        public void XmlReadTest()
        {
            var framework = new MockFrameworkHandle();
            var runcontext = new MockRunContext();
            var executor = new MockTestExecutor();
            IList<string> xml_output = new List<string>() { @"<?xml version=""1.0"" encoding=""UTF-8""?>",
@"<Catch name=""CatchUnitTest.exe"">",
@"  <Group name=""CatchUnitTest.exe"">",
@"    <TestCase name=""Second fixture"" tags=""[fixture]"" filename=""ReferenceCatchProject\fixture_test.cpp"" line=""21"">",
@"      <Expression success=""false"" type=""REQUIRE"" filename=""ReferenceCatchProject\fixture_test.cpp"" line=""25"">",
@"        <Original>",
@"          expected == actual",
@"        </Original>",
@"        <Expanded>",
@"          true == false",
@"        </Expanded>",
@"      </Expression>",
@"      <OverallResult success=""false"" durationInSeconds=""0.001865""/>",
@"    </TestCase>",
@"    <TestCase name=""Fail message test"" tags=""[fixture]"" filename=""ReferenceCatchProject\fixture_test.cpp"" line=""31"">",
@"      <Failure filename=""ReferenceCatchProject\fixture_test.cpp"" line=""33"">",
@"        Not implemented!",
@"      </Failure>",
@"      <OverallResult success=""false"" durationInSeconds=""0.001188""/>",
@"    </TestCase>",
@"    <TestCase name=""Fail message test in between"" tags=""[fixture]"" filename=""ReferenceCatchProject\fixture_test.cpp"" line=""36"">",
@"      <Failure filename=""ReferenceCatchProject\fixture_test.cpp"" line=""40"">",
@"        Not implemented!",
@"      </Failure>",
@"      <OverallResult success=""false"" durationInSeconds=""0.001295""/>",
@"    </TestCase>",
@"    <OverallResults successes=""3"" failures=""3"" expectedFailures=""0""/>",
@"  </Group>",
@"  <OverallResults successes=""3"" failures=""3"" expectedFailures=""0""/>",
@"</Catch>"
};
            IList<TestCase> tests = new List<TestCase>();
            tests.Add(new TestCase("Second fixture", new Uri(TestExecutor.ExecutorUriString), "ReferenceCatchProject") { CodeFilePath = "ReferenceCatchProject\fixture_test.cpp", LineNumber = 21 });
            tests.Add(new TestCase("Fail message test", new Uri(TestExecutor.ExecutorUriString), "ReferenceCatchProject") { CodeFilePath = "ReferenceCatchProject\fixture_test.cpp", LineNumber = 31 });
            tests.Add(new TestCase("Fail message test in between", new Uri(TestExecutor.ExecutorUriString), "ReferenceCatchProject") { CodeFilePath = "ReferenceCatchProject\fixture_test.cpp", LineNumber = 36 });
            executor.MockComposeResults(xml_output, tests, framework);
            Assert.AreEqual(3, framework.Results.Count);
            for (int i = 0; i < framework.Results.Count; ++i)
            {
                Assert.AreEqual(TestOutcome.Failed, framework.Results[i].Outcome);
            }
        }

        [TestMethod]
        public void TestSingleTestCaseAndOneSection()
        {
            // Set up a fake testing context.
            var framework = new MockFrameworkHandle();

            // Execute all tests.
            TestExecutor executor = new TestExecutor();
            List<TestCase> testCases = new List<TestCase>();
            var test = new TestCase("Has failure", TestExecutor.ExecutorUri, "ReferenceCatchProject.exe")
            {
                CodeFilePath = @"ReferenceCatchProject\Tests.cpp",
                DisplayName = "Has failure Second fails",
                LineNumber = 31,
                Source = Common.ReferenceExePath
            };
            test.Id = EqtHash.GuidFromString(test.FullyQualifiedName+test.ExecutorUri+test.Source+"Has failure/Second fails");
            test.Traits.Add(new Trait("tag", null));
            test.SetPropertyValue(TestExecutor.Section, "Has failure/Second fails");
            testCases.Add(test);
            executor.RunTests(testCases, new MockRunContext(), framework);

            // Make sure we got results for all.
            Assert.AreEqual(1, framework.Results.Count);
            Assert.AreEqual(TestOutcome.Failed, framework.Results[0].Outcome);
        }

        [TestMethod]
        public void TestTestCaseAndSection()
        {
            // Set up a fake testing context.
            var framework = new MockFrameworkHandle();

            // Execute all tests.
            TestExecutor executor = new TestExecutor();
            List<TestCase> testCases = new List<TestCase>();
            {
                var test = new TestCase("Has failure", TestExecutor.ExecutorUri, "ReferenceCatchProject.exe")
                {
                    CodeFilePath = @"ReferenceCatchProject\Tests.cpp",
                    DisplayName = "Has failure First works",
                    LineNumber = 26,
                    Source = Common.ReferenceExePath
                };
                test.Id = EqtHash.GuidFromString(test.FullyQualifiedName + test.ExecutorUri + test.Source + "Has failure/First works");
                test.Traits.Add(new Trait("tag", null));
                test.SetPropertyValue(TestExecutor.Section, "Has failure/First works");
                testCases.Add(test);
            }
            {
                var test = new TestCase("Has failure", TestExecutor.ExecutorUri, "ReferenceCatchProject.exe")
                {
                    CodeFilePath = @"ReferenceCatchProject\Tests.cpp",
                    DisplayName = "Has failure Second fails",
                    LineNumber = 31,
                    Source = Common.ReferenceExePath
                };
                test.Id = EqtHash.GuidFromString(test.FullyQualifiedName + test.ExecutorUri + test.Source + "Has failure/Second fails");
                test.Traits.Add(new Trait("tag", null));
                test.SetPropertyValue(TestExecutor.Section, "Has failure/Second fails");
                testCases.Add(test);
            }

            executor.RunTests(testCases, new MockRunContext(), framework);

            // Make sure we got results for all.
            Assert.AreEqual(2, framework.Results.Count);
            Assert.AreEqual(TestOutcome.Passed, framework.Results[0].Outcome);
            Assert.AreEqual(TestOutcome.Failed, framework.Results[1].Outcome);
        }

        [TestMethod]
        public void TestTestCaseAndTestCaseSection()
        {
            // Set up a fake testing context.
            var framework = new MockFrameworkHandle();

            // Execute all tests.
            TestExecutor executor = new TestExecutor();
            List<TestCase> testCases = new List<TestCase>();
            {
                var test = new TestCase("Info", TestExecutor.ExecutorUri, "ReferenceCatchProject.exe")
                {
                    CodeFilePath = @"ReferenceCatchProject\Tests.cpp",
                    DisplayName = "Info",
                    LineNumber = 51,
                    Source = Common.ReferenceExePath
                };
                test.Id = EqtHash.GuidFromString(test.FullyQualifiedName + test.ExecutorUri + test.Source + "Info");
                test.Traits.Add(new Trait("Logging", null));
                testCases.Add(test);
            }
            {
                var test = new TestCase("Has failure", TestExecutor.ExecutorUri, "ReferenceCatchProject.exe")
                {
                    CodeFilePath = @"ReferenceCatchProject\Tests.cpp",
                    DisplayName = "Has failure Second fails",
                    LineNumber = 31,
                    Source = Common.ReferenceExePath
                };
                test.Id = EqtHash.GuidFromString(test.FullyQualifiedName + test.ExecutorUri + test.Source + "Has failure/Second fails");
                test.Traits.Add(new Trait("tag", null));
                test.SetPropertyValue(TestExecutor.Section, "Has failure/Second fails");
                testCases.Add(test);
            }

            executor.RunTests(testCases, new MockRunContext(), framework);

            // Make sure we got results for all.
            Assert.AreEqual(2, framework.Results.Count);
            Assert.AreEqual(TestOutcome.Failed, framework.Results[0].Outcome);
            Assert.AreEqual(TestOutcome.Failed, framework.Results[1].Outcome);
        }

    }
}
