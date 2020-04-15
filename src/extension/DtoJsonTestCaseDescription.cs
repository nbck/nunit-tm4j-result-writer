namespace NUnit.Engine.Addins
{
    using System;

    [Serializable]
    class DtoJsonTestCaseDescription
    {
        public string TestCaseKey { get; set; }

        public string TestedAppVersion { get; set; }
    }
}
