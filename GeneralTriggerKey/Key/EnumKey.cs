﻿using GeneralTriggerKey.KeyMap;
using System.Collections.Generic;
using System.Linq;

namespace GeneralTriggerKey.Key
{
    /// <summary>
    /// 对应枚举的key[单项key]
    /// </summary>
    public class EnumKey : KeyGroupUnit, IEnumKey
    {
        public IEnumGroup BelongEnumGroup { get; private set; }

        public long OriginId { get; private set; }

        public string OriginName
        {
            get
            {
                return Name!.Split('-').Last();
            }
        }

        public bool IsMultiKey => false;

        public bool HasAlias
        {
            get { return !(this.Alias is null || this.Alias.Count == 0); }
        }

        public HashSet<string> Alias { get; private set; } = new HashSet<string>();

        public string DisplayName { get; private set; }

        /// <summary>
        /// 单key初始化
        /// </summary>
        public EnumKey(long id, long originId, IEnumGroup belongGroup, string name)
            : base(id, MapKeyType.None, name)
        {
            OriginId = originId;
            BelongEnumGroup = belongGroup;

            DisplayName = Name!.Split('-').Last();
        }

        public override string ToGraphvizNodeString()
        {
            return $"{Id} [label=\"[E]{DisplayName}\"];";
        }

        public override string ToString()
        {
            return $"[EnumKey]({Id})<{DisplayName}>-OId:{OriginId}=>Belong:{BelongEnumGroup}";
        }
    }
}