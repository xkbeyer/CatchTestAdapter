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

TODO
====
- More tests in ReferenceCatchProject (may be combined with the test repo).
- Clean up dependencies.
- Test with VS2015.