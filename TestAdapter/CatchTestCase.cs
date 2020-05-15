using System.Xml.Serialization;

namespace Catch.TestAdapter.Tests
{
    [XmlRoot("Catch")]
    public class Catch
    {
        [XmlArray("Group")]
        [XmlArrayItem("TestCase", typeof(TestCase))]
        public TestCase[] TestCases { get; set; }
    }

    [XmlRoot("TestCase")]
    public class TestCase
    {
        private Expression[] _expressions = new Expression[] { };
        private TestCase[] _sections = new TestCase[] { };

        [XmlAttribute("name")]
        public string Name { get; set; } = "";
        [XmlAttribute("tags")]
        public string Tags { get; set; } = "";
        [XmlAttribute("filename")]
        public string Filename { get; set; } = "";
        [XmlAttribute("line")]
        public string Line { get; set; } = "";
        [XmlElement("Expression", typeof(Expression))]
        public Expression[] Expressions {
            get { return _expressions; }
            set { if (value != null) _expressions = value; }
        }
        [XmlElement("Warning", typeof(string))]
        public string[] Warning { get; set; } = new string[] { };
        [XmlElement("Info",typeof(string))]
        public string[] Info { get; set; } = new string[] { };
        [XmlElement("Failure", typeof(Failure))]
        public Failure Failure { get; set; }
        [XmlElement("Section", typeof(TestCase))]
        public TestCase[] Sections {
            get { return _sections; }
            set { if (value != null) _sections = value; }
        }
        [XmlElement("OverallResult", typeof(OverallResult))]
        public OverallResult Result { get; set; }
        [XmlElement("OverallResults", typeof(OverallResults), IsNullable = true)]   
        public OverallResults Results { get; set; }
    }
    public class Failure
    {
        [XmlAttribute("filename")]
        public string Filename { get; set; } = "";
        [XmlAttribute("line")]
        public string Line { get; set; } = "";
        [XmlText]
        public string text;
    }
    public class Expression
    {
        [XmlAttribute("success")]
        public string Success { get; set; } = "";
        [XmlAttribute("type")]
        public string Type { get; set; } = "";
        [XmlAttribute("filename")]
        public string Filename { get; set; } = "";
        [XmlAttribute("line")]
        public string Line { get; set; } = "";
        [XmlElement("Original")]
        public string Original = "";
        [XmlElement("Expanded")]
        public string Expanded = "";
    }

    public class OverallResult
    {
        [XmlAttribute("success")]
        public string Success = "";
        [XmlAttribute("durationInSeconds")]
        public string Duration = "";
    }
    public class OverallResults
    {
        [XmlAttribute("successes")]
        public string Successes = "";
        [XmlAttribute("failures")]
        public string Failures = "";
        [XmlAttribute("expectedFailures")]
        public string ExpectedFailures = "";
        [XmlAttribute("durationInSeconds")]
        public string Duration = "";
    }
}
