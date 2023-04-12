using GeneralTriggerKey.KeyMap;
using IdGen;
using System;
using System.Collections.Generic;
using System.Text;

namespace GeneralTriggerKey.Key
{
    /// <summary>
    /// 桥接Key
    /// </summary>
    internal sealed class BridgeKey : KeyGroupUnit, IBridgeKey
    {
        public int JumpLevel { get; private set; } = 0;

        public ISimpleNode Current { get; private set; }

        public ISimpleNode Next { get; private set; }

        public bool IsMultiKey => false;

        public string DisplayName => $"{Current.DisplayName}/{Next.DisplayName}";

        public BridgeKey(long id, string name, int level, ISimpleNode left, ISimpleNode right)
            : base(id, MapKeyType.BRIDGE, name)
        {
            JumpLevel = level;
            Current = left;
            Next = right;
        }

        public override string ToGraphvizNodeString()
        {
            return $"{Id} [label=\"[B]<{JumpLevel}/{JumpLevel + 1}>{DisplayName}\"];";
        }

        public override string ToString()
        {
            return ToString(0);
        }

        public string ToString(int retraction = 0)
        {
            var nextRetraction = retraction + 2;
            var prefix = new String(' ', retraction);

            var strBuilder = new StringBuilder($"{prefix}[BridgeKey]({Id})<{DisplayName}>|{JumpLevel}/{JumpLevel + 1}|\n");

            foreach (var data in DAGChildKeys)
            {
                if (data is BridgeKey _bkey)
                    strBuilder.Append($"{_bkey.ToString(nextRetraction)}\n");
                else
                    strBuilder.Append($"{prefix}  {data}\n");
            }
            strBuilder.Length -= 1;
            return strBuilder.ToString();
        }
    }
}
