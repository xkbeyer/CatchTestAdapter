using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestAdapterTest
{
    /// <summary>
    /// Data common to tests.
    /// </summary>
    static class Common
    {

        // The name of the reference catch executable.
        public const string ReferenceExe = @"ReferenceCatchProject.exe";

        // The path to the reference catch executable.
        public const string ReferenceExePath = @"..\..\..\x64\Debug\" + ReferenceExe;

        // A list with the reference exe.
        public static List<string> ReferenceExeList { get; } = new List<String>() { ReferenceExe };

        // The number of tests in the reference project.
        public const int ReferenceTestCount = 4;
    }
}
