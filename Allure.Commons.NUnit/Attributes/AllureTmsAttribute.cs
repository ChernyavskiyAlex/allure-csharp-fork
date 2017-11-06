using System;

namespace Allure.Commons.NUnit.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class AllureTmsAttribute : Attribute
    {
        public Link Link { get; }
        public AllureTmsAttribute(string name, string url)
        {
            Link = new Link {name = name, type = "tms", url = url};
        }
    }
}