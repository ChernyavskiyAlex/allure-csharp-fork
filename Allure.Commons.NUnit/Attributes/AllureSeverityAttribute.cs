using System;

namespace Allure.Commons.NUnit.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class AllureSeverityAttribute : Attribute
    {
        public  string Value { get; }

        public AllureSeverityAttribute(AllureSeverity severity)
        {
            Value = severity.ToString().ToLower();
        }
    }
}