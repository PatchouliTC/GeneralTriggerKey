using GeneralTriggerKey.KeyMap;
using GeneralTriggerKey.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GeneralTriggerKey
{
    /// <summary>
    /// 延时执行GeneralKey
    /// </summary>
    public readonly struct LazyKey
    {
        /// <summary>
        /// 当前节点组类型
        /// </summary>
        public readonly MapKeyType KeyType;

        public readonly List<long> CacheKeyIds;

        public readonly int Depth;
        public LazyKey(MapKeyType keyType, int depth = 0, params long[] keys)
        {
            KeyType = keyType;
            CacheKeyIds = new List<long>(keys);
            Depth = depth;
        }

        public LazyKey(MapKeyType keyType, List<long> keys, int depth = 0)
        {
            KeyType = keyType;
            CacheKeyIds = keys;
            Depth = depth;
        }

        public static implicit operator GeneralKey(LazyKey d)
        {
            return d.Compile();
        }

        public static LazyKey operator &(LazyKey left, LazyKey right)
        {
            if (!(left.KeyType == MapKeyType.AND || right.KeyType == MapKeyType.None || left.KeyType == MapKeyType.OR) ||
                !(right.KeyType == MapKeyType.AND || right.KeyType == MapKeyType.None || right.KeyType == MapKeyType.OR)
                )
                throw new ArgumentException(message: "Not Support or/and with non or/and");

            //and & and/single
            if ((left.KeyType == MapKeyType.AND || right.KeyType == MapKeyType.None) && (right.KeyType == MapKeyType.AND || right.KeyType == MapKeyType.None))
            {
                List<long> _temp = new List<long>();
                _temp.AddRange(left.CacheKeyIds);
                _temp.AddRange(right.CacheKeyIds);
                return new LazyKey(MapKeyType.AND, _temp);
            }
            else if (left.KeyType == MapKeyType.OR && right.KeyType == MapKeyType.OR)
            {
                var key1 = left.Compile();
                var key2 = right.Compile();
                return new LazyKey(MapKeyType.AND, 0, key1.Id, key2.Id);
            }
            else if (left.KeyType == MapKeyType.OR || right.KeyType == MapKeyType.OR)
            {
                var _or_key = left.KeyType == MapKeyType.OR ? left : right;

                var _and_key = left.KeyType == MapKeyType.OR ? right : left;


                var _key = _or_key.Compile();
                var _cache = new List<long>() { _key.Id };
                _cache.AddRange(_and_key.CacheKeyIds);
                return new LazyKey(MapKeyType.AND, _cache);
            }
            throw new ArgumentException(message: "Not Support or/and with non or/and");
        }

        public static LazyKey operator |(LazyKey left, LazyKey right)
        {
            if (!(left.KeyType == MapKeyType.AND || right.KeyType == MapKeyType.None || left.KeyType == MapKeyType.OR) ||
                !(right.KeyType == MapKeyType.AND || right.KeyType == MapKeyType.None || right.KeyType == MapKeyType.OR)
            )
                throw new ArgumentException(message: "Not Support or/and with non or/and");

            if (left.KeyType == MapKeyType.OR && right.KeyType == MapKeyType.OR)
            {
                List<long> _temp = new List<long>();
                _temp.AddRange(left.CacheKeyIds);
                _temp.AddRange(right.CacheKeyIds);
                return new LazyKey(MapKeyType.OR, _temp);
            }
            else if ((left.KeyType == MapKeyType.AND || right.KeyType == MapKeyType.None) && (right.KeyType == MapKeyType.AND || right.KeyType == MapKeyType.None))
            {
                var key1 = left.Compile();
                var key2 = right.Compile();
                return new LazyKey(MapKeyType.OR, 0, key1.Id, key2.Id);
            }
            else if (left.KeyType == MapKeyType.OR || right.KeyType == MapKeyType.OR)
            {
                var _or_key = left.KeyType == MapKeyType.OR ? left : right;

                var _and_key = left.KeyType == MapKeyType.OR ? right : left;


                var _key = _and_key.Compile();
                var _cache = new List<long>() { _key.Id };
                _cache.AddRange(_or_key.CacheKeyIds);
                return new LazyKey(MapKeyType.OR, _cache);
            }
            throw new ArgumentException(message: "Not Support or/and with non or/and");
        }

        public GeneralKey Compile()
        {
            return Compile(this);
        }

        private static GeneralKey Compile(LazyKey key)
        {
            if (key.CacheKeyIds.Count <= 0)
                throw new ArgumentException(message: "Not allow compile for none ids");

            else if (key.CacheKeyIds.Count == 1)
            {
                if (KMStorageWrapper.TryGetKey(key.CacheKeyIds[0], out IKey _key))
                {
                    return new GeneralKey(_key.Id, _key.IsMultiKey, _key.KeyRelateType);
                }
            }
            else
            {
                if (key.KeyType == MapKeyType.OR)
                {
                    if (KMStorageWrapper.TryRegisterMultiKey(out var multi_key_runtime_id, keyType: key.KeyType, key.CacheKeyIds.ToArray()))
                    {
                        KMStorageWrapper.TryGetKey(multi_key_runtime_id, out IKey _key);
                        return new GeneralKey(_key.Id, _key.IsMultiKey, _key.KeyRelateType);
                    }
                }
                else if (key.KeyType == MapKeyType.AND)
                {
                    //检查下每个key的状态
                    var _or_relate = new List<IMultiKey>();
                    var _other_relate = new List<long>();
                    foreach (var _key in key.CacheKeyIds)
                    {
                        if (KMStorageWrapper.TryGetKey(_key, out IKey _key_inst))
                        {
                            if (_key_inst is IMultiKey _mkey && _mkey.KeyRelateType == MapKeyType.OR)
                            {
                                _or_relate.Add(_mkey);
                            }
                            else if (_key_inst.KeyRelateType != MapKeyType.LEVEL || _key_inst.KeyRelateType != MapKeyType.Bridge)
                            {
                                _other_relate.Add(_key);
                            }
                        }
                    }
                    if (_or_relate.Count > 0)
                    {
                        //做有所or关系之间外加非or关系的笛卡尔积
                        var _swap_1 = new Stack<long[]>();
                        var _swap_2 = new Stack<long[]>();

                        var _filter_set = new HashSet<long>();
                        _swap_1.Push(_other_relate.ToArray());

                        foreach (var _or_relate_key_group in _or_relate)
                        {
                            while (_swap_1.Count > 0)
                            {
                                var _last_array = _swap_1.Pop();
                                foreach (var _or_key in _or_relate_key_group.RelateSingleKeys)
                                {
                                    _filter_set.Concat(_last_array);
                                    _filter_set.Add(_or_key);
                                    _swap_2.Push(_filter_set.ToArray());
                                    _filter_set.Clear();
                                }
                            }

                            //swap stack
                            var _temp = _swap_1;
                            _swap_1 = _swap_2;
                            _swap_2 = _temp;
                        }

                        _filter_set.Clear();
                        //transform all and relate
                        while (_swap_1.Count > 0)
                        {
                            var _last_array = _swap_1.Pop();
                            if (KMStorageWrapper.TryRegisterMultiKey(out var _and_key, keyType: MapKeyType.AND, _last_array))
                                _filter_set.Add(_and_key);
                        }
                        if (KMStorageWrapper.TryRegisterMultiKey(out var multi_key_runtime_id, keyType: MapKeyType.OR, _filter_set.ToArray()))
                        {
                            KMStorageWrapper.TryGetKey(multi_key_runtime_id, out IKey _key);
                            return new GeneralKey(_key.Id, _key.IsMultiKey, _key.KeyRelateType);
                        }
                    }
                    else
                    {
                        if (KMStorageWrapper.TryRegisterMultiKey(out var multi_key_runtime_id, keyType: MapKeyType.OR, _other_relate.ToArray()))
                        {
                            KMStorageWrapper.TryGetKey(multi_key_runtime_id, out IKey _key);
                            return new GeneralKey(_key.Id, _key.IsMultiKey, _key.KeyRelateType);
                        }
                    }

                }
                else if (key.KeyType == MapKeyType.Bridge)
                {
                    if (key.CacheKeyIds.Count > 2)
                        throw new ArgumentException(message: "Not Allow create bridge key with more than 2 simple keys");
                    if (KMStorageWrapper.TryGetKey(key.CacheKeyIds[0], out ISimpleNode _skey1) && KMStorageWrapper.TryGetKey(key.CacheKeyIds[1], out ISimpleNode _skey2))
                        if (key.Depth > 0)
                            if (KeyMapStorage.Instance.TryCreateBridgeKey(out var _bkey, _skey1.Id, _skey2.Id, key.Depth))
                            {
                                KMStorageWrapper.TryGetKey(_bkey, out IKey _key);
                                return new GeneralKey(_key.Id, _key.IsMultiKey, _key.KeyRelateType);
                            }
                }
                else if (key.KeyType == MapKeyType.LEVEL)
                {
                    var _cache = new List<long>();
                    foreach (var _lbid in key.CacheKeyIds)
                    {
                        if (KMStorageWrapper.TryGetKey(key.CacheKeyIds[0], out IKey _skey1))
                        {
                            if (_skey1 is ILevelKey _lkey)
                            {
                                _cache.AddRange(_lkey.KeySequence);
                            }
                            else if (_skey1 is IBridgeKey _bbkey)
                            {
                                if (_cache.Count > 0)
                                {
                                    if (KMStorageWrapper.TryGetKey(_cache.LastOrDefault(), out IKey _last_bridge_key))
                                    {
                                        var _temp_b_key = _last_bridge_key as IBridgeKey;
                                        if (_temp_b_key!.JumpLevel != _bbkey.JumpLevel - 1 || _temp_b_key!.Next.Id != _bbkey.Current.Id)
                                            throw new ArgumentException(message: $"Not Support connect {_temp_b_key} with {_bbkey}[error level or same key]");
                                        _cache.Add(_bbkey.Id);
                                    }
                                }
                                else
                                {
                                    if (_bbkey.JumpLevel != 1)
                                        throw new ArgumentException(message: "Level key require start from level 1");
                                    _cache.Add(_bbkey.Id);
                                }
                            }
                            else
                                throw new ArgumentException(message: $"Not Support Connect non bridge key to level]");
                        }

                    }
                    if (KeyMapStorage.Instance.TryRegisterLevelKey(out var _bkey, _cache.ToArray()))
                    {
                        KMStorageWrapper.TryGetKey(_bkey, out IKey _key);
                        return new GeneralKey(_key.Id, _key.IsMultiKey, _key.KeyRelateType);
                    }
                }

            }
            throw new ArgumentException(message: $"UnKnown Lazy GeneralKey generate for {key.KeyType}({key.Depth})");
        }
    }
}
