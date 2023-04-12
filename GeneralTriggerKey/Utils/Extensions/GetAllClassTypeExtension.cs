using System;
using System.Collections.Generic;
using System.Reflection;

namespace GeneralTriggerKey.Utils.Extensions
{
    internal class ClassWithAttributes<T>
         where T : Attribute
    {
        public List<T> Attribute { get; set; } = null!;
        public Type ClassInfo { get; set; } = null!;
    }

    internal class ClassWithAttribute<T>
     where T : Attribute
    {
        public T Attribute { get; set; } = null!;
        public Type ClassInfo { get; set; } = null!;
    }

    internal static class GetAllClassTypeExtension
    {
        /// <summary>
        /// 获取该程序集中携带指定特性标签的所有类的类型列表
        /// <para>标签数量存在重复可能时</para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="assembly"></param>
        /// <returns></returns>

        public static List<ClassWithAttributes<T>>? GetClassWithAttributesType<T>(this Assembly assembly)
            where T : Attribute
        {
            List<ClassWithAttributes<T>> resultList = new List<ClassWithAttributes<T>>();

            foreach (var type in assembly.GetTypes())
            {
                object[] obj = type.GetCustomAttributes(typeof(T), false);
                if (obj.Length == 0)
                    continue;

                var attributeList = new List<T>();
                foreach (var attribute in obj)
                {
                    if (attribute is T)
                        attributeList.Add((T)attribute);
                }

                resultList.Add(new ClassWithAttributes<T>
                {
                    Attribute = attributeList,
                    ClassInfo = type
                });

            }
            return resultList.Count > 0 ? resultList : null;
        }

        /// <summary>
        /// 获取该程序集中携带指定特性标签的所有类的类型列表
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="assembly"></param>
        /// <returns></returns>
        internal static List<ClassWithAttribute<T>>? GetClassWithAttributeType<T>(this Assembly assembly)
            where T : Attribute
        {
            List<ClassWithAttribute<T>> resultList = new List<ClassWithAttribute<T>>();

            foreach (var type in assembly.GetTypes())
            {
                object[] obj = type.GetCustomAttributes(typeof(T), false);
                if (obj.Length == 0)
                    continue;

                resultList.Add(new ClassWithAttribute<T>
                {
                    Attribute = (T)obj[0],
                    ClassInfo = type
                });

            }
            return resultList.Count > 0 ? resultList : null;
        }
    }
}
