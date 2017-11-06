using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Allure.Commons.NUnit.Attributes;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace Allure.Commons.NUnit
{
    public class AllureNUnit
    {
        public readonly AllureLifecycle Allure = AllureLifecycle.Instance;

        public void OneTimeSetUp(bool cleanAllureDir = false)
        {
            if(cleanAllureDir) Allure.CleanupResultDirectory();
            var fixture = new TestResultContainer
            {
                uuid = TestContext.CurrentContext.Test.ID,
                name = TestContext.CurrentContext.Test.ClassName
            };
            Allure.StartTestContainer(fixture);
        }

        public void OneTimeTearDown()
        {
            Allure.StopTestContainer(TestContext.CurrentContext.Test.ID);
            Allure.WriteTestContainer(TestContext.CurrentContext.Test.ID);
        }

        public void SetUp()
        {
            var test = new TestResult
            {
                uuid = TestContext.CurrentContext.Test.ID,
                name = TestContext.CurrentContext.Test.MethodName,
                fullName = TestContext.CurrentContext.Test.FullName,
                labels = new List<Label>
                {
                    Label.Suite(TestContext.CurrentContext.Test.ClassName),
                    Label.Thread(),
                }
            };
            Allure.StartTestCase(test);
        }

        public void TearDown(object obj)
        {
            Allure.UpdateTestCase(x => x.statusDetails = new StatusDetails
            {
                message = TestContext.CurrentContext.Result.Message,
                trace = TestContext.CurrentContext.Result.StackTrace
            });

            var testMethod = obj.GetType().GetMethod(TestContext.CurrentContext.Test.MethodName);

            if (testMethod != null)
            {
                foreach (var attribute in testMethod.GetCustomAttributes().OfType<AllureIssueAttribute>())
                {
                    Allure.UpdateTestCase(x => x.links.Add(attribute.Link));
                }

                foreach (var attribute in testMethod.GetCustomAttributes().OfType<AllureTmsAttribute>())
                {
                    Allure.UpdateTestCase(x => x.links.Add(attribute.Link));
                }

                foreach (var attribute in testMethod.GetCustomAttributes().OfType<CategoryAttribute>())
                {
                    Allure.UpdateTestCase(x => x.labels.Add(Label.Tag(attribute.Name)));
                }

                foreach (var attribute in testMethod.GetCustomAttributes().OfType<AllureSeverityAttribute>())
                {
                    Allure.UpdateTestCase(x => x.labels.Add(Label.Severity(attribute.Value)));
                }
                foreach (var attribute in testMethod.GetCustomAttributes().OfType<AllureFeatureAttribute>())
                {
                    Allure.UpdateTestCase(x => x.labels.Add(Label.Feature(attribute.Value)));
                }

                foreach (var attribute in testMethod.GetCustomAttributes().OfType<AllureStoryAttribute>())
                {
                    Allure.UpdateTestCase(x => x.labels.Add(Label.Story(attribute.Value)));
                }

                foreach (var attribute in testMethod.GetCustomAttributes().OfType<DescriptionAttribute>())
                {
                    Allure.UpdateTestCase(x => x.description = attribute.Properties.Get("Description").ToString());
                }
            }

            Allure.StopTestCase(x => x.status = GetNunitStatus(TestContext.CurrentContext.Result.Outcome.Status));
            Allure.WriteTestCase(TestContext.CurrentContext.Test.ID);
        }

        private Status GetNunitStatus(TestStatus status)
        {
            switch (status)
            {
                case TestStatus.Inconclusive:
                    return Status.broken;
                case TestStatus.Skipped:
                    return Status.skipped;
                case TestStatus.Passed:
                    return Status.passed;
                case TestStatus.Warning:
                    return Status.broken;
                case TestStatus.Failed:
                    return Status.failed;
                default:
                    return Status.none;
            }
        }
    }
}