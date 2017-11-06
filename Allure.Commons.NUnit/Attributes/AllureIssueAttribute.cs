using System;

namespace Allure.Commons.NUnit.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class AllureIssueAttribute : Attribute
    {
        public Link Link { get; }
        public AllureIssueAttribute(string name, string url)
        {
            Link = new Link {name = name, type = "issue", url = url};
        }
    }
}