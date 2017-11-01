using System;

namespace Allure.Commons.NUnit.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class AllureStoryAttribute : Attribute
    {
        public string Value { get; }

        public AllureStoryAttribute(string story)
        {
            Value = story;
        }
    }
}