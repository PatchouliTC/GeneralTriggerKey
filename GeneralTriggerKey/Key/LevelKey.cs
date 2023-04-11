using GeneralTriggerKey.KeyMap;
using System;
using System.Collections.Generic;
using System.Text;

namespace GeneralTriggerKey.Key
{
    /// <summary>
    /// 层级Key
    /// </summary>
    internal sealed class LevelKey : KeyGroupUnit, ILevelKey
    {
        public long[] KeySequence { get; private set; }

        public int MaxDepth { get; private set; }

        public bool IsMultiKey => true;

        public string DisplayName => $"";

        public LevelKey(long id, string name, int depth, long[] key_list)
            : base(id, MapKeyType.LEVEL, name)
        {
            MaxDepth = depth;
            KeySequence = key_list;
        }
    }
}
