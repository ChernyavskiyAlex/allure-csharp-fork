﻿using Allure.Commons;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TechTalk.SpecFlow.Bindings;
using TechTalk.SpecFlow.Configuration;
using TechTalk.SpecFlow.ErrorHandling;
using TechTalk.SpecFlow.Infrastructure;
using TechTalk.SpecFlow.Tracing;

namespace Allure.SpecFlowPlugin
{
    class AllureBindingInvoker : BindingInvoker
    {
        public AllureBindingInvoker(RuntimeConfiguration runtimeConfiguration, IErrorProvider errorProvider) : base(runtimeConfiguration, errorProvider)
        {
        }
        public override object InvokeBinding(IBinding binding, IContextManager contextManager, object[] arguments, ITestTracer testTracer, out TimeSpan duration)
        {
            var hook = binding as HookBinding;
            if (hook != null)
            {
                try
                {
                    // fire Allure event
                    string containerId = (contextManager.ScenarioContext != null) ?
                        Allure.ScenarioContainerId(contextManager.ScenarioContext) :
                        Allure.FeatureId(contextManager.FeatureContext);

                    string fixtureId = Allure.NewId();

                    switch (hook.HookType)
                    {
                        case HookType.BeforeFeature:
                            if (hook.HookOrder != int.MinValue)
                                AllureLifecycle.Instance.StartBeforeFixture(
                                    containerId,
                                    fixtureId,
                                    Allure.GetFixtureResult(hook)
                                );
                            else
                            {
                                Allure.Lifecycle.StartTestContainer(new TestResultContainer()
                                {
                                    uuid = containerId
                                });

                                return base.InvokeBinding(binding, contextManager, arguments, testTracer, out duration);
                            }
                            break;

                        case HookType.BeforeScenario:
                            if (hook.HookOrder != int.MinValue && hook.HookOrder != int.MaxValue)
                                AllureLifecycle.Instance.StartBeforeFixture(
                                    containerId,
                                    fixtureId,
                                    Allure.GetFixtureResult(hook)
                                );
                            else
                                return base.InvokeBinding(binding, contextManager, arguments, testTracer, out duration);

                            break;


                        case HookType.AfterFeature:
                            if (hook.HookOrder != int.MaxValue)
                                AllureLifecycle.Instance.StartAfterFixture(
                                    containerId,
                                    fixtureId,
                                    Allure.GetFixtureResult(hook)
                                );
                            else
                            {
                                Allure.Lifecycle.WriteTestContainer(containerId);
                                return base.InvokeBinding(binding, contextManager, arguments, testTracer, out duration);
                            }
                            break;

                        case HookType.AfterScenario:
                            if (hook.HookOrder != int.MinValue && hook.HookOrder != int.MaxValue)
                                AllureLifecycle.Instance.StartAfterFixture(
                                    containerId,
                                    fixtureId,
                                    Allure.GetFixtureResult(hook)
                                );
                            else
                                return base.InvokeBinding(binding, contextManager, arguments, testTracer, out duration);

                            break;

                        case HookType.BeforeTestRun:
                        case HookType.AfterTestRun:
                        case HookType.BeforeScenarioBlock:
                        case HookType.AfterScenarioBlock:
                        case HookType.BeforeStep:
                        case HookType.AfterStep:
                        default:
                            return base.InvokeBinding(binding, contextManager, arguments, testTracer, out duration);
                    }

                    var result = base.InvokeBinding(binding, contextManager, arguments, testTracer, out duration);
                    AllureLifecycle.Instance.StopFixture(fixtureId);
                    return result;

                }
                catch (Exception ex)
                {
                    throw;
                }
            }
            else
                return base.InvokeBinding(binding, contextManager, arguments, testTracer, out duration);
        }
        //public override object InvokeBinding(IBinding binding, IContextManager contextManager, object[] arguments, ITestTracer testTracer, out TimeSpan duration)
        //{
        //    var hook = binding as HookBinding;

        //    // Skip Allure hooks
        //    if (hook != null && hook.HookOrder != int.MinValue && hook.HookOrder != int.MaxValue)
        //    {

        //        var Allure = contextManager.FeatureContext?.Get<Allure>();

        //        // create empty container to handle BeforeTestRun
        //        var container = new TestResultContainer();
        //        try
        //        {
        //            switch (hook.HookType)
        //            {
        //                case HookType.BeforeFeature:
        //                case HookType.AfterFeature:
        //                    contextManager.FeatureContext.TryGetValue(out container);
        //                    break;

        //                case HookType.BeforeScenario:
        //                case HookType.AfterScenario:
        //                case HookType.BeforeScenarioBlock:
        //                case HookType.AfterScenarioBlock:
        //                    contextManager.ScenarioContext.TryGetValue(out container);
        //                    break;

        //                case HookType.BeforeStep:
        //                case HookType.AfterStep:
        //                    contextManager.ScenarioContext.TryGetValue(out container);
        //                    return base.InvokeBinding(binding, contextManager, arguments, testTracer, out duration);

        //                case HookType.BeforeTestRun:
        //                case HookType.AfterTestRun:
        //                default:
        //                    return base.InvokeBinding(binding, contextManager, arguments, testTracer, out duration);
        //            }


        //            var fixture = new FixtureResult()
        //            {
        //                name = $"{ hook.Method.Name} [order = {hook.HookOrder}]"
        //            };


        //            if (hook.HookType.ToString().StartsWith("Before"))
        //                Allure.Lifecycle.StartBeforeFixture(
        //                        container.uuid,
        //                        hook.GetHashCode().ToString(),
        //                        fixture);
        //            else
        //                Allure.Lifecycle.StartAfterFixture(
        //                        container.uuid,
        //                        hook.GetHashCode().ToString(),
        //                        fixture);

        //            var result = base.InvokeBinding(binding, contextManager, arguments, testTracer, out duration);

        //            Allure.Lifecycle
        //                .StopFixture(x => x.status = Status.passed);

        //            return result;


        //        }

        //        // Exit point. Stop Fixture.
        //        catch (Exception ex)
        //        {
        //            if (!(hook.HookType == HookType.BeforeStep ^ hook.HookType == HookType.AfterStep))
        //                Allure.Lifecycle
        //                    .StopFixture(x =>
        //                    {
        //                        x.status = Status.broken;
        //                    });

        //            // Create empty scenario if it doesn't exist.
        //            var testCase = Allure.CreateTestResult(contextManager.FeatureContext?.FeatureInfo, hook.HookType.ToString());
        //            Allure.Lifecycle.StartTestCase(container.uuid, testCase);

        //            // Get scenario from ScenarioContext.
        //            contextManager.ScenarioContext?.TryGetValue(out testCase);

        //            // Stop and write scenario.
        //            Allure.Lifecycle
        //                .UpdateTestCase(testCase.uuid, x =>
        //                    {
        //                        x.status = Status.broken;
        //                        x.statusDetails = new StatusDetails()
        //                        {
        //                            message = ex.Message,
        //                            trace = ex.StackTrace
        //                        };
        //                    })
        //                .StopTestCase(testCase.uuid)
        //                .WriteTestCase(testCase.uuid);

        //            // clear step and scenario contexts
        //            contextManager.ScenarioContext?.StepContext?.Remove(typeof(StepResult).FullName);
        //            contextManager.ScenarioContext?.Remove(typeof(TestResult).FullName);

        //            throw;
        //        }
        //    }

        //    else
        //        return base.InvokeBinding(binding, contextManager, arguments, testTracer, out duration);
        //}
    }
}
