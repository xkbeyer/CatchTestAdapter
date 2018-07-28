[![Github Releases](https://img.shields.io/github/release/xkbeyer/CatchTestAdapter.svg)](https://github.com/xkbeyer/CatchTestAdapter/releases)

# CatchTestAdapter
A Visual Studio Extension to run [Catch2](https://github.com/catchorg/Catch2) unit tests within the Visual Studio TestExplorer. 

### Installation
Use the latest [CatchTestAdapter.vsix](https://github.com/xkbeyer/CatchTestAdapter/releases/latest). 

### Status
- Test cases are shown after discovery process. 
- Test result are shown.
- Stack trace link to the source line works now. 
- Section
- BDD Scenario
- Traits
- Tested with Visual Studio Community 2017.

### Test
The Adapter is tested against the Solution in [CatchUnitTestRef](https://github.com/xkbeyer/CatchUnitTestRef) and the [TestAdapterTest](https://github.com/xkbeyer/CatchTestAdapter/tree/master/TestAdapterTest).

### Settings

You can configure the adapter by adding a `CatchAdapter` element to your .runsettings file.
If you do not manually set a runsettings file from the Test menu in Visual Studio, this
adapter will look for `*.runsettings` files from the solution directory and all its ancestors.
Settings closer to the solution take precedence, lists are merged.

```xml
<?xml version="1.0" encoding="utf-8"?>
<RunSettings>
  <!-- Catch adapter -->
  <CatchAdapter>
    <!-- Regexes one of which must match an executable's name
         for tests to be searched from it. -->
    <TestExeInclude>
      <Regex>.*\.Test\.exe</Regex>
    </TestExeInclude>
    <!-- If one of these regexes matches, the exe is excluded
         even if it matches an include. -->
    <TestExeExclude>
      <Regex>Cheese</Regex>
    </TestExeExclude>
  </CatchAdapter>
</RunSettings>
```

### TODO

- More tests in ReferenceCatchProject (may be combined with the CatchUnitTestRef test repo).
- Test with VS2015.
- May be an option page to set some Catch test runner arguments.