using GeneralTriggerKey.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace GeneralTriggerKey.KeyMap
{
    /// <summary>
    /// 联合键类型
    /// </summary>
    public enum MapKeyType
    {
        /// <summary>
        /// For key,default such as single key
        /// </summary>
        NONE,
        /// <summary>
        /// 组
        /// </summary>
        GROUP,
        /// <summary>
        /// 层级键
        /// </summary>
        LEVEL,
        /// <summary>
        /// 桥接键
        /// </summary>
        BRIDGE,
        /// <summary>
        /// 与关系
        /// </summary>
        AND,
        /// <summary>
        /// 或关系
        /// </summary>
        OR,
    }

    [MapEnum]
    public enum StorageKey
    {
        [EnumAlia("任意","~")]
        ANY
    }
}
