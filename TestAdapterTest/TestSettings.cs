using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestAdapter.Settings;

namespace TestAdapterTest
{
    [TestClass]
    public class TestSettings
    {
        [TestMethod]
        public void EmptyExeFilterAcceptsAll()
        {
            CatchAdapterSettings settings = new CatchAdapterSettings();

            Assert.IsTrue( settings.IncludeTestExe( "blaa" ) );
            Assert.IsTrue( settings.IncludeTestExe( "Test.exe" ) );
        }

        [TestMethod]
        public void IncludeExeFilterAcceptsOnlyMatching()
        {
            CatchAdapterSettings settings = new CatchAdapterSettings();
            settings.TestExeInclude.Add( "blaa" );
            settings.TestExeInclude.Add( @".*\.exe" );

            Assert.IsTrue( settings.IncludeTestExe( "blaa" ) );
            Assert.IsTrue( settings.IncludeTestExe( "Test.exe" ) );
            Assert.IsFalse( settings.IncludeTestExe( "Hippopotamus" ) );
        }

        [TestMethod]
        public void ExcludeExeFilterRejectsOnlyMatching()
        {
            CatchAdapterSettings settings = new CatchAdapterSettings();
            settings.TestExeExclude.Add( "blaa" );
            settings.TestExeExclude.Add( @".*\.exe" );

            Assert.IsFalse( settings.IncludeTestExe( "blaa" ) );
            Assert.IsFalse( settings.IncludeTestExe( "Test.exe" ) );
            Assert.IsTrue( settings.IncludeTestExe( "Hippopotamus" ) );
        }

        [TestMethod]
        public void IncludeExcludeTogether()
        {
            CatchAdapterSettings settings = new CatchAdapterSettings();
            settings.TestExeInclude.Add( @".*\.exe" );
            settings.TestExeExclude.Add( @"bl(aa|uu)" );
            
            Assert.IsTrue( settings.IncludeTestExe( "Test.exe" ) );
            Assert.IsFalse( settings.IncludeTestExe( "Hippopotamus" ) );
            Assert.IsFalse( settings.IncludeTestExe( "blaa.exe" ) );
            Assert.IsFalse( settings.IncludeTestExe( "bluu.exe" ) );
            Assert.IsTrue( settings.IncludeTestExe( "blii.exe" ) );
        }
    }
}
