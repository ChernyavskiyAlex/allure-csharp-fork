using System;

namespace Allure.Commons.NUnit.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class AllureFeatureAttribute : Attribute
    {
        public string Value { get; }

        public AllureFeatureAttribute(string story)
        {
            Value = story;
        }
    }
}