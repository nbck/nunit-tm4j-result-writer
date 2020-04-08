namespace NUnit.Engine.Addins
{
    using System.Collections.Generic;
    using System.Xml;
    using NUnit.Engine.Extensibility;

    [Extension]
    [ExtensionProperty("Format", "tm4j")]
    public class TM4JTestResultWriter : JsonResultWriterBase
    {
        protected override object CreateTm4JResult(List<XmlNode> testNodes)
        {
            List<execution> executions = new List<execution>();
            foreach (XmlNode testNode in testNodes)
            {
                GetTestCaseAttributeValues(testNode, out var name, out var result);
                VerifyTestCaseProperties(name, result);

                string description = GetDescription(testNode);

                if (!string.IsNullOrEmpty(description))
                {
                    string tm4JTestResultText = result == NUnitTestResultAttributeSuccessValueConst ? "Passed" : "Failed";

                    var testCase = new Case { key = description };
                    var newExecution = new execution { source = name, result = tm4JTestResultText, testCase = testCase };
                    executions.Add(newExecution);
                }
            }

            object tm4JResult = new DtoJsonAutomatedTestResult { Executions = executions, Version = 1 };
            return tm4JResult;
        }
    }
}
