CatchTestAdapter
======
A Visual Studio Extension to run Catch unit tests within the Visual Studio TestExplorer. 

Installation
============
After building the project in release mode the CatchTestAdapter.vsix file is located in the bin\Release folder.

Usage
=====
After installation VS uses the TestAdapter.

Status
======
- Test cases are shown after discovery process. 
- Test result are shown.
- Stack trace link to the source line works now. 
- Scenario 
- BDD
- 2018-03-10 Tested with Visual Studio Community 2017.

Test
====
The Adapter is tested against the Solution in [CatchUnitTestRef](https://github.com/xkbeyer/CatchUnitTestRef)

Settings
========
You can configure the adapter by adding a `CatchAdapter` element to your .runsettings file.

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

TODO
====
- More tests in ReferenceCatchProject (may be combined with the test repo).
- Clean up dependencies.
- Test with VS2015.