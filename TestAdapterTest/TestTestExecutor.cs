using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;


using CatchTestAdapter;

using TestAdapterTest.Mocks;

namespace TestAdapterTest
{
    [TestClass]
    public class TestTestExecutor
    {
        [TestMethod]
        public void TestExecutesAllTests()
        {

            TestExecutor executor = new TestExecutor();
            
        }
    }
}
