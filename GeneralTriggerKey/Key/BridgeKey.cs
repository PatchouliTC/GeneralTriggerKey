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
            : base(id, MapKeyType.Bridge, name)
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
            var _next_retraction = retraction + 2;
            var _prefix = new String(' ', retraction);

            var _str_builder = new StringBuilder($"{_prefix}[BridgeKey]({Id})<{DisplayName}>|{JumpLevel}/{JumpLevel + 1}|\n");

            foreach (var data in DAGChildKeys)
            {
                if (data is BridgeKey _bkey)
                    _str_builder.Append($"{_bkey.ToString(_next_retraction)}\n");
                else
                    _str_builder.Append($"{_prefix}  {data}\n");
            }
            _str_builder.Length -= 1;
            return _str_builder.ToString();
        }
    }
}
