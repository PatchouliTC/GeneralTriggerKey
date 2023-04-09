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

                if (!value1.IsMultiKey && !value2.IsMultiKey)
                {
                    return id1 == id2;
                }
                else if ((value1.IsMultiKey && value2.IsMultiKey &&
                    ((value1 as IMultiKey)!.KeyRelateType == (value2 as IMultiKey)!.KeyRelateType)
                    ))
                {
                    return (value1 as IMultiKey)!.IsRSupersetOf((value2 as IMultiKey)!);
                }
                else if ((value1.IsMultiKey && !value2.IsMultiKey) || (value1.IsMultiKey && value2.IsMultiKey &&
                    (value1 as IMultiKey)!.KeyRelateType == MapKeyType.OR &&
                     (value2 as IMultiKey)!.KeyRelateType == MapKeyType.AND))
                {
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
                if (!value1.IsMultiKey && !value2.IsMultiKey)
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
        /// 当前键能否满足后者条件
        /// </summary>
        /// <param name="current_id"></param>
        /// <param name="check_id"></param>
        /// <returns></returns>
        public static bool CanTrigger(this long current_id, long check_id, long force_contain_key_id = -1)
        {
            if (KMStorageWrapper.TryGetKey(current_id, out IKey value1) && KMStorageWrapper.TryGetKey(check_id, out IKey value2))
            {
                KMStorageWrapper.TryGetKey(force_contain_key_id, out IKey _need_contain_key);

                if (!value1.IsMultiKey && !value2.IsMultiKey)
                {
                    if (_need_contain_key is null)
                        return value1.Id == value2.Id;
                    return value1.Id == value2.Id && value1.Id == _need_contain_key.Id;
                }
                else if (!value1.IsMultiKey && value2.IsMultiKey && (value2 as IMultiKey)!.KeyRelateType == MapKeyType.OR)
                {
                    if (_need_contain_key is null)
                        return value1.CanTriggerNode.Contains(value2.Id);
                    return _need_contain_key.Id == value1.Id && value1.CanTriggerNode.Contains(value2.Id);
                }

                else if (value1.IsMultiKey && (value1 as IMultiKey)!.KeyRelateType == MapKeyType.AND)
                {
                    var _multi_key = (value1 as IMultiKey);
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
                if (value1.IsMultiKey && value2.IsMultiKey && (value1 as IMultiKey)!.KeyRelateType == (value2 as IMultiKey)!.KeyRelateType)
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
                if (value1.IsMultiKey && value2.IsMultiKey && (value1 as IMultiKey)!.KeyRelateType == (value2 as IMultiKey)!.KeyRelateType)
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
                if (
                    !(value1.IsMultiKey && (value1 as IMultiKey)!.KeyRelateType == MapKeyType.OR) &&
                    !(value2.IsMultiKey && (value2 as IMultiKey)!.KeyRelateType == MapKeyType.OR)
                    )
                {
                    return KMStorageWrapper.TryRegisterMultiKey(out id_result, MapKeyType.AND, id1, id2);
                }
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
                if (
                    !(value1.IsMultiKey && (value1 as IMultiKey)!.KeyRelateType == MapKeyType.OR) &&
                    !(value2.IsMultiKey && (value2 as IMultiKey)!.KeyRelateType == MapKeyType.OR)
                    )
                {
                    return KMStorageWrapper.TryRegisterMultiKeyIfExist(out id_result, MapKeyType.AND, id1, id2);
                }
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
    }
}
