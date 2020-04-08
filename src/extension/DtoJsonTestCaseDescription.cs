namespace NUnit.Engine.Addins
{
    using System;

    [Serializable]
    class DtoJsonTestCaseDescription
    {
        //public string TestCycleKey { get; set; }

        public string TestCaseKey { get; set; }

        //public string TestCaseVersion { get; set; }

        public string TestedAppVersion { get; set; }
    }
}
