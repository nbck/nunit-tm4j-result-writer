namespace NUnit.Engine.Addins
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;

    [Serializable]
    class DtoJsonAutomatedTestResult
    {
        [JsonProperty("version")]
        public double Version { get; set; }


        [JsonProperty("executions")]
        public List<execution> Executions { get; set; }
    }

    class execution
    {
        public string source { get; set; }
        public string result { get; set; }
        public Case testCase { get; set; }

    }

    class Case
    {
        public string key { get; set; }
    }
}
