using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestAdapterTest.Mocks
{
    class MockRunSettings : IRunSettings
    {
        public string SettingsXml { get; } = @"<?xml version=""1.0"" encoding=""utf-16""?>
<RunSettings>
  <RunConfiguration>
    <ResultsDirectory>ReferenceCatchProject\TestResults</ResultsDirectory>
    <SolutionDirectory>ReferenceCatchProject\</SolutionDirectory>
    <TargetPlatform>X86</TargetPlatform>
    <TargetFrameworkVersion>Framework40</TargetFrameworkVersion>
    <CollectSourceInformation>True</CollectSourceInformation>
    <DesignMode>True</DesignMode>
  </RunConfiguration>
  <CatchAdapter>
    <TestExeInclude />
    <TestExeExclude>
      <Regex>Cheese</Regex>
    </TestExeExclude>
  </CatchAdapter>
</RunSettings>";

        public ISettingsProvider GetSettings( string settingsName )
        {
            return this.Provider;
        }

        public ISettingsProvider Provider { get; set; }
    }
}
