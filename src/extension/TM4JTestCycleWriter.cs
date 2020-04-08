namespace NUnit.Engine.Addins
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using System.Xml;
    using Newtonsoft.Json;
    using NUnit.Engine.Extensibility;

    [Extension]
    [ExtensionProperty("Format", "tm4jtestcycle")]
    public class TM4JTestCycleWriter : JsonResultWriterBase
    {
        protected override object CreateTm4JResult(List<XmlNode> testNodes)
        {
            string testLocalDateTime = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:sszz00");

            var results = new List<object>();
            foreach (XmlNode testNode in testNodes)
            {
                GetTestCaseAttributeValues(testNode, out var name, out var result);
                VerifyTestCaseProperties(name, result);

                string description = GetDescription(testNode);

                if (string.IsNullOrEmpty(description))
                {
                    break;
                }

                DtoJsonTestCaseDescription testCaseDescription = null;
                if (!description.StartsWith("{"))
                {
                    break;
                }

                var serializerSettings = GetSerializerSettings();

                testCaseDescription =
                    JsonConvert.DeserializeObject<DtoJsonTestCaseDescription>(description, serializerSettings);

                if (string.IsNullOrEmpty(testCaseDescription.TestCaseKey))
                {
                    break;
                }

                bool isValidTestCaseKey = Regex.IsMatch(
                    testCaseDescription.TestCaseKey,
                    Properties.Settings.Default.Tm4jTestCaseKeyPattern);

                if (!isValidTestCaseKey)
                {
                    break;
                }

                string testCaseResult = result == NUnitTestResultAttributeSuccessValueConst ? "Pass" : "Fail";

                Dictionary<string, string> customPropertiesDictionary =
                    GetCustomPropertiesDictionary(serializerSettings);

#if DEBUG
                Environment.SetEnvironmentVariable("bamboo.buildNumber", "71");
                Environment.SetEnvironmentVariable("bamboo.repository.git.branch", "lala-branch-1");
#endif

                customPropertiesDictionary = GetUpdatedCustomPropertiesDictionary(customPropertiesDictionary);

                if (customPropertiesDictionary != null)
                {
                    DtoJsonTestCycleResultWithCustomProperties testCase = new DtoJsonTestCycleResultWithCustomProperties
                                                                              {
                                                                                  status = testCaseResult,
                                                                                  testCaseKey =
                                                                                      testCaseDescription.TestCaseKey,
                                                                                  version =
                                                                                      testCaseDescription
                                                                                          .TestedAppVersion,
                                                                                  environment =
                                                                                      SubstituteEnvironmentVariable(
                                                                                          Properties.Settings.Default
                                                                                              .Environment),
                                                                                  actualStartDate = testLocalDateTime,
                                                                                  actualEndDate = testLocalDateTime,
                                                                                  customFields =
                                                                                      customPropertiesDictionary
                                                                              };

                    results.Add(testCase);
                }
                else
                {
                    DtoJsonTm4jTestResult testCase = new DtoJsonTm4jTestResult
                                                         {
                                                             status = testCaseResult,
                                                             testCaseKey = testCaseDescription.TestCaseKey,
                                                             version = testCaseDescription.TestedAppVersion,
                                                             environment =
                                                                 SubstituteEnvironmentVariable(
                                                                     Properties.Settings.Default.Environment),
                                                             actualStartDate = testLocalDateTime,
                                                             actualEndDate = testLocalDateTime,
                                                         };
                    results.Add(testCase);
                }
            }

            return results;
        }

        private static Dictionary<string, string> GetCustomPropertiesDictionary(JsonSerializerSettings serializerSettings)
        {
            try
            {
                string customPropertiesJson = Properties.Settings.Default.CustomPropertiesJson;
                var customPropertiesDictionary =
                    JsonConvert.DeserializeObject<Dictionary<string, string>>(customPropertiesJson, serializerSettings);

                return customPropertiesDictionary;
            }
            catch (Exception)
            {
            }

            return null;
        }

        private static Dictionary<string, string> GetUpdatedCustomPropertiesDictionary(Dictionary<string, string> customPropertiesDictionary)
        {
            if (customPropertiesDictionary != null)
            {
                var customPropertiesUpdatedDictionary = new Dictionary<string, string>();
                foreach (var kvp in customPropertiesDictionary)
                {
                    string newValue = SubstituteEnvironmentVariable(kvp.Value);
                    customPropertiesUpdatedDictionary.Add(kvp.Key, newValue);
                }

                customPropertiesDictionary = customPropertiesUpdatedDictionary;
            }

            return customPropertiesDictionary;
        }

        private static string SubstituteEnvironmentVariable(string evalString)
        {
            string newValue = evalString;
            if (evalString.StartsWith("env:"))
            {
                string envName = evalString.Substring(4);
                string envValue = System.Environment.GetEnvironmentVariable(envName);
                if (!string.IsNullOrEmpty(envValue))
                {
                    newValue = envValue;
                }
                else
                {
                    newValue = string.Empty;
                }
            }

            return newValue;
        }
    }
}
