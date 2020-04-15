namespace NUnit.Engine.Addins
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using System.Xml;
    using JetBrains.Annotations;
    using Newtonsoft.Json;
    using NUnit.Engine.Extensibility;

    [Extension]
    [ExtensionProperty("Format", "tm4jtestcycle")]
    public class TM4JTestCycleWriter : JsonResultWriterBase
    {
        private const string TestCaseStatusPassConst = "Pass";

        private const string TestCaseStatusFailConst = "Fail";

        protected override object CreateTm4JResult(List<XmlNode> testNodes)
        {
            string testLocalDateTime = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:sszz00");
            
            List<DtoJsonTestCycleResultV2> singleResults = GetSingleTestCaseResults(testNodes, testLocalDateTime);

            if (Properties.Settings.Default.ShallAggregateTestCycleResults)
            {
                var results = GetGroupedResults(singleResults, testLocalDateTime);
                return results;
            }

            return singleResults;
        }

        private static List<DtoJsonTestCycleResultV2> GetGroupedResults(
            List<DtoJsonTestCycleResultV2> singleResults,
            string testLocalDateTime)
        {
            var results = new List<DtoJsonTestCycleResultV2>();

            var groupedTestCases = new Dictionary<string, List<DtoJsonTestCycleResultV2>>();

            foreach (var singleResult in singleResults)
            {
                if (groupedTestCases.ContainsKey(singleResult.testCaseKey))
                {
                    groupedTestCases[singleResult.testCaseKey].Add(singleResult);
                }
                else
                {
                    groupedTestCases.Add(singleResult.testCaseKey, new List<DtoJsonTestCycleResultV2>());
                    groupedTestCases[singleResult.testCaseKey].Add(singleResult);
                }
            }

            foreach (var testCaseGroup in groupedTestCases)
            {
                if (!(testCaseGroup.Value.Count > 0))
                {
                    break;
                }

                string testCaseGroupKey = testCaseGroup.Key;
                string testedSoftwareVersion =
                    SubstituteEnvironmentVariable(Properties.Settings.Default.ReportedSoftwareVersion);

                List<Dictionary<string, string>> scriptResults = new List<Dictionary<string, string>>();
                string groupStatus = string.Empty;
                int resultIndex = 0;

                foreach (var singleTestCaseResult in testCaseGroup.Value)
                {
                    if (singleTestCaseResult.status == TestCaseStatusFailConst)
                    {
                        groupStatus = TestCaseStatusFailConst;
                    }

                    if (singleTestCaseResult.status == TestCaseStatusPassConst
                        && groupStatus != TestCaseStatusFailConst)
                    {
                        groupStatus = TestCaseStatusPassConst;
                    }

                    var newScriptResult = new Dictionary<string, string>();

                    string statusVerb;
                    switch (singleTestCaseResult.status)
                    {
                        case TestCaseStatusFailConst:
                            statusVerb = "failed";
                            break;

                        case TestCaseStatusPassConst:
                            statusVerb = "passed";
                            break;

                        default:
                            statusVerb = $"has unknown result '{singleTestCaseResult.status}'";
                            break;
                    }

                    newScriptResult.Add("index", resultIndex++.ToString());
                    newScriptResult.Add("status", singleTestCaseResult.status);
                    newScriptResult.Add("comment", $"{singleTestCaseResult.UnitTestFullName} {statusVerb}.");
                    scriptResults.Add(newScriptResult);
                }

                // if no test results for test case the test case fails. There is no "undefined" status.
                if (groupStatus == string.Empty)
                {
                    groupStatus = TestCaseStatusFailConst;
                }

                var serializerSettings = GetSerializerSettings();
                Dictionary<string, string> customPropertiesDictionary =
                    GetCustomPropertiesDictionary(serializerSettings);
                customPropertiesDictionary = GetUpdatedCustomPropertiesDictionary(customPropertiesDictionary);

                var testCaseGroupResult = new DtoJsonTestCycleResultV2()
                                              {
                                                  status = groupStatus,
                                                  testCaseKey = testCaseGroupKey,
                                                  version = testedSoftwareVersion,
                                                  environment =
                                                      SubstituteEnvironmentVariable(
                                                          Properties.Settings.Default.Environment),
                                                  actualStartDate = testLocalDateTime,
                                                  actualEndDate = testLocalDateTime,
                                                  customFields = customPropertiesDictionary,
                                                  scriptResults = scriptResults
                                              };

                results.Add(testCaseGroupResult);
            }

            return results;
        }

        private static List<DtoJsonTestCycleResultV2> GetSingleTestCaseResults(
            List<XmlNode> testNodes,
            string testLocalDateTime)
        {
            var results = new List<DtoJsonTestCycleResultV2>();

            string testedSoftwareVersion =
                SubstituteEnvironmentVariable(Properties.Settings.Default.ReportedSoftwareVersion);


            var serializerSettings = GetSerializerSettings();

            foreach (XmlNode testNode in testNodes)
            {
                GetTestCaseAttributeValues(testNode, out var unitTestFullName, out var result);
                VerifyTestCaseProperties(unitTestFullName, result);

                string description = GetDescription(testNode);

                if (string.IsNullOrEmpty(description))
                {
                    break;
                }

                string testCaseKey;
                if (description.StartsWith("{"))
                {
                    var testCaseDescription =
                        JsonConvert.DeserializeObject<DtoJsonTestCaseDescription>(description, serializerSettings);
                    testCaseKey = testCaseDescription.TestCaseKey;
                }
                else
                {
                    testCaseKey = description;
                }

                if (string.IsNullOrEmpty(testCaseKey))
                {
                    break;
                }

                bool isValidTestCaseKey = Regex.IsMatch(
                    testCaseKey,
                    Properties.Settings.Default.Tm4jTestCaseKeyPattern);

                if (!isValidTestCaseKey)
                {
                    break;
                }

                string testCaseStatus = result == NUnitTestResultAttributeSuccessValueConst
                                            ? TestCaseStatusPassConst
                                            : TestCaseStatusFailConst;

                //#if DEBUG
                //                Environment.SetEnvironmentVariable("bamboo.buildNumber", "71");
                //                Environment.SetEnvironmentVariable("bamboo.repository.git.branch", "lala-branch-1");
                //#endif

                Dictionary<string, string> customPropertiesDictionary =
                    GetCustomPropertiesDictionary(serializerSettings);
                customPropertiesDictionary = GetUpdatedCustomPropertiesDictionary(customPropertiesDictionary);

                List<Dictionary<string, string>> scriptResults = new List<Dictionary<string, string>>();

                var testCaseResult = new DtoJsonTestCycleResultV2
                                         {
                                             UnitTestFullName = unitTestFullName,
                                             status = testCaseStatus,
                                             testCaseKey = testCaseKey,
                                             version = testedSoftwareVersion,
                                             environment =
                                                 SubstituteEnvironmentVariable(Properties.Settings.Default.Environment),
                                             actualStartDate = testLocalDateTime,
                                             actualEndDate = testLocalDateTime,
                                             customFields = customPropertiesDictionary,
                                             scriptResults = scriptResults
                                         };

                results.Add(testCaseResult);
            }

            return results;
        }

        [NotNull]
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

            return new Dictionary<string, string>();
        }

        [NotNull]
        private static Dictionary<string, string> GetUpdatedCustomPropertiesDictionary(
            [NotNull] Dictionary<string, string> customPropertiesDictionary)
        {
            var customPropertiesUpdatedDictionary = new Dictionary<string, string>();
            foreach (var kvp in customPropertiesDictionary)
            {
                string newValue = SubstituteEnvironmentVariable(kvp.Value);
                customPropertiesUpdatedDictionary.Add(kvp.Key, newValue);
            }

            customPropertiesDictionary = customPropertiesUpdatedDictionary;
            
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
