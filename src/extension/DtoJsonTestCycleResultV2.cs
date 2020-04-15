namespace NUnit.Engine.Addins
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;

    [Serializable]
    class DtoJsonTestCycleResultV2
    {
        [JsonIgnore]
        public string UnitTestFullName { get; set; }

        public string status { get; set; }

        public string testCaseKey { get; set; }

        public string environment { get; set; }

        public string version { get; set; }

        public string actualStartDate { get; set; }

        public string actualEndDate { get; set; }

        public Dictionary<string, string> customFields { get; set; }

        public IEnumerable<Dictionary<string, string>> scriptResults { get; set; }
        
    }
}
