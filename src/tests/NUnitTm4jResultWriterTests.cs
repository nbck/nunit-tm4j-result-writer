namespace NUnit.Engine.Addins
{
    using NUnit.Engine.Extensibility;
    using NUnit.Framework;

    public class NUnitTm4jResultWriterTests
    {
        [Test]
        public void TestCycleWriterCheckExtensionAttribute()
        {
            Assert.That(typeof(TM4JTestCycleWriter),
                Has.Attribute<ExtensionAttribute>());
        }

        [Test]
        public void TestCycleWriterCheckExtensionPropertyAttribute()
        {
            Assert.That(typeof(TM4JTestCycleWriter),
                Has.Attribute<ExtensionPropertyAttribute>()
                    .With.Property("Name").EqualTo("Format")
                    .And.Property("Value").EqualTo("tm4jtestcycle"));
        }

        [Test]
        public void TestResultWriterCheckExtensionAttribute()
        {
            Assert.That(typeof(TM4JTestResultWriter),
                Has.Attribute<ExtensionAttribute>());
        }

        [Test]
        public void TestResultWriterCheckExtensionPropertyAttribute()
        {
            Assert.That(typeof(TM4JTestResultWriter),
                Has.Attribute<ExtensionPropertyAttribute>()
                    .With.Property("Name").EqualTo("Format")
                    .And.Property("Value").EqualTo("tm4j"));
        }
    }
}
