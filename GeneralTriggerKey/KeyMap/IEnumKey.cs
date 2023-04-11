using System;
using System.Collections.Generic;
using System.Text;

namespace GeneralTriggerKey.KeyMap
{
    internal interface IEnumKey : ISimpleNode
    {
        /// <summary>
        /// 对应key的原始ID
        /// </summary>
        public long OriginId { get; }

        public string OriginName { get; }
        /// <summary>
        /// 隶属枚举组
        /// </summary>
        public IEnumGroup BelongEnumGroup { get; }
        public bool HasAlias { get; }
        public HashSet<string>? Alias { get; }

    }
}
