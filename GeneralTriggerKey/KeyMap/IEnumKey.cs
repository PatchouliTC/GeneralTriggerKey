using System;
using System.Collections.Generic;
using System.Text;

namespace GeneralTriggerKey.KeyMap
{
    /// <summary>
    /// 枚举键
    /// </summary>
    public interface IEnumKey : IKey
    {
        /// <summary>
        /// 对应key的原始ID
        /// </summary>
        public long OriginId { get; }
        /// <summary>
        /// 对应Key原始枚举键的键名称
        /// </summary>
        public string OriginName { get; }
        /// <summary>
        /// 隶属枚举组
        /// </summary>
        public IEnumGroup BelongEnumGroup { get; }
    }
}
