using GeneralTriggerKey.KeyMap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GeneralTriggerKey.Utils.Extensions
{
    public static class KMCompareExtension
    {

        /// <summary>
        /// 是否包含另一个
        /// </summary>
        /// <param name="id1"></param>
        /// <param name="id2"></param>
        /// <returns></returns>
        public static bool Contains(this long id1, long id2)
        {
            if (KMStorageWrapper.TryGetKey(id1, out IKey value1) && KMStorageWrapper.TryGetKey(id2, out IKey value2))
            {
                if (value1 is IBridgeKey bkey1 && value2 is IBridgeKey bkey2)
                {
                    return (bkey1.JumpLevel == bkey2.JumpLevel)
                        && (bkey1.Current.CanTriggerNode.Contains(bkey2.Current.Id))
                        && (bkey1.Next.CanTriggerNode.Contains(bkey2.Next.Id));
                }
                else if (value1 is ILevelKey lkey1 && value2 is ILevelKey lkey2)
                {
                    return (lkey1.EndLevel >= lkey2.EndLevel)
                        && (lkey1.CanTriggerNode.Contains(lkey2.Id));
                }
                //都不是,比较相等性
                else if (!value1.IsMultiKey && !value2.IsMultiKey)
                {
                    return id1 == id2;
                }
                //都是联合键且联合模式相等,比较关联的单一键列表超集关系
                else if ((value1.IsMultiKey && value2.IsMultiKey &&
                    ((value1 as IMultiKey)!.KeyRelateType == (value2 as IMultiKey)!.KeyRelateType)
                    ))
                {
                    return (value1 as IMultiKey)!.IsRSupersetOf((value2 as IMultiKey)!);
                }
                //当前是联合键,要比较对象是单一键
                //或者两者都是联合键,但是当前是OR,要比较对象是AND
                else if ((value1.IsMultiKey && !value2.IsMultiKey) || (value1.IsMultiKey && value2.IsMultiKey &&
                    (value1 as IMultiKey)!.KeyRelateType == MapKeyType.OR &&
                     (value2 as IMultiKey)!.KeyRelateType == MapKeyType.AND))
                {
                    //调用第一个联合键的Contains方法,对其所有子集和子集的子集进行深度搜索
                    //第一次调用较慢[需要深度递归,并且作为or关系同时需要对relatesinglekeys进行检查]
                    return (value1 as IMultiKey)!.Contains(value2.Id);
                }

            }
            return false;
        }

        /// <summary>
        /// 是否有重叠键[存在交集]
        /// </summary>
        /// <param name="id1"></param>
        /// <param name="id2"></param>
        /// <returns></returns>
        public static bool Overlaps(this long id1, long id2)
        {
            if (KMStorageWrapper.TryGetKey(id1, out IKey value1) && KMStorageWrapper.TryGetKey(id2, out IKey value2))
            {
                if (value1 is IBridgeKey bkey1 && value2 is IBridgeKey bkey2)
                {
                    return id1 == id2;
                }
                else if (value1 is ILevelKey lkey1 && value2 is ILevelKey lkey2)
                {
                    return id1.Contains(id2);
                }
                else if (!value1.IsMultiKey && !value2.IsMultiKey)
                {
                    return id1 == id2;
                }
                else if ((value1.IsMultiKey && value2.IsMultiKey &&
                    ((value1 as IMultiKey)!.KeyRelateType == (value2 as IMultiKey)!.KeyRelateType)
                    ))
                {
                    return (value1 as IMultiKey)!.ROverlaps((value2 as IMultiKey)!);
                }
                else if (value1.IsMultiKey != value2.IsMultiKey)
                {
                    var _multi_key = (value1.IsMultiKey) ? (value1 as IMultiKey) : (value2 as IMultiKey);
                    var _single_key = (value1.IsMultiKey) ? value2 : value1;

                    return _multi_key!.Contains(_single_key.Id);
                }

            }
            return false;
        }

        /// <summary>
        /// 当前键能否触发后者条件
        /// </summary>
        /// <param name="current_id"></param>
        /// <param name="check_id"></param>
        /// <returns></returns>
        public static bool CanTrigger(this long current_id, long check_id, long force_contain_key_id = -1)
        {
            if (KMStorageWrapper.TryGetKey(current_id, out IKey value1) && KMStorageWrapper.TryGetKey(check_id, out IKey value2))
            {
                KMStorageWrapper.TryGetKey(force_contain_key_id, out IKey _need_contain_key);

                if (value1 is IBridgeKey bkey1 && value2 is IBridgeKey bkey2)
                {
                    return bkey1.CanTriggerNode.Contains(bkey2.Id);
                }
                else if (value1 is ILevelKey lkey1 && value2 is ILevelKey lkey2)
                {
                    return lkey1.CanTriggerNode.Contains(lkey2.Id);
                }
                //双单例键,比较值相等
                else if (!value1.IsMultiKey && !value2.IsMultiKey)
                {
                    if (_need_contain_key is null)
                        return value1.Id == value2.Id;
                    return value1.Id == value2.Id && value1.Id == _need_contain_key.Id;
                }
                //当前项是单键时,被比较项必须是or关系[and关系一定无法触发]
                else if (!value1.IsMultiKey && value2.IsMultiKey && (value2 as IMultiKey)!.KeyRelateType == MapKeyType.OR)
                {
                    if (_need_contain_key is null)
                        return value1.CanTriggerNode.Contains(value2.Id);
                    return _need_contain_key.Id == value1.Id && value1.CanTriggerNode.Contains(value2.Id);
                    //var _multi_key = (value2 as IMultiKey);
                    ////只要or中包含该基础节点即可
                    //if (_need_contain_key is null)
                    //    return _multi_key!.RelateSingleKeys.Contains(value1.Id);
                    //return _need_contain_key.Id == value1.Id && _multi_key!.RelateSingleKeys.Contains(value1.Id);
                }
                //被比较项联合键+当前被比较对象能否触发目标对象
                //当前项必须是and关系,or关系无比较意义
                else if (value1.IsMultiKey && (value1 as IMultiKey)!.KeyRelateType == MapKeyType.AND)
                {
                    var _multi_key = (value1 as IMultiKey);
                    //项2是单例键,直接看项1的singlekey是否包含即可
                    if (!value2.IsMultiKey)
                    {
                        if (_need_contain_key is null)
                            return _multi_key!.CanTriggerNode.Contains(value2.Id);
                        return value2.Id == _need_contain_key.Id && _multi_key!.CanTriggerNode.Contains(value2.Id);
                    }
                    else
                    {
                        var _require_check_multi_key = (value2 as IMultiKey);


                        if (_need_contain_key is null)
                            return _multi_key!.CanTriggerNode.Contains(_require_check_multi_key!.Id);

                        var _check_contain = _require_check_multi_key!.Contains(_need_contain_key.Id);
                        if (_check_contain)
                            return _multi_key!.CanTriggerNode.Contains(_require_check_multi_key!.Id);
                    }

                }
            }
            return false;
        }


        /// <summary>
        /// 两个集合的差集
        /// </summary>
        /// <param name="id1"></param>
        /// <param name="id2"></param>
        /// <param name="id_result"></param>
        /// <returns></returns>
        public static bool SymmetricExceptWith(this long id1, long id2, out long id_result)
        {
            if (KMStorageWrapper.TryGetKey(id1, out IKey value1) && KMStorageWrapper.TryGetKey(id2, out IKey value2))
            {
                if (value1 is IBridgeKey bkey1 && value2 is IBridgeKey bkey2)
                {
                    id_result = -1;
                    return false;
                }
                else if (value1 is ILevelKey lkey1 && value2 is ILevelKey lkey2)
                {
                    id_result = -1;
                    return false;
                }
                else if (value1.IsMultiKey && value2.IsMultiKey && (value1 as IMultiKey)!.KeyRelateType == (value2 as IMultiKey)!.KeyRelateType)
                {
                    var _temp = (value1 as IMultiKey)!.RelateSingleKeys.ToHashSet();
                    _temp.SymmetricExceptWith((value2 as IMultiKey)!.RelateSingleKeys);

                    return KMStorageWrapper.TryRegisterMultiKey(out id_result, (value1 as IMultiKey)!.KeyRelateType, _temp.ToArray());
                }
                else if ((value1.IsMultiKey && !value2.IsMultiKey) || (!value1.IsMultiKey && value2.IsMultiKey))
                {
                    var _relate_instance = (value1.IsMultiKey) ? (value1 as IMultiKey) : (value2 as IMultiKey);
                    var _single_instance = (value1.IsMultiKey) ? value2 : value1;

                    var _temp = _relate_instance!.RelateSingleKeys.ToHashSet();
                    if (_relate_instance!.RelateSingleKeys.Contains(_single_instance.Id))
                        _temp.Remove(_single_instance.Id);
                    else
                        _temp.Add(_single_instance.Id);
                    return KMStorageWrapper.TryRegisterMultiKey(out id_result, _relate_instance!.KeyRelateType, _temp.ToArray());
                }
            }
            id_result = -1;
            return false;
        }

        /// <summary>
        /// 两个集合的交集
        /// </summary>
        /// <param name="id1"></param>
        /// <param name="id2"></param>
        /// <param name="id_result"></param>
        /// <returns></returns>
        public static bool IntersectWith(this long id1, long id2, out long id_result)
        {

            if (KMStorageWrapper.TryGetKey(id1, out IKey value1) && KMStorageWrapper.TryGetKey(id2, out IKey value2))
            {
                if (value1 is IBridgeKey bkey1 && value2 is IBridgeKey bkey2)
                {
                    id_result = -1;
                    return false;
                }
                else if (value1 is ILevelKey lkey1 && value2 is ILevelKey lkey2)
                {
                    id_result = -1;
                    return false;
                }
                else if (value1.IsMultiKey && value2.IsMultiKey && (value1 as IMultiKey)!.KeyRelateType == (value2 as IMultiKey)!.KeyRelateType)
                {
                    var _temp = (value1 as IMultiKey)!.RelateSingleKeys.ToHashSet();
                    _temp.IntersectWith((value2 as IMultiKey)!.RelateSingleKeys);

                    return KMStorageWrapper.TryRegisterMultiKey(out id_result, (value1 as IMultiKey)!.KeyRelateType, _temp.ToArray());
                }
                else if ((value1.IsMultiKey && !value2.IsMultiKey) || (!value1.IsMultiKey && value2.IsMultiKey))
                {
                    var _relate_instance = (value1.IsMultiKey) ? (value1 as IMultiKey) : (value2 as IMultiKey);
                    var _single_instance = (value1.IsMultiKey) ? value2 : value1;

                    if (_relate_instance!.KeyRelateType == MapKeyType.AND)
                    {
                        if (_relate_instance!.RelateSingleKeys.Contains(_single_instance.Id))
                        {
                            id_result = _single_instance.Id;
                            return true;
                        }
                    }
                    else if (_relate_instance!.KeyRelateType == MapKeyType.OR)
                    {
                        if (_relate_instance!.Contains(_single_instance.Id))
                        {
                            id_result = _single_instance.Id;
                            return true;
                        }
                    }
                }
            }
            id_result = -1;
            return false;
        }

        /// <summary>
        /// 获取两个key的或关系集合[|]
        /// </summary>
        /// <param name="id1"></param>
        /// <param name="id_result"></param>
        /// <param name="id2"></param>
        /// <returns></returns>
        public static bool OrWith(this long id1, long id2, out long id_result)
        {
            if (KMStorageWrapper.TryGetKey(id1, out IKey value1) && KMStorageWrapper.TryGetKey(id2, out IKey value2))
            {
                return KMStorageWrapper.TryRegisterMultiKey(out id_result, MapKeyType.OR, id1, id2);
            }
            id_result = -1;
            return false;
        }

        /// <summary>
        /// 获取两个key的和关系集合[&]
        /// </summary>
        /// <param name="id1"></param>
        /// <param name="id2"></param>
        /// <returns></returns>
        public static bool AndWith(this long id1, long id2, out long id_result)
        {
            if (KMStorageWrapper.TryGetKey(id1, out IKey value1) && KMStorageWrapper.TryGetKey(id2, out IKey value2))
            {
                //二者均不为or关系
                if (
                    !(value1.IsMultiKey && (value1 as IMultiKey)!.KeyRelateType == MapKeyType.OR) &&
                    !(value2.IsMultiKey && (value2 as IMultiKey)!.KeyRelateType == MapKeyType.OR)
                    )
                {
                    return KMStorageWrapper.TryRegisterMultiKey(out id_result, MapKeyType.AND, id1, id2);
                }
                //两个为or关系
                //拆开两组单列键进行AND操作的笛卡尔积
                //最终获得的数组之间再进行or关系
                else if (
                    (value1.IsMultiKey && (value1 as IMultiKey)!.KeyRelateType == MapKeyType.OR) &&
                    (value2.IsMultiKey && (value2 as IMultiKey)!.KeyRelateType == MapKeyType.OR)
                    )
                {
                    var _single_relate_1 = (value1 as IMultiKey)!.RelateSingleKeys;
                    var _single_relate_2 = (value2 as IMultiKey)!.RelateSingleKeys;

                    var result_ids = new List<long>();
                    foreach (var key1 in _single_relate_1)
                    {
                        foreach (var key2 in _single_relate_2)
                        {
                            if (KMStorageWrapper.TryRegisterMultiKey(out var _temp_id, MapKeyType.AND, key1, key2))
                            {
                                result_ids.Add(_temp_id);
                            }
                            else
                            {
                                id_result = -1;
                                return false;
                            }
                        }
                    }

                    long _temp = 0;

                    foreach (var temp_key in result_ids)
                    {
                        if (_temp == 0)
                        {
                            _temp = temp_key;
                            continue;
                        }
                        if (KMStorageWrapper.TryRegisterMultiKey(out var _temp_id, MapKeyType.OR, _temp, temp_key))
                        {
                            _temp = _temp_id;
                        }
                        else
                        {
                            id_result = -1;
                            return false;
                        }
                    }
                    id_result = _temp;
                    return true;
                }
                //一个是or另一个单例键
                else
                {
                    var _or_relate_instance = (value1.IsMultiKey && (value1 as IMultiKey)!.KeyRelateType == MapKeyType.OR) ? (value1 as IMultiKey) : (value2 as IMultiKey);
                    var _single_instance = (value1.IsMultiKey && (value1 as IMultiKey)!.KeyRelateType == MapKeyType.OR) ? value2 : value1;

                    var _single_relate = _or_relate_instance!.RelateSingleKeys;

                    var result_ids = new List<long>();
                    foreach (var key in _single_relate)
                    {
                        if (KMStorageWrapper.TryRegisterMultiKey(out var _temp_id, MapKeyType.AND, _single_instance.Id, key))
                        {
                            result_ids.Add(_temp_id);
                        }
                        else
                        {
                            id_result = -1;
                            return false;
                        }

                    }

                    long _temp = 0;

                    foreach (var temp_key in result_ids)
                    {
                        if (_temp == 0)
                        {
                            _temp = temp_key;
                            continue;
                        }
                        if (KMStorageWrapper.TryRegisterMultiKey(out var _temp_id, MapKeyType.OR, _temp, temp_key))
                        {
                            _temp = _temp_id;
                        }
                        else
                        {
                            id_result = -1;
                            return false;
                        }
                    }
                    id_result = _temp;
                    return true;
                }

            }
            id_result = -1;
            return false;
        }

        /// <summary>
        /// 获取两个key的和关系集合[&]
        /// <para>仅当key存在时获取,如果不存在则获取失败</para>
        /// </summary>
        /// <param name="id1"></param>
        /// <param name="id2"></param>
        /// <returns></returns>
        public static bool AndWithIfExist(this long id1, long id2, out long id_result)
        {
            if (KMStorageWrapper.TryGetKey(id1, out IKey value1) && KMStorageWrapper.TryGetKey(id2, out IKey value2))
            {
                //二者均不为or关系
                if (
                    !(value1.IsMultiKey && (value1 as IMultiKey)!.KeyRelateType == MapKeyType.OR) &&
                    !(value2.IsMultiKey && (value2 as IMultiKey)!.KeyRelateType == MapKeyType.OR)
                    )
                {
                    return KMStorageWrapper.TryRegisterMultiKeyIfExist(out id_result, MapKeyType.AND, id1, id2);
                }
                //两个为or关系
                //拆开两组单列键进行AND操作的笛卡尔积
                //最终获得的数组之间再进行or关系
                else if (
                    (value1.IsMultiKey && (value1 as IMultiKey)!.KeyRelateType == MapKeyType.OR) &&
                    (value2.IsMultiKey && (value2 as IMultiKey)!.KeyRelateType == MapKeyType.OR)
                    )
                {
                    var _single_relate_1 = (value1 as IMultiKey)!.RelateSingleKeys;
                    var _single_relate_2 = (value2 as IMultiKey)!.RelateSingleKeys;

                    var result_ids = new List<long>();
                    foreach (var key1 in _single_relate_1)
                    {
                        foreach (var key2 in _single_relate_2)
                        {
                            if (KMStorageWrapper.TryRegisterMultiKeyIfExist(out var _temp_id, MapKeyType.AND, key1, key2))
                            {
                                result_ids.Add(_temp_id);
                            }
                            else
                            {
                                id_result = -1;
                                return false;
                            }
                        }
                    }

                    long _temp = 0;

                    foreach (var temp_key in result_ids)
                    {
                        if (_temp == 0)
                        {
                            _temp = temp_key;
                            continue;
                        }
                        if (KMStorageWrapper.TryRegisterMultiKeyIfExist(out var _temp_id, MapKeyType.OR, _temp, temp_key))
                        {
                            _temp = _temp_id;
                        }
                        else
                        {
                            id_result = -1;
                            return false;
                        }
                    }
                    id_result = _temp;
                    return true;
                }
                //一个是or另一个单例键
                else
                {
                    var _or_relate_instance = (value1.IsMultiKey && (value1 as IMultiKey)!.KeyRelateType == MapKeyType.OR) ? (value1 as IMultiKey) : (value2 as IMultiKey);
                    var _single_instance = (value1.IsMultiKey && (value1 as IMultiKey)!.KeyRelateType == MapKeyType.OR) ? value2 : value1;

                    var _single_relate = _or_relate_instance!.RelateSingleKeys;

                    var result_ids = new List<long>();
                    foreach (var key in _single_relate)
                    {
                        if (KMStorageWrapper.TryRegisterMultiKeyIfExist(out var _temp_id, MapKeyType.AND, _single_instance.Id, key))
                        {
                            result_ids.Add(_temp_id);
                        }
                        else
                        {
                            id_result = -1;
                            return false;
                        }

                    }

                    long _temp = 0;

                    foreach (var temp_key in result_ids)
                    {
                        if (_temp == 0)
                        {
                            _temp = temp_key;
                            continue;
                        }
                        if (KMStorageWrapper.TryRegisterMultiKeyIfExist(out var _temp_id, MapKeyType.OR, _temp, temp_key))
                        {
                            _temp = _temp_id;
                        }
                        else
                        {
                            id_result = -1;
                            return false;
                        }
                    }
                    id_result = _temp;
                    return true;
                }

            }
            id_result = -1;
            return false;
        }

        /// <summary>
        /// 获取两个桥接key的层级key
        /// </summary>
        /// <param name=""></param>
        /// <param name="id2"></param>
        /// <param name="id_result"></param>
        /// <returns></returns>
        public static bool DivideWith(this long id1, long id2, out long id_result)
        {
            id_result = -1;
            if (KMStorageWrapper.TryGetKey(id1, out IKey value1) && KMStorageWrapper.TryGetKey(id2, out IKey value2))
            {
                //两值都是基本映射,默认生成1层桥并关联1阶层
                if ((value1.KeyRelateType == MapKeyType.AND || value1.KeyRelateType == MapKeyType.None || value1.KeyRelateType == MapKeyType.OR)
                    && (value2.KeyRelateType == MapKeyType.AND || value2.KeyRelateType == MapKeyType.None || value2.KeyRelateType == MapKeyType.OR))
                {
                    KeyMapStorage.Instance.TryCreateBridgeKey(out var bridge_key_runtime_id, value1.Id, value2.Id, 1);
                    return KeyMapStorage.Instance.TryRegisterLevelKey(out id_result, bridge_key_runtime_id);
                }
                //两边都是桥,直接关联为新的层
                else if (value1.KeyRelateType == MapKeyType.Bridge && value2.KeyRelateType == MapKeyType.Bridge)
                {
                    return KeyMapStorage.Instance.TryRegisterLevelKey(out id_result, value1.Id, value2.Id);
                }
                //左层右层,拼接两个层级
                else if (value1.KeyRelateType == MapKeyType.LEVEL && value2.KeyRelateType == MapKeyType.LEVEL)
                {
                    KMStorageWrapper.TryGetKey(value1.Id, out ILevelKey _lkey1);
                    KMStorageWrapper.TryGetKey(value2.Id, out ILevelKey _lkey2);

                    var _temp = new List<long>(_lkey1.KeySequence);
                    _temp.AddRange(_lkey2.KeySequence);
                    return KeyMapStorage.Instance.TryRegisterLevelKey(out id_result, _temp.ToArray());
                }

                //左层右单键,单键组装深一层桥然后拼接层级
                else if (value1.KeyRelateType == MapKeyType.LEVEL &&
                    (value2.KeyRelateType == MapKeyType.AND || value2.KeyRelateType == MapKeyType.None || value2.KeyRelateType == MapKeyType.OR))
                {
                    KMStorageWrapper.TryGetKey(value1.Id, out ILevelKey _lkey1);
                    KMStorageWrapper.TryGetKey(_lkey1.KeySequence[_lkey1.KeySequence.Length - 1], out IBridgeKey _bkey);

                    KeyMapStorage.Instance.TryCreateBridgeKey(out var bridge_key_runtime_id, _bkey.Next.Id, value2.Id, _bkey.JumpLevel + 1);
                    var _temp = new List<long>(_lkey1.KeySequence);
                    _temp.Add(bridge_key_runtime_id);
                    return KeyMapStorage.Instance.TryRegisterLevelKey(out id_result, _temp.ToArray());
                }
                //左层级右桥,在该层级后接续新的桥
                else if (value1.KeyRelateType == MapKeyType.LEVEL && value2.KeyRelateType == MapKeyType.Bridge)
                {
                    KMStorageWrapper.TryGetKey(value1.Id, out ILevelKey _lkey);

                    var _temp = new List<long>(_lkey.KeySequence){value2.Id};
                    return KeyMapStorage.Instance.TryRegisterLevelKey(out id_result, _temp.ToArray());
                }

                //左桥右单键,单键组装深1层桥然后拼接层级
                else if (value1.KeyRelateType == MapKeyType.Bridge
                    &&(value2.KeyRelateType == MapKeyType.AND || value2.KeyRelateType == MapKeyType.None || value2.KeyRelateType == MapKeyType.OR))
                {
                    KMStorageWrapper.TryGetKey(value1.Id, out IBridgeKey _lbkey);
                    KeyMapStorage.Instance.TryCreateBridgeKey(out var bridge_key_runtime_id, _lbkey.Next.Id, value2.Id, _lbkey.JumpLevel + 1);
                    return KeyMapStorage.Instance.TryRegisterLevelKey(out id_result, value1.Id, bridge_key_runtime_id);
                }
                //左桥右层,在层左端大于1场合,且层左端层数币桥高1,在层前端拼接桥
                else if (value1.KeyRelateType == MapKeyType.Bridge
                    && (value2.KeyRelateType == MapKeyType.LEVEL))
                {
                    KMStorageWrapper.TryGetKey(value1.Id, out IBridgeKey _lbkey);
                    KMStorageWrapper.TryGetKey(value2.Id, out ILevelKey _rkey);
                    if (_rkey.StartLevel > 1)
                    {
                        KMStorageWrapper.TryGetKey(_rkey.KeySequence[1], out IBridgeKey _bkey);
                        if (_bkey.JumpLevel - _lbkey.JumpLevel == 1)
                        {
                            var _temp = new List<long>() { _lbkey.Id };
                            _temp.AddRange(_rkey.KeySequence);
                            return KeyMapStorage.Instance.TryRegisterLevelKey(out id_result, _temp.ToArray());
                        }
                    }
                }

                //左单键右桥,在桥所在层大于1场合,在桥前端生成单键的桥并拼接层级
                else if ((value1.KeyRelateType == MapKeyType.AND || value1.KeyRelateType == MapKeyType.None || value1.KeyRelateType == MapKeyType.OR)
                     && (value2.KeyRelateType == MapKeyType.Bridge))
                {
                    KMStorageWrapper.TryGetKey(value2.Id, out IBridgeKey _rbkey);
                    if (_rbkey.JumpLevel > 1)
                    {
                        KeyMapStorage.Instance.TryCreateBridgeKey(out var bridge_key_runtime_id, value1.Id, _rbkey.Current.Id, _rbkey.JumpLevel - 1);
                        return KeyMapStorage.Instance.TryRegisterLevelKey(out id_result, value1.Id, bridge_key_runtime_id);
                    }
                }
                //左单键右层,在层左端大于1场合,在层前端生成单键的桥并拼接层级
                else if ((value1.KeyRelateType == MapKeyType.AND || value1.KeyRelateType == MapKeyType.None || value1.KeyRelateType == MapKeyType.OR)
                     && (value2.KeyRelateType == MapKeyType.LEVEL))
                {
                    KMStorageWrapper.TryGetKey(value2.Id, out ILevelKey _rkey1);
                    if (_rkey1.StartLevel > 1)
                    {
                        KMStorageWrapper.TryGetKey(_rkey1.KeySequence[1], out IBridgeKey _bkey);
                        KeyMapStorage.Instance.TryCreateBridgeKey(out var bridge_key_runtime_id, value1.Id, _bkey.Current.Id, _bkey.JumpLevel - 1);
                        var _temp = new List<long>() { bridge_key_runtime_id };
                        _temp.AddRange(_rkey1.KeySequence);
                        return KeyMapStorage.Instance.TryRegisterLevelKey(out id_result, _temp.ToArray());
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 获取桥接key
        /// </summary>
        /// <param name="id1"></param>
        /// <param name="id2"></param>
        /// <param name="id_result"></param>
        /// <returns></returns>
        public static bool ConnectWith(this long id1, long id2, int level, out long id_result)
        {
            return KeyMapStorage.Instance.TryCreateBridgeKey(out id_result, id1, id2, level);
        }
    }
}
