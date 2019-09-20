using System;

namespace ViciNet.RequestAttribute
{
    [AttributeUsage(AttributeTargets.Field)]
    public class RequestNameAttribute : Attribute
    {
        public string Name { get; }

        public RequestNameAttribute(string name)
        {
            Name = name;
        }
    }
}