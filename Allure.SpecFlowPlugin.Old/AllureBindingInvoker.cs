using Allure.Commons;
using CsvHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Bindings;
using TechTalk.SpecFlow.Configuration;
using TechTalk.SpecFlow.ErrorHandling;
using TechTalk.SpecFlow.Infrastructure;
using TechTalk.SpecFlow.Tracing;

namespace Allure.SpecFlowPlugin
{
    internal class AllureBindingInvoker : BindingInvoker
    {
        private static readonly AllureLifecycle Allure = AllureLifecycle.Instance;

        public AllureBindingInvoker(RuntimeConfiguration runtimeConfiguration, IErrorProvider errorProvider) : base(runtimeConfiguration, errorProvider)
        {
        }
        public override object InvokeBinding(IBinding binding, IContextManager contextManager, object[] arguments, ITestTracer testTracer, out TimeSpan duration)
        {
            // process hook
            if (binding is HookBinding hook)
            {
                var featureContainerId = AllureHelper.GetFeatureContainerId(contextManager.FeatureContext?.FeatureInfo);

                switch (hook.HookType)
                {
                    case HookType.BeforeFeature:
                        if (hook.HookOrder == int.MinValue)
                        {
                            // starting point
                            var featureContainer = new TestResultContainer()
                            {
                                uuid = AllureHelper.GetFeatureContainerId(contextManager.FeatureContext?.FeatureInfo)
                            };
                            Allure.StartTestContainer(featureContainer);

                            contextManager.FeatureContext.Set(new HashSet<TestResultContainer>());
                            contextManager.FeatureContext.Set(new HashSet<TestResult>());

                            return base.InvokeBinding(binding, contextManager, arguments, testTracer, out duration);
                        }
                        else
                            try
                            {
                                StartFixture(hook, featureContainerId);
                                var result = base.InvokeBinding(binding, contextManager, arguments, testTracer, out duration);
                                Allure.StopFixture(x => x.status = Status.passed);
                                return result;
                            }
                            catch (Exception ex)
                            {
                                Allure.StopFixture(x => x.status = Status.broken);

                                // if BeforeFeature is failed execution is stopped. We need to create, update, stop and write everything here.

                                // create fake scenario container
                                var scenarioContainer = AllureHelper.StartTestContainer(contextManager.FeatureContext, null);

                                // start fake scenario
                                var scenario = AllureHelper.StartTestCase(scenarioContainer.uuid, contextManager.FeatureContext, null);

                                // update, stop and write
                                Allure
                                    .StopTestCase(x =>
                                    {
                                        x.status = Status.broken;
                                        x.statusDetails = new StatusDetails()
                                        {
                                            message = ex.Message,
                                            trace = ex.StackTrace
                                        };
                                    })
                                    .WriteTestCase(scenario.uuid)
                                    .StopTestContainer(scenarioContainer.uuid)
                                    .WriteTestContainer(scenarioContainer.uuid)
                                    .StopTestContainer(featureContainerId)
                                    .WriteTestContainer(featureContainerId);

                                throw;
                            }

                    case HookType.BeforeStep:
                    case HookType.AfterStep:
                        {
                            var scenario = AllureHelper.GetCurrentTestCase(contextManager.ScenarioContext);

                            try
                            {
                                return base.InvokeBinding(binding, contextManager, arguments, testTracer, out duration);
                            }
                            catch (Exception ex)
                            {
                                Allure
                                    .UpdateTestCase(scenario.uuid,
                                        x =>
                                        {
                                            x.status = Status.broken;
                                            x.statusDetails = new StatusDetails()
                                            {
                                                message = ex.Message,
                                                trace = ex.StackTrace
                                            };
                                        });
                                throw;
                            }
                        }

                    case HookType.BeforeScenario:
                    case HookType.AfterScenario:
                        if (hook.HookOrder == int.MinValue || hook.HookOrder == int.MaxValue)
                            return base.InvokeBinding(binding, contextManager, arguments, testTracer, out duration);
                        else
                        {
                            var scenarioContainer = AllureHelper.GetCurrentTestConainer(contextManager.ScenarioContext);

                            try
                            {
                                StartFixture(hook, scenarioContainer.uuid);
                                var result = base.InvokeBinding(binding, contextManager, arguments, testTracer, out duration);
                                Allure.StopFixture(x => x.status = Status.passed);
                                return result;
                            }
                            catch (Exception ex)
                            {
                                Allure.StopFixture(x => x.status = Status.broken);

                                // get or add new scenario
                                var scenario = AllureHelper.GetCurrentTestCase(contextManager.ScenarioContext) ??
                                    AllureHelper.StartTestCase(scenarioContainer.uuid, contextManager.FeatureContext, contextManager.ScenarioContext);

                                Allure.UpdateTestCase(scenario.uuid,
                                    x =>
                                    {
                                        x.status = Status.broken;
                                        x.statusDetails = new StatusDetails()
                                        {
                                            message = ex.Message,
                                            trace = ex.StackTrace
                                        };
                                    });
                                throw;
                            }
                        }

                    case HookType.AfterFeature:
                        if (hook.HookOrder == int.MaxValue)
                        // finish point
                        {
                            WriteScenarios(contextManager);
                            Allure
                                   .StopTestContainer(featureContainerId)
                                   .WriteTestContainer(featureContainerId);

                            return base.InvokeBinding(binding, contextManager, arguments, testTracer, out duration);
                        }
                        else
                        {

                            try
                            {
                                StartFixture(hook, featureContainerId);
                                var result = base.InvokeBinding(binding, contextManager, arguments, testTracer, out duration);
                                Allure.StopFixture(x => x.status = Status.passed);
                                return result;
                            }
                            catch (Exception ex)
                            {
                                var scenario = contextManager.FeatureContext.Get<HashSet<TestResult>>().Last();
                                Allure
                                    .StopFixture(x => x.status = Status.broken)
                                    .UpdateTestCase(scenario.uuid,
                                        x =>
                                        {
                                            x.status = Status.broken;
                                            x.statusDetails = new StatusDetails()
                                            {
                                                message = ex.Message,
                                                trace = ex.StackTrace
                                            };
                                        });

                                WriteScenarios(contextManager);

                                Allure
                                    .StopTestContainer(featureContainerId)
                                    .WriteTestContainer(featureContainerId);

                                throw;
                            }
                        }

                    case HookType.BeforeScenarioBlock:
                    case HookType.AfterScenarioBlock:
                    case HookType.BeforeTestRun:
                    case HookType.AfterTestRun:
                    default:
                        return base.InvokeBinding(binding, contextManager, arguments, testTracer, out duration);
                }
            }
            else
            {
                return base.InvokeBinding(binding, contextManager, arguments, testTracer, out duration);
            }
        }

        private void StartFixture(HookBinding hook, string containerId)
        {
            if (hook.HookType.ToString().StartsWith("Before"))
                Allure.StartBeforeFixture(containerId, AllureHelper.NewId(), AllureHelper.GetFixtureResult(hook));
            else
                Allure.StartAfterFixture(containerId, AllureHelper.NewId(), AllureHelper.GetFixtureResult(hook));
        }
        private static void StartStep(StepInfo stepInfo, string containerId)
        {
            var stepResult = new StepResult()
            {
                name = $"{stepInfo.StepDefinitionType} {stepInfo.Text}"
            };

            Allure.StartStep(containerId, AllureHelper.NewId(), stepResult);

            if (stepInfo.Table != null)
            {
                var csvFile = $"{Guid.NewGuid().ToString()}.csv";
                using (var csv = new CsvWriter(File.CreateText(csvFile)))
                {
                    foreach (var item in stepInfo.Table.Header)
                    {
                        csv.WriteField(item);
                    }
                    csv.NextRecord();
                    foreach (var row in stepInfo.Table.Rows)
                    {
                        foreach (var item in row.Values)
                        {
                            csv.WriteField(item);
                        }
                        csv.NextRecord();
                    }
                }
                Allure.AddAttachment("table", "text/csv", csvFile);
            }
        }
        private static void WriteScenarios(IContextManager contextManager)
        {
            foreach (var s in contextManager.FeatureContext.Get<HashSet<TestResult>>())
            {
                Allure.WriteTestCase(s.uuid);
            }

            foreach (var c in contextManager.FeatureContext.Get<HashSet<TestResultContainer>>())
            {
                Allure
                    .StopTestContainer(c.uuid)
                    .WriteTestContainer(c.uuid);
            }
        }
    }
}