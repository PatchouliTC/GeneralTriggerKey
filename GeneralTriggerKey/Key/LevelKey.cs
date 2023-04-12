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

        public string DisplayName {get; private set;}

        private int StartLevel;

        public LevelKey(long id, string name, int depth, long[] key_list)
            : base(id, MapKeyType.LEVEL, name)
        {
            MaxDepth = depth;
            KeySequence = key_list;

            var _singlenamebuilder = new StringBuilder();

            for(int i=0;i<KeySequence.Length;i++)
            {
                KeyMapStorage.Instance.Keys.TryGetValue(KeySequence[i], out var _key);
                if(_key is IBridgeKey _bkey)
                {
                    if (i == 0)
                    {
                        StartLevel = _bkey.JumpLevel;
                        _singlenamebuilder.Append($"({_bkey.Current.DisplayName})");
                    }
                    if (i == KeySequence.Length-1) MaxDepth=_bkey.JumpLevel+ 1;
                    _singlenamebuilder.Append($"/({_bkey.Next.DisplayName})");
                }
            }
            DisplayName= _singlenamebuilder.ToString();
        }

        public override string ToGraphvizNodeString()
        {
            return $"{Id} [label=\"[L]<{StartLevel}-{MaxDepth}>{DisplayName}\"];";
        }

        public override string ToString()
        {
            return ToString(0);
        }

        public string ToString(int retraction = 0)
        {
            var _next_retraction = retraction + 2;
            var _prefix = new String(' ', retraction);

            var _str_builder = new StringBuilder($"{_prefix}[LevelKey]({Id})<{DisplayName}>|{StartLevel}>{MaxDepth}|\n");

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
