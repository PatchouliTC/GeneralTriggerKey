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
    public class MulitKey : KeyGroupUnit, IMultiKey
    {
        public bool IsMultiKey => true;
        public Dictionary<long, IKey> ChildKeys { get; private set; }
        public HashSet<string>? Alias { get; private set; }
        public List<IMultiKey> ParentKeys { get; private set; }
        public HashSet<long> RelateSingleKeys { get; private set; }
        public string DisplayName { get; private set; }
        public long[] MultiKeys { get; private set; }

        public bool HasAlias => !(this.Alias is null || this.Alias.Count == 0);
        public bool HasOtherMultiKey => ChildKeys.Values.Any(x => x.IsMultiKey);

        #region Search Cache
        private HashSet<long> _contain_search_cache = new HashSet<long>();
        private HashSet<long> _checked_contain_keys = new HashSet<long>();

        private HashSet<long> _supersetof_search_cache = new HashSet<long>();
        private HashSet<long> _checked_supersetof_keys = new HashSet<long>();

        private HashSet<long> _overlaps_search_cache = new HashSet<long>();
        private HashSet<long> _checked_overlaps_keys = new HashSet<long>();
        #endregion
        public MulitKey(long id, MapKeyType keyType, HashSet<long> relatesinglekeys, string name, IEnumerable<long> _relate_keys, HashSet<string>? alias = null)
            : base(id, keyType, name)
        {
            ChildKeys = new Dictionary<long, IKey>();
            ParentKeys = new List<IMultiKey>();
            RelateSingleKeys = relatesinglekeys ?? new HashSet<long>();
            Alias = alias;

            //concat self displayname
            var _singlenamebuilder = new StringBuilder();
            if (KeyRelateType == MapKeyType.AND)
            {
                foreach (var _id in _relate_keys)
                {
                    KeyMapStorage.Instance.Keys.TryGetValue(_id, out var key);
                    _singlenamebuilder.Append($"{key.DisplayName}&");
                }
            }
            else if (KeyRelateType == MapKeyType.OR)
            {
                //Or关系,可能存在multikey
                foreach (var _id in _relate_keys)
                {
                    KeyMapStorage.Instance.Keys.TryGetValue(_id, out var _key);
                    if (_key.IsMultiKey)
                    {
                        _singlenamebuilder.Append("(");
                        foreach (var __id in (_key as IMultiKey)!.MultiKeys)
                        {
                            KeyMapStorage.Instance.Keys.TryGetValue(__id, out var __key);
                            _singlenamebuilder.Append($"{__key.DisplayName}&");
                        }
                        _singlenamebuilder.Remove(_singlenamebuilder.Length - 1, 1);
                        _singlenamebuilder.Append(")|");
                    }
                    else
                    {
                        _singlenamebuilder.Append($"{_key.DisplayName}|");
                    }
                }
            }
            _singlenamebuilder.Length -= 1;
            DisplayName = _singlenamebuilder.ToString();
            MultiKeys = _relate_keys.ToArray();
        }

        public bool Contains(long id)
        {
            if (_contain_search_cache.Contains(id))
                return true;
            if (_checked_contain_keys.Contains(id))
                return false;
            _checked_contain_keys.Add(id);

            if (ChildKeys.ContainsKey(id) || RelateSingleKeys.Contains(id))
            {
                _contain_search_cache.Add(id);
                return true;
            }

            foreach (var child in ChildKeys.Values.Where(x => x.IsMultiKey))
            {
                var _temp = child as IMultiKey;
                if (_temp!.Contains(id))
                {
                    _contain_search_cache.Add(id);
                    return true;
                }

            }
            if (KeyRelateType == MapKeyType.OR)
            {
                foreach (var _single_key in RelateSingleKeys)
                {
                    KeyMapStorage.Instance.Keys.TryGetValue(_single_key, out var _key);
                    if (_key.IsMultiKey)
                    {
                        var _temp = _key as IMultiKey;
                        if (_temp!.Contains(id))
                        {
                            _contain_search_cache.Add(id);
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public bool IsRSupersetOf(IMultiKey key)
        {
            if (_supersetof_search_cache.Contains(key.Id))
                return true;
            if (_checked_supersetof_keys.Contains(key.Id))
                return false;
            _checked_supersetof_keys.Add(key.Id);
            if (RelateSingleKeys.IsSupersetOf(key.RelateSingleKeys))
            {
                _supersetof_search_cache.Add(key.Id);
                return true;
            }
            return false;
        }

        public bool ROverlaps(IMultiKey key)
        {
            if (_overlaps_search_cache.Contains(key.Id))
                return true;
            if (_checked_overlaps_keys.Contains(key.Id))
                return false;
            _checked_overlaps_keys.Add(key.Id);
            if (RelateSingleKeys.Overlaps(key.RelateSingleKeys))
            {
                _overlaps_search_cache.Add(key.Id);
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
            var _next_retraction = retraction + 2;
            var _prefix = new String(' ', retraction);

            var _str_builder = new StringBuilder($"{_prefix}[Multikey]({Id})<{DisplayName}>{{{Name}}}\n");

            foreach (var data in ChildKeys)
            {
                if (data.Value is MulitKey _m_key)
                    _str_builder.Append($"{_m_key.ToString(_next_retraction)}\n");
                else
                    _str_builder.Append($"{_prefix}  {data.Value}\n");
            }
            _str_builder.Length -= 1;
            return _str_builder.ToString();
        }
    }
}
