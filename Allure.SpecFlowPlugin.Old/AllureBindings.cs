using Allure.Commons;
using TechTalk.SpecFlow;

namespace Allure.SpecFlowPlugin
{
    [Binding]
    public class AllureBindings
    {
        private static readonly AllureLifecycle Allure = AllureLifecycle.Instance;

        private readonly FeatureContext _featureContext;
        private readonly ScenarioContext _scenarioContext;

        public AllureBindings(FeatureContext featureContext, ScenarioContext scenarioContext)
        {
            _featureContext = featureContext;
            _scenarioContext = scenarioContext;
        }

        [BeforeFeature(Order = int.MinValue)]
        public static void FirstBeforeFeature()
        {
            // start feature container in BindingInvoker
        }

        [AfterFeature(Order = int.MaxValue)]
        public static void LastAfterFeature()
        {
            // write feature container in BindingInvoker
        }

        [BeforeScenario(Order = int.MinValue)]
        public void FirstBeforeScenario()
        {
            AllureHelper.StartTestContainer(_featureContext, _scenarioContext);
            //AllureHelper.StartTestCase(scenarioContainer.uuid, featureContext, scenarioContext);
        }

        [BeforeScenario(Order = int.MaxValue)]
        public void LastBeforeScenario()
        {
            // start scenario after last fixture and before the first step to have valid current step context in allure storage
            var scenarioContainer = AllureHelper.GetCurrentTestConainer(_scenarioContext);
            AllureHelper.StartTestCase(scenarioContainer.uuid, _featureContext, _scenarioContext);
        }

        [AfterScenario(Order = int.MinValue)]
        public void FirstAfterScenario()
        {
            var scenarioId = AllureHelper.GetCurrentTestCase(_scenarioContext).uuid;

            // update status to passed if there were no step of binding failures
            Allure
                .UpdateTestCase(scenarioId,
                    x => x.status = (x.status != Status.none) ? x.status : Status.passed)
                .StopTestCase(scenarioId);
        }
    }
}
