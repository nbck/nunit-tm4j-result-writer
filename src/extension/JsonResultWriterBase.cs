namespace NUnit.Engine.Addins
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Xml;

    using JetBrains.Annotations;

    using Newtonsoft.Json;

    using NUnit.Engine.Extensibility;

    public abstract class JsonResultWriterBase : IResultWriter
    {
        // V2
        // private const string NUnitTestResultAttributeSuccessValueConst = "Success";
        // private const string NUnitTestNameAttributeConst = "name";
        // private const string NUnitTopNodeNameConst = "test-results";

        // V3
        protected const string NUnitTestResultAttributeSuccessValueConst = "Passed";
        private const string NUnitTestNameAttributeConst = "fullname";
        private const string NUnitTopNodeNameConst = "test-run";

        /// <summary>
        /// Checks if the output is writable. If the output is not
        /// writable, this method should throw an exception.
        /// </summary>
        /// <param name="outputPath"></param>
        public void CheckWritability(string outputPath)
        {
            using (new StreamWriter(outputPath, false, Encoding.UTF8)) { }
        }

        public void WriteResultFile(XmlNode resultNode, string outputPath)
        {
            using (var writer = new StreamWriter(outputPath, false, Encoding.UTF8))
            {
                WriteResultFile(resultNode, writer);
            }
        }

        public void WriteResultFile(XmlNode resultNode, TextWriter writer)
        {
            if (resultNode.Name != NUnitTopNodeNameConst)
            {
                ThrowInvalidXml();
            }

            var testCaseAttributes = new List<string> { NUnitTestNameAttributeConst, "result" };

            var testNodes = GetDescendantNodes(resultNode, "test-case", testCaseAttributes);

            var tm4JResult = this.CreateTm4JResult(testNodes);

            var serializerSettings = GetSerializerSettings();

            var configurationAsJson = JsonConvert.SerializeObject(tm4JResult, serializerSettings);

            writer.Write(configurationAsJson);
        }

        protected abstract object CreateTm4JResult(List<XmlNode> testNodes);

        protected static void VerifyTestCaseProperties(string name, string result)
        {
            if (name == string.Empty)
            {
                ThrowInvalidXml();
            }

            if (result == string.Empty)
            {
                ThrowInvalidXml();
            }
        }

        protected static void GetTestCaseAttributeValues(XmlNode testNode, out string name, out string result)
        {
            name = string.Empty;
            result = string.Empty;
            foreach (XmlAttribute attribute in testNode.Attributes)
            {
                switch (attribute.Name)
                {
                    case NUnitTestNameAttributeConst:
                        {
                            name = attribute.Value;
                            break;
                        }
                    case "result":
                        {
                            result = attribute.Value;
                            break;
                        }
                }
            }
        }

        protected static string GetDescription(XmlNode testNode)
        {
            foreach (XmlNode testSub1 in testNode.ChildNodes)
            {
                if (testSub1.Name == "properties")
                {
                    foreach (XmlNode propertyNode in testSub1.ChildNodes)
                    {
                        if (propertyNode.Name == "property" && propertyNode.Attributes != null)
                        {
                            GetIsDescriptionWithValue(propertyNode, out bool isDescription, out string value);

                            if (isDescription && !string.IsNullOrEmpty(value))
                            {
                                return value;
                            }
                        }
                    }

                    break;
                }
            }

            return string.Empty;
        }

        private static void GetIsDescriptionWithValue(
            [NotNull] XmlNode property,
            out bool hasName,
            [CanBeNull] out string value)
        {
            Debug.Assert(property.Attributes != null);

            hasName = false;
            value = null;
            foreach (XmlAttribute attribute in property.Attributes)
            {
                if (attribute.Name == "name" && attribute.Value == "Description")
                {
                    hasName = true;
                }
                else if (attribute.Name == "value")
                {
                    value = attribute.Value;
                }

                if (hasName && !string.IsNullOrEmpty(value))
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Gets the JSON serializer settings.
        /// </summary>
        /// <returns>
        /// The <see cref="Newtonsoft.Json.JsonSerializerSettings"/>.
        /// </returns>
        protected static JsonSerializerSettings GetSerializerSettings()
        {
            return new JsonSerializerSettings
                       {
                           Culture = CultureInfo.InvariantCulture,
                           Formatting = Newtonsoft.Json.Formatting.Indented
                       };
        }

        /// <summary>
        /// Recursively gets the descendant nodes of <paramref name="resultNode"/> with <paramref name="nodeName"/>
        /// and having all attributes with the names provided in <paramref name="requiredAttributes"/>.
        /// </summary>
        /// <param name="resultNode">The node to search.</param>
        /// <param name="nodeName">The name of the descendant nodes to return.</param>
        /// <param name="requiredAttributes">The required attributes (list of names) of the descendant nodes to return.</param>
        /// <returns></returns>
        private static List<XmlNode> GetDescendantNodes([NotNull] XmlNode resultNode, string nodeName, List<string> requiredAttributes)
        {
            List<XmlNode> result = new List<XmlNode>();

            foreach (XmlNode childNodeToCheck in resultNode.ChildNodes)
            {
                if (childNodeToCheck.Name == nodeName)
                {
                    var allMatched = AreAllAttributesPresent(childNodeToCheck, requiredAttributes);

                    if (allMatched)
                    {
                        result.Add(childNodeToCheck);
                    }
                }

                List<XmlNode> childDescendants = GetDescendantNodes(childNodeToCheck, nodeName, requiredAttributes);
                result.AddRange(childDescendants);
            }

            return result;
        }
        
        private static bool AreAllAttributesPresent([NotNull] XmlNode childNodeToCheck, List<string> requiredAttributes)
        {
            if (requiredAttributes.Count == 0)
            {
                return true;
            }

            if (childNodeToCheck.Attributes == null)
            {
                return false;
            }

            bool allMatched = true;
            foreach (string checkAttribute in requiredAttributes)
            {
                bool matched = false;
                foreach (XmlAttribute attribute in childNodeToCheck.Attributes)
                {
                    if (attribute.Name == checkAttribute)
                    {
                        matched = true;
                        break;
                    }
                }

                if (!matched)
                {
                    allMatched = false;
                    break;
                }
            }

            return allMatched;
        }

        private static void ThrowInvalidXml()
        {
            throw new XmlException("invalid XML");
        }
    }
}
