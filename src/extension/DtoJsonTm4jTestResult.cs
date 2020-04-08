namespace NUnit.Engine.Addins
{
    using System;

    [Serializable]
    class DtoJsonTm4jTestResult
    {
        public string status { get; set; }

        public string testCaseKey { get; set; }

        public string environment { get; set; }

        public string version { get; set; }

        public string actualStartDate { get; set; }

        public string actualEndDate { get; set; }
    }
}
