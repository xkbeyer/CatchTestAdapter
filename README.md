[![Github Releases](https://img.shields.io/github/release/xkbeyer/CatchTestAdapter.svg)](https://github.com/xkbeyer/CatchTestAdapter/releases)
[![MIT license](http://img.shields.io/badge/license-MIT-brightgreen.svg)](http://opensource.org/licenses/MIT)
# CatchTestAdapter
A Visual Studio Extension to run [Catch2](https://github.com/catchorg/Catch2) unit tests within the Visual Studio TestExplorer. 

### Installation
Use the latest [CatchTestAdapter.vsix](https://github.com/xkbeyer/CatchTestAdapter/releases/latest).
It can be installed by double clicking on the downloaded file.

### Visual Studio Compatibility
##### v1.6.x: Works with Visual Studio 2019 v16.2 and newer 
Beginning with Visual Studio 2019 v16.2 the CatchTestAdapter 1.5.1 is broken. 
The new TestExplorer Window doesn't accept new test cases as a sub test case.
As a result the Catch2 `SECTION`s are no longer shown as sub test cases after the first run. 
Now they are shown as sub results of a test case.

##### v1.5.1: Works with Visual Studio prior to v16.2 

### Status
  
- Test cases are shown after discovery process.
- Stack trace link to the source line.
- Catch2 `TAGS` are implemented as Traits.
- `SECTION` and `SCENARIO` are shown as sub results.

### Testing
To run the unit tests against the `CatchTestAdapter.dll` of the solution, the `Local.runsettings` file must be loaded.
The `TestAdaptersPaths` should be adapted to point to the Solution directory.
```xml
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
