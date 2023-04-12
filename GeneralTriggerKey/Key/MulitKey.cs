using GeneralTriggerKey.KeyMap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GeneralTriggerKey.Key
{
    /// <summary>
    /// 组合型key
    /// </summary>
    internal sealed class MulitKey : SimpleKeyNode, IMultiKey
    {
        public bool IsMultiKey => true;
        public HashSet<long> RelateSingleKeys { get; private set; }
        public string DisplayName { get; private set; }
        public long[] MultiKeys { get; private set; }

        #region Search Cache
        private HashSet<long> _containSearchCache = new HashSet<long>();
        private HashSet<long> _checkedContainKeys = new HashSet<long>();

        private HashSet<long> _supersetofSearchCache = new HashSet<long>();
        private HashSet<long> _checkedSupersetofKeys = new HashSet<long>();

        private HashSet<long> _overlapsSearchCache = new HashSet<long>();
        private HashSet<long> _checkedOverlapsKeys = new HashSet<long>();
        #endregion
        public MulitKey(long id, MapKeyType keyType, HashSet<long> relatesinglekeys, string name, IEnumerable<long> relateKeys, HashSet<string>? alias = null)
            : base(id, keyType, name)
        {
            RelateSingleKeys = relatesinglekeys ?? new HashSet<long>();
            CanTriggerNode.Add(Id);

            var nameBuilder = new StringBuilder();
            if (KeyRelateType == MapKeyType.AND)
            {
                foreach (var _id in relateKeys)
                {
                    KeyMapStorage.Instance.Keys.TryGetValue(_id, out var key);
                    nameBuilder.Append($"{key.DisplayName}&");
                }
            }
            else if (KeyRelateType == MapKeyType.OR)
            {
                //Or关系,可能存在multikey
                foreach (var relateId in relateKeys)
                {
                    KeyMapStorage.Instance.Keys.TryGetValue(relateId, out var relateKeyInst);
                    if (relateKeyInst.IsMultiKey)
                    {
                        nameBuilder.Append("(");
                        foreach (var __id in (relateKeyInst as IMultiKey)!.MultiKeys)
                        {
                            KeyMapStorage.Instance.Keys.TryGetValue(__id, out var __key);
                            nameBuilder.Append($"{__key.DisplayName}&");
                        }
                        nameBuilder.Remove(nameBuilder.Length - 1, 1);
                        nameBuilder.Append(")|");
                    }
                    else
                    {
                        nameBuilder.Append($"{relateKeyInst.DisplayName}|");
                    }
                }
            }
            nameBuilder.Length -= 1;
            DisplayName = nameBuilder.ToString();
            MultiKeys = relateKeys.ToArray();
        }

        public bool Contains(long id)
        {
            if (_containSearchCache.Contains(id))
                return true;
            if (_checkedContainKeys.Contains(id))
                return false;
            _checkedContainKeys.Add(id);
            if (RelateSingleKeys.Contains(id))
            {
                _containSearchCache.Add(id);
                return true;
            }

            if (KeyRelateType == MapKeyType.OR)
            {
                foreach (var key in DAGParentKeys)
                {
                    if (key.KeyRelateType == MapKeyType.NONE)
                        continue;
                    var multiKey=key as IMultiKey;
                    if (multiKey!.Contains(id))
                    {
                        _containSearchCache.Add(id);
                        return true;
                    }
                }
            }
            else if (KeyRelateType == MapKeyType.AND)
            {
                foreach (var key in DAGChildKeys)
                {
                    if (key.KeyRelateType == MapKeyType.NONE)
                        continue;
                    var multiKey = key as IMultiKey;
                    if (multiKey!.Contains(id))
                    {
                        _containSearchCache.Add(id);
                        return true;
                    }
                }
            }

            return false;
        }
        public bool IsRSupersetOf(IMultiKey key)
        {
            if (_supersetofSearchCache.Contains(key.Id))
                return true;
            if (_checkedSupersetofKeys.Contains(key.Id))
                return false;
            _checkedSupersetofKeys.Add(key.Id);
            if (RelateSingleKeys.IsSupersetOf(key.RelateSingleKeys))
            {
                _supersetofSearchCache.Add(key.Id);
                return true;
            }
            return false;
        }

        public bool ROverlaps(IMultiKey key)
        {
            if (_overlapsSearchCache.Contains(key.Id))
                return true;
            if (_checkedOverlapsKeys.Contains(key.Id))
                return false;
            _checkedOverlapsKeys.Add(key.Id);
            if (RelateSingleKeys.Overlaps(key.RelateSingleKeys))
            {
                _overlapsSearchCache.Add(key.Id);
                return true;
            }
            return false;
        }

        public override string ToGraphvizNodeString()
        {
            return $"{Id} [label=\"[M]{DisplayName}\"];";
        }

        public override string ToString()
        {
            return ToString(0);
        }

        public string ToString(int retraction = 0)
        {
            var nextRetraction = retraction + 2;
            var prefix = new String(' ', retraction);

            var strBuilder = new StringBuilder($"{prefix}[Multikey]({Id})<{DisplayName}>\n");

            if (KeyRelateType == MapKeyType.AND)
            {
                foreach (var child in DAGChildKeys)
                {
                    if (child.KeyRelateType == MapKeyType.OR)
                        continue;
                    if (child is MulitKey multiKey)
                        strBuilder.Append($"{multiKey.ToString(nextRetraction)}\n");
                    else
                        strBuilder.Append($"{prefix}  {child}\n");
                }
            }
            else if (KeyRelateType == MapKeyType.OR)
            {
                foreach (var child in DAGParentKeys)
                {
                    if (child is MulitKey multiKey)
                        strBuilder.Append($"{multiKey.ToString(nextRetraction)}\n");
                    else
                        strBuilder.Append($"{prefix}  {child}\n");
                }
            }
                strBuilder.Length -= 1;
            return strBuilder.ToString();
        }
    }
}
