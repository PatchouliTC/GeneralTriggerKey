using System;
using System.Collections.Generic;
using System.Text;

namespace GeneralTriggerKey.Attributes
{
    /// <summary>
    /// 需要被发现的枚举表
    /// </summary>
    [AttributeUsage(AttributeTargets.Enum, AllowMultiple = true, Inherited = false)]
    public class MapEnumAttribute : Attribute
    {

    }

    /// <summary>
    /// 枚举项别名
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class EnumAliaAttribute : Attribute
    {
        public string[] Names;
        public EnumAliaAttribute(params string[] names)
        {
            Names = names;
        }
    }
}
