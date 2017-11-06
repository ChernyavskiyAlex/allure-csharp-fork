using Allure.Commons.Storage;
using Allure.Commons.Writer;
using HeyRed.Mime;
using System;
using System.IO;
namespace Allure.Commons
{
    public sealed class AllureLifecycle
    {
        private static readonly object Lockobj = new object();
        private readonly AllureStorage _storage;
        private readonly IAllureResultsWriter _writer;
        private static AllureLifecycle _instance;

        public string ResultsDirectory => _writer.ToString();

        public static AllureLifecycle Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (Lockobj)
                    {
                        _instance = _instance ?? CreateInstance();
                    }
                }
                return _instance;
            }
        }

        private AllureLifecycle(string outDir)
        {
            _writer = GetDefaultResultsWriter(outDir);
            _storage = new AllureStorage();
        }

        public static AllureLifecycle CreateInstance(string outDir = "allure-results")
        {
            return new AllureLifecycle(outDir);
        }

        #region TestContainer

        public AllureLifecycle StartTestContainer(TestResultContainer container)
        {
            container.start = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            _storage.Put(container.uuid, container);
            return this;
        }

        public AllureLifecycle StartTestContainer(string parentUuid, TestResultContainer container)
        {
            UpdateTestContainer(parentUuid, c => c.children.Add(container.uuid));
            StartTestContainer(container);
            return this;
        }

        public AllureLifecycle UpdateTestContainer(string uuid, Action<TestResultContainer> update)
        {
            update.Invoke(_storage.Get<TestResultContainer>(uuid));
            return this;
        }

        public AllureLifecycle StopTestContainer(string uuid)
        {
            UpdateTestContainer(uuid, c => c.stop = DateTimeOffset.Now.ToUnixTimeMilliseconds());
            return this;
        }

        public AllureLifecycle WriteTestContainer(string uuid)
        {
            _writer.Write(_storage.Remove<TestResultContainer>(uuid));
            return this;
        }

        #endregion

        #region Fixture

        public AllureLifecycle StartBeforeFixture(string parentUuid, string uuid, FixtureResult result)
        {
            UpdateTestContainer(parentUuid, container => container.befores.Add(result));
            StartFixture(uuid, result);
            return this;
        }

        public AllureLifecycle StartAfterFixture(string parentUuid, string uuid, FixtureResult result)
        {
            UpdateTestContainer(parentUuid, container => container.afters.Add(result));
            StartFixture(uuid, result);
            return this;
        }

        public AllureLifecycle UpdateFixture(Action<FixtureResult> update)
        {
            UpdateFixture(_storage.GetRootStep(), update);
            return this;
        }

        public AllureLifecycle UpdateFixture(string uuid, Action<FixtureResult> update)
        {
            update.Invoke(_storage.Get<FixtureResult>(uuid));
            return this;
        }

        public AllureLifecycle StopFixture(Action<FixtureResult> beforeStop)
        {
            UpdateFixture(beforeStop);
            return StopFixture(_storage.GetRootStep());
        }

        public AllureLifecycle StopFixture(string uuid)
        {
            var fixture = _storage.Remove<FixtureResult>(uuid);
            _storage.ClearStepContext();
            fixture.stage = Stage.finished;
            fixture.stop = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            return this;
        }

        #endregion

        #region TestCase

        public AllureLifecycle StartTestCase(string containerUuid, TestResult testResult)
        {
            UpdateTestContainer(containerUuid, c => c.children.Add(testResult.uuid));
            return StartTestCase(testResult);
        }

        public AllureLifecycle StartTestCase(TestResult testResult)
        {
            testResult.stage = Stage.running;
            testResult.start = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            _storage.Put(testResult.uuid, testResult);
            _storage.ClearStepContext();
            _storage.StartStep(testResult.uuid);
            return this;
        }

        public AllureLifecycle UpdateTestCase(string uuid, Action<TestResult> update)
        {
            update.Invoke(_storage.Get<TestResult>(uuid));
            return this;
        }

        public AllureLifecycle UpdateTestCase(Action<TestResult> update)
        {
            return UpdateTestCase(_storage.GetRootStep(), update);
        }

        public AllureLifecycle StopTestCase(Action<TestResult> beforeStop)
        {
            UpdateTestCase(beforeStop);
            return StopTestCase(_storage.GetRootStep());
        }

        public AllureLifecycle StopTestCase(string uuid)
        {
            var testResult = _storage.Get<TestResult>(uuid);
            testResult.stage = Stage.finished;
            testResult.stop = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            _storage.ClearStepContext();
            return this;
        }

        public AllureLifecycle WriteTestCase(string uuid)
        {
            _writer.Write(_storage.Remove<TestResult>(uuid));
            return this;
        }

        #endregion

        #region Step

        public AllureLifecycle StartStep(string uuid, StepResult result)
        {
            StartStep(_storage.GetCurrentStep(), uuid, result);
            return this;
        }

        public AllureLifecycle StartStep(string parentUuid, string uuid, StepResult stepResult)
        {
            stepResult.stage = Stage.running;
            stepResult.start = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            _storage.StartStep(uuid);
            _storage.AddStep(parentUuid, uuid, stepResult);
            return this;
        }

        public AllureLifecycle UpdateStep(Action<StepResult> update)
        {
            update.Invoke(_storage.Get<StepResult>(_storage.GetCurrentStep()));
            return this;
        }

        public AllureLifecycle UpdateStep(string uuid, Action<StepResult> update)
        {
            update.Invoke(_storage.Get<StepResult>(uuid));
            return this;
        }

        public AllureLifecycle StopStep(Action<StepResult> beforeStop)
        {
            UpdateStep(beforeStop);
            return StopStep(_storage.GetCurrentStep());
        }

        public AllureLifecycle StopStep(string uuid)
        {
            var step = _storage.Remove<StepResult>(uuid);
            step.stage = Stage.finished;
            step.stop = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            _storage.StopStep();
            return this;
        }

        public AllureLifecycle StopStep()
        {
            StopStep(_storage.GetCurrentStep());
            return this;
        }

        #endregion

        #region Attachment

        public AllureLifecycle AddAttachment(string name, string type, string path)
        {
            var fileExtension = new FileInfo(path).Extension;
            return AddAttachment(name, type, File.ReadAllBytes(path), fileExtension);
        }

        public AllureLifecycle AddAttachment(string name, string type, byte[] content, string fileExtension = "")
        {
            var source = $"{Guid.NewGuid():N}{AllureConstants.ATTACHMENT_FILE_SUFFIX}{fileExtension}";
            var attachment = new Attachment
            {
                name = name,
                type = type,
                source = source
            };
            _writer.Write(source, content);
            _storage.Get<ExecutableItem>(_storage.GetCurrentStep()).attachments.Add(attachment);
            return this;
        }

        public AllureLifecycle AddAttachment(string path, string name = null)
        {
            name = name ?? Path.GetFileName(path);
            var type = MimeTypesMap.GetMimeType(path);
            return AddAttachment(name, type, path);
        }

        #endregion

        #region Extensions

        public void CleanupResultDirectory()
        {
            _writer.CleanUp();
        }

        public AllureLifecycle AddScreenDiff(string testCaseUuid, string expectedPng, string actualPng, string diffPng)
        {
            AddAttachment(expectedPng, "expected")
                .AddAttachment(actualPng, "actual")
                .AddAttachment(diffPng, "diff")
                .UpdateTestCase(testCaseUuid, x => x.labels.Add(Label.TestType("screenshotDiff")));

            return this;
        }

        #endregion


        #region Privates

        private void StartFixture(string uuid, FixtureResult fixtureResult)
        {
            _storage.Put(uuid, fixtureResult);
            fixtureResult.stage = Stage.running;
            fixtureResult.start = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            _storage.ClearStepContext();
            _storage.StartStep(uuid);
        }

        internal IAllureResultsWriter GetDefaultResultsWriter(string outDir)
        {
            return new FileSystemResultsWriter(outDir);
        }

        #endregion
    }
}