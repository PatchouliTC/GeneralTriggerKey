﻿using GeneralTriggerKey.KeyMap;
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

        public int EndLevel { get; private set; }
        public int StartLevel { get; private set; } 
        public bool IsMultiKey => true;
        public string DisplayName {get; private set;}

        public LevelKey(long id, string name,long[] bridgeKeySequence)
            : base(id, MapKeyType.LEVEL, name)
        {
            KeySequence = bridgeKeySequence;

            var nameBuilder = new StringBuilder();
            for(int i=0;i<KeySequence.Length;i++)
            {
                KeyMapStorage.Instance.Keys.TryGetValue(KeySequence[i], out var seqKey);
                if (seqKey is IBridgeKey bridgeKey)
                {
                    if (i == 0)
                    {
                        StartLevel = bridgeKey.JumpLevel;
                        if(bridgeKey.Current.IsMultiKey) nameBuilder.Append($"({bridgeKey.Current.DisplayName})");
                        else nameBuilder.Append($"{bridgeKey.Current.DisplayName}");
                    }
                    if (i == KeySequence.Length - 1) EndLevel = bridgeKey.JumpLevel + 1;

                    if (bridgeKey.Next.IsMultiKey) nameBuilder.Append($"/({bridgeKey.Next.DisplayName})");
                    else nameBuilder.Append($"/{bridgeKey.Next.DisplayName}");
                }
                else throw new ArgumentException(message:$"Level key relate sequence exist non bridge key {seqKey}");
            }
            DisplayName= nameBuilder.ToString();
        }

        public override string ToGraphvizNodeString()
        {
            return $"{Id} [label=\"[L]<{StartLevel}-{EndLevel}>{DisplayName}\"];";
        }

        public override string ToString()
        {
            return ToString(0);
        }

        public string ToString(int retraction = 0)
        {
            var nextRetraction = retraction + 2;
            var prefix = new String(' ', retraction);

            var strBuilder = new StringBuilder($"{prefix}[LevelKey]({Id})<{DisplayName}>|{StartLevel}>{EndLevel}|\n");

            foreach (var data in DAGChildKeys)
            {
                if (data is LevelKey _bkey)
                    strBuilder.Append($"{_bkey.ToString(nextRetraction)}\n");
                else
                    strBuilder.Append($"{prefix}  {data}\n");
            }
            strBuilder.Length -= 1;
            return strBuilder.ToString();
        }
    }
}
