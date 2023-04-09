using GeneralTriggerKey.KeyMap;
using IdGen;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace GeneralTriggerKey.Group
{
    internal class EnumGroup : KeyGroupUnit, IEnumGroup
    {
        public Type Type { get; private set; }

        public Dictionary<long, IKey> RelateKeys { get; private set; } = new Dictionary<long, IKey>();

        public string? GroupAlia { get; private set; }

        public EnumGroup(long id, Type enumType, string? aliaName = null)
            : base(id, MapKeyType.Group, enumType.FullName)
        {
            Type = enumType;
            GroupAlia = aliaName;
        }

        public override string ToString()
        {

            return $"[EnumGroup]({Id})<{Name}>";
        }
    }
}
