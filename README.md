[![Github Releases](https://img.shields.io/github/release/xkbeyer/CatchTestAdapter.svg)](https://github.com/xkbeyer/CatchTestAdapter/releases)

# CatchTestAdapter
A Visual Studio Extension to run [Catch2](https://github.com/catchorg/Catch2) unit tests within the Visual Studio TestExplorer. 

### Installation
Use the latest [CatchTestAdapter.vsix](https://github.com/xkbeyer/CatchTestAdapter/releases/latest).
It can be installed by double clicking on the downloaded file.

### Status
- Test cases are shown after discovery process.
- After RunAll test result of all `SECTION` (incl. `SCENARIO`) are shown. Sections are grouped below the `TEST_CASE` entry.
- Stack trace link to the source line.
- Catch2 TAGS are implemented as Traits.
- Tested with Visual Studio Community 2017.
- Needs Catch 2.4.0 regarding selective running a scenario subsection due to changes in white spaces.

### Testing
To run the unit tests against the `CatchTestAdapter.dll` of the solution, the `Local.runsettings` file must be loaded.
The `TestAdaptersPaths` should be adapted to point to the Solution directory.
```
<TestAdaptersPaths>c:\Path\to\the\Solution\bin\Debug</TestAdaptersPaths> 
```

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
- More documentation with examples.
- May be an option page to set some Catch2 test runner arguments.