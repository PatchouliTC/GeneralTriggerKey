using System;
using System.Collections.Generic;
using System.Reflection;

namespace GeneralTriggerKey.Utils.Extensions
{
    public class ClassWithAttributes<T>
         where T : Attribute
    {
        public List<T> Attribute { get; set; } = null!;
        public Type ClassInfo { get; set; } = null!;
    }

    public class ClassWithAttribute<T>
     where T : Attribute
    {
        public T Attribute { get; set; } = null!;
        public Type ClassInfo { get; set; } = null!;
    }

    public static class GetAllClassTypeExtension
    {
        /// <summary>
        /// 获取该程序集中携带指定特性标签的所有类的类型列表
        /// <para>标签数量存在重复可能时</para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="assembly"></param>
        /// <param name="targetAttribute"></param>
        /// <returns></returns>

        public static List<ClassWithAttributes<T>>? GetClassWithAttributesType<T>(this Assembly assembly)
            where T : Attribute
        {
            List<ClassWithAttributes<T>> _list = new List<ClassWithAttributes<T>>();

            foreach (var type in assembly.GetTypes())
            {
                object[] obj = type.GetCustomAttributes(typeof(T), false);
                if (obj.Length == 0)
                    continue;

                var _attribute_list = new List<T>();
                foreach (var _attribute in obj)
                {
                    if (_attribute is T)
                        _attribute_list.Add((T)_attribute);
                }

                _list.Add(new ClassWithAttributes<T>
                {
                    Attribute = _attribute_list,
                    ClassInfo = type
                });

            }
            return _list.Count > 0 ? _list : null;
        }

        /// <summary>
        /// 获取该程序集中携带指定特性标签的所有类的类型列表
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="assembly"></param>
        /// <returns></returns>
        public static List<ClassWithAttribute<T>>? GetClassWithAttributeType<T>(this Assembly assembly)
            where T : Attribute
        {
            List<ClassWithAttribute<T>> _list = new List<ClassWithAttribute<T>>();

            foreach (var type in assembly.GetTypes())
            {
                object[] obj = type.GetCustomAttributes(typeof(T), false);
                if (obj.Length == 0)
                    continue;

                _list.Add(new ClassWithAttribute<T>
                {
                    Attribute = (T)obj[0],
                    ClassInfo = type
                });

            }
            return _list.Count > 0 ? _list : null;
        }
    }
}
