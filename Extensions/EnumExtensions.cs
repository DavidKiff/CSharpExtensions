using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Extensions
{
    public static class EnumExtensions
    {
        public static T GetAttribute<T>(this Enum enumeration) where T : Attribute
        {
            var enumType = enumeration.GetType();

            return (T) Attribute.GetCustomAttribute(enumType.GetField(Enum.GetName(enumType, enumeration)), typeof(T));
        }

        public static IEnumerable<T> GetAttributes<T>(this Enum enumeration) where T : Attribute
        {
            var enumType = enumeration.GetType();

            return Attribute.GetCustomAttributes(enumType.GetField(Enum.GetName(enumType, enumeration)), typeof(T)).Cast<T>();
        }

        public static string GetDescription(this Enum enumeration)
        {
            return enumeration.GetAttribute<DescriptionAttribute>()?.Description;
        }
    }
}
