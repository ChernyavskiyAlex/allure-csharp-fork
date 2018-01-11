using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Allure.Commons.Storage
{
    internal class AllureStorage
    {
        private readonly ConcurrentDictionary<string, object> _storage = new ConcurrentDictionary<string, object>();

        private readonly ThreadLocal<LinkedList<string>> _stepContext = new ThreadLocal<LinkedList<string>>(() => new LinkedList<string>());

        public T Get<T>(string uuid)
        {
            return (T) _storage[uuid];
        }

        public T Put<T>(string uuid, T item)
        {
            return (T) _storage.GetOrAdd(uuid, item);
        }

        public T Remove<T>(string uuid)
        {
            _storage.TryRemove(uuid, out object value);
            return (T) value;
        }

        public void ClearStepContext()
        {
            _stepContext.Value.Clear();
        }

        public void StartStep(string uuid)
        {
            _stepContext.Value.AddFirst(uuid);
        }

        public void StopStep()
        {
            _stepContext.Value.RemoveFirst();
        }

        public string GetRootStep()
        {
            return _stepContext.Value.Last?.Value;
        }

        public string GetCurrentStep()
        {
            return _stepContext.Value.First?.Value;
        }

        public void AddStep(string parentUuid, string uuid, StepResult stepResult)
        {
            Put(uuid, stepResult);
            Get<ExecutableItem>(parentUuid).steps.Add(stepResult);
        }
    }
}