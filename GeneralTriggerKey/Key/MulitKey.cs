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

        public Dictionary<long, IKey> ChildKeys { get; private set; }
        public List<IMultiKey> ParentKeys { get; private set; }
        public HashSet<long> RelateSingleKeys { get; private set; }
        public string DisplayName { get; private set; }
        public long[] MultiKeys { get; private set; }
        public bool HasOtherMultiKey => ChildKeys.Values.Any(x => x.IsMultiKey);

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
            ChildKeys = new Dictionary<long, IKey>();
            ParentKeys = new List<IMultiKey>();
            RelateSingleKeys = relatesinglekeys ?? new HashSet<long>();

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
            //检查子集和全展开后的单例键是否存在
            if (ChildKeys.ContainsKey(id) || RelateSingleKeys.Contains(id))
            {
                _containSearchCache.Add(id);
                return true;
            }

            //不存在则检查所有复合键类型的子集
            foreach (var child in ChildKeys.Values.OfType<IMultiKey>())
            {
                //递归遍历深层级
                if (child.Contains(id))
                {
                    _containSearchCache.Add(id);
                    return true;
                }
            }
            //如果当前项是OR,需要将singlekey中是AND关系的联合键取出并进行检查包含性
            if (KeyRelateType == MapKeyType.OR)
            {
                foreach (var singleKey in RelateSingleKeys)
                {
                    KeyMapStorage.Instance.Keys.TryGetValue(singleKey, out var singleKeyInst);
                    if (singleKeyInst is IMultiKey factMultiKeyInst)
                    {
                        if (factMultiKeyInst.Contains(id))
                        {
                            _containSearchCache.Add(id);
                            return true;
                        }
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

            foreach (var data in ChildKeys)
            {
                if (data.Value is MulitKey)
                    strBuilder.Append($"{(data.Value as MulitKey)!.ToString(nextRetraction)}\n");
                else
                    strBuilder.Append($"{prefix}  {data.Value}\n");
            }
            strBuilder.Length -= 1;
            return strBuilder.ToString();
        }
    }
}
