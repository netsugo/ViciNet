using System;

namespace ViciNet.RequestAttribute
{
    public static class EnumExtension
    {
        private static string GetEnumName<T>(T enumValue) where T : Enum
        {
            var field = typeof(T).GetField(enumValue.ToString());
            var attrType = typeof(RequestNameAttribute);
            var attribute = Attribute.GetCustomAttribute(field, attrType);
            return (attribute as RequestNameAttribute)?.Name;
        }

        public static string GetName(this Command enumValue)
        {
            return GetEnumName(enumValue);
        }

        public static string GetName(this StreamEvent enumValue)
        {
            return GetEnumName(enumValue);
        }
    }
}