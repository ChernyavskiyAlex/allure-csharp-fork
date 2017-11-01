using System;
using System.Collections.Generic;
using System.IO;
using Allure.Commons;
using Allure.Commons.Model;
using NUnit.Framework;

namespace ClassLibrary1
{
    public class Class1
    {
        static AllureLifecycle cycle = AllureLifecycle.Instance;
        public string Guid = System.Guid.NewGuid().ToString();

        [SetUp]
        public void SetUp()
        {
            var result = new TestResult();
            result.uuid = Guid;
            result.name = TestContext.CurrentContext.Test.Name;
            result.testCaseId = Guid;
        
            cycle.StartTestCase(result);
        }

        [TearDown]
        public void TearDown()
        {
            cycle.StopTestCase(x => x.status = Status.passed);
            cycle.WriteTestCase(Guid);
        }
        [Test]
        [AllureStory]
        public void Test([Range(1,5)] int i)
        {
//            Directory.Delete(cycle.ResultsDirectory, true);
            Console.WriteLine(Guid);
           
            var labels = new List<Label> { Label.Thread(), Label.Story("ASDASDQWE") };
//            cycle.StartTestCase(new TestResult { uuid = name, name = name, labels = labels });
        
            
            Console.WriteLine(i);
            Console.WriteLine(cycle.ResultsDirectory);
        }
    }

    public class AllureStoryAttribute : Attribute
    {

    }
}
