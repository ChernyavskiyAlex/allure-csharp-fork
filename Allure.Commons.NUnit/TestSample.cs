using System;
using Allure.Commons.NUnit.Attributes;
using NUnit.Framework;

namespace Allure.Commons.NUnit
{
    [TestFixture]
    [Parallelizable(ParallelScope.Children)]
    public class Class1
    {
        private readonly AllureNUnit _al = new AllureNUnit();

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _al.OneTimeSetUp(true);
        }

        [SetUp]
        public void SetUp()
        {
            _al.SetUp();
        }
        [TearDown]
        public void TearDown()
        {
            _al.TearDown(this);
        }

        [Test]  
        [AllureIssue("ALR-1", "http://ya.ru")]
        [AllureIssue("ALR-148", "http://ya.ru")]
        [AllureTms("TMS-123", "http://ya.ru/TMS-123")]
        [Category("ASD")]
        [Category("132")]
        [Description("OLOLOLOLOLOL")]
        [AllureStory("story123")]
        [AllureStory("story1234")]
        [AllureFeature("F1")]
        [AllureFeature("F2")]
        [AllureSeverity(AllureSeverity.Critical)]
        public void Passed()
        {
            Console.WriteLine(_al.Allure.ResultsDirectory);
        }

        [Test]
        public void Test2([Range(1, 2)] int i)
        {
            Console.WriteLine("ASDSA");
        }

        [Test]
        public void Ignore()
        {
            Assert.Ignore("ASDASDS");
        }

        [Test]
        public void Inconclusive()
        {
            Assert.Inconclusive("Inconclusive");
        }

        [Test]
        public void WithExcepion()
        {
            throw new NotImplementedException("I DON'T KNOW");
        }

        [Test]
        public void WithError()
        {
            Assert.AreEqual("ASDSADSAADS",1231231243);
        }
    }
}