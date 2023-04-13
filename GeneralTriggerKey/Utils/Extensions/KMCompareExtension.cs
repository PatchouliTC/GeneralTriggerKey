using GeneralTriggerKey.Key;
using GeneralTriggerKey.KeyMap;
using IdGen;
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
            if (id1 == KeyMapStorage.Instance.AnyTypeCode.Id || id2 == KeyMapStorage.Instance.AnyTypeCode.Id)
            {
                if (id1 == id2) return true;
                else if (id2 == KeyMapStorage.Instance.AnyTypeCode.Id) return true;
                return false;
            }

            if (KeyMapStorage.Instance.TryGetKey(id1, out IKey value1) && KeyMapStorage.Instance.TryGetKey(id2, out IKey value2))
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
                else if (value1.KeyRelateType == MapKeyType.BRIDGE || value1.KeyRelateType == MapKeyType.LEVEL || value1.KeyRelateType == MapKeyType.BRIDGE || value1.KeyRelateType == MapKeyType.LEVEL)
                {
                    return false;
                }
                //都不是,比较相等性
                else if (!value1.IsMultiKey && !value2.IsMultiKey)
                {
                    return id1 == id2;
                }
                //都是联合键且联合模式相等,比较关联的单一键列表超集关系
                else if ((value1.IsMultiKey && value2.IsMultiKey &&
                    (value1.KeyRelateType == value2.KeyRelateType)
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
            if (id1 == KeyMapStorage.Instance.AnyTypeCode.Id || id2 == KeyMapStorage.Instance.AnyTypeCode.Id)
            {
                if (id1 == id2) return true;
                else if (id2 == KeyMapStorage.Instance.AnyTypeCode.Id) return true;
                return false;
            }

            if (KeyMapStorage.Instance.TryGetKey(id1, out IKey value1) && KeyMapStorage.Instance.TryGetKey(id2, out IKey value2))
            {
                if (value1 is IBridgeKey bkey1 && value2 is IBridgeKey bkey2) return id1 == id2;
                else if (value1 is ILevelKey lkey1 && value2 is ILevelKey lkey2) return id1.Contains(id2);
                else if (value1.KeyRelateType == MapKeyType.BRIDGE || value1.KeyRelateType == MapKeyType.LEVEL || value1.KeyRelateType == MapKeyType.BRIDGE || value1.KeyRelateType == MapKeyType.LEVEL)
                {
                    return false;
                }
                else if (!value1.IsMultiKey && !value2.IsMultiKey) return id1 == id2;
                else if ((value1.IsMultiKey && value2.IsMultiKey &&
                    (value1.KeyRelateType == value2.KeyRelateType)
                    )) return (value1 as IMultiKey)!.ROverlaps((value2 as IMultiKey)!);
                else if (value1.IsMultiKey != value2.IsMultiKey)
                {
                    var multiKey = (value1.IsMultiKey) ? (value1 as IMultiKey) : (value2 as IMultiKey);
                    var _single_key = (value1.IsMultiKey) ? value2 : value1;
                    return multiKey!.Contains(_single_key.Id);
                }
            }
            return false;
        }

        /// <summary>
        /// 当前键能否触发后者条件
        /// </summary>
        /// <param name="currentId"></param>
        /// <param name="requireCheckId"></param>
        /// <returns></returns>
        public static bool CanTrigger(this long currentId, long requireCheckId, long mustContainKeyId = -1)
        {
            if (KeyMapStorage.Instance.TryGetKey(currentId, out IKey value1) && KeyMapStorage.Instance.TryGetKey(requireCheckId, out IKey value2))
            {
                KeyMapStorage.Instance.TryGetKey(mustContainKeyId, out IKey mustContainKeyIdInst);
                if (mustContainKeyIdInst != null && mustContainKeyIdInst.Id == KeyMapStorage.Instance.AnyTypeCode.Id) mustContainKeyIdInst = null!;

                if (currentId == KeyMapStorage.Instance.AnyTypeCode.Id || requireCheckId == KeyMapStorage.Instance.AnyTypeCode.Id)
                {
                    if (mustContainKeyIdInst is null)
                    {
                        if (currentId == KeyMapStorage.Instance.AnyTypeCode.Id) return false;
                        if (requireCheckId == KeyMapStorage.Instance.AnyTypeCode.Id) return true;
                    }
                    else if (value2 is ILevelKey levelKey2)
                    {
                        //单键与层比较,取层左侧第一个节点的Current检查能否触发
                        if (value1 is ISimpleNode simplekey1)
                        {
                            if (KeyMapStorage.Instance.TryGetKey(levelKey2.KeySequence[0], out IBridgeKey bridgeKey))
                            {
                                return bridgeKey.Current.CanTriggerNode.Contains(simplekey1.Id);
                            }
                        }
                        //桥与层比较,检测桥触发节点是否具备目标seq
                        else if (value1 is IBridgeKey bridgeKey1)
                        {
                            return levelKey2.KeySequence.Any(x => bridgeKey1.CanTriggerNode.Contains(x));
                        }
                    }
                    return value1.CanTriggerNode.Contains(value2.Id);
                }
                return false;

                //if (value1 is IBridgeKey bkey1 && value2 is IBridgeKey bkey2) return bkey1.CanTriggerNode.Contains(bkey2.Id);
                //else if (value1 is ILevelKey lkey1 && value2 is ILevelKey lkey2) return lkey1.CanTriggerNode.Contains(lkey2.Id);

                //else if (value1.KeyRelateType == MapKeyType.BRIDGE || value1.KeyRelateType == MapKeyType.LEVEL || value1.KeyRelateType == MapKeyType.BRIDGE || value1.KeyRelateType == MapKeyType.LEVEL)
                //{
                //    return false;
                //}
                ////双单例键,比较值相等
                //else if (!value1.IsMultiKey && !value2.IsMultiKey)
                //{
                //    if (mustContainKeyIdInst is null) return value1.Id == value2.Id;
                //    return value1.Id == value2.Id && value1.Id == mustContainKeyIdInst.Id;
                //}
                ////当前项是单键时,被比较项必须是or关系[and关系一定无法触发]
                //else if (!value1.IsMultiKey && value2.IsMultiKey && value2.KeyRelateType == MapKeyType.OR)
                //{
                //    if (mustContainKeyIdInst is null) return value1.CanTriggerNode.Contains(value2.Id);
                //    return mustContainKeyIdInst.Id == value1.Id && value1.CanTriggerNode.Contains(value2.Id);
                //    //var _multi_key = (value2 as IMultiKey);
                //    ////只要or中包含该基础节点即可
                //    //if (_need_contain_key is null)
                //    //    return _multi_key!.RelateSingleKeys.Contains(value1.Id);
                //    //return _need_contain_key.Id == value1.Id && _multi_key!.RelateSingleKeys.Contains(value1.Id);
                //}
                ////被比较项联合键+当前被比较对象能否触发目标对象
                ////当前项必须是and关系,or关系无比较意义
                //else if (value1.IsMultiKey && value1.KeyRelateType == MapKeyType.AND)
                //{
                //    var multiKeyInst1 = (value1 as IMultiKey);
                //    //项2是单例键,直接看项1的singlekey是否包含即可
                //    if (!value2.IsMultiKey)
                //    {
                //        if (mustContainKeyIdInst is null) return multiKeyInst1!.CanTriggerNode.Contains(value2.Id);
                //        return value2.Id == mustContainKeyIdInst.Id && multiKeyInst1!.CanTriggerNode.Contains(value2.Id);
                //    }
                //    else
                //    {
                //        var multiKeyInst2 = (value2 as IMultiKey);


                //        if (mustContainKeyIdInst is null) 
                //            return multiKeyInst1!.CanTriggerNode.Contains(multiKeyInst2!.Id);
                //        else
                //        {
                //            var isMustContainKeyInInst1 = multiKeyInst1!.Contains(mustContainKeyIdInst.Id);
                //            var isMustContainKeyInInst2 = multiKeyInst2!.Contains(mustContainKeyIdInst.Id);
                //            if (isMustContainKeyInInst1 && isMustContainKeyInInst2) 
                //                return multiKeyInst1!.CanTriggerNode.Contains(multiKeyInst2!.Id);
                //        }
                //    }
                //}
            }
            return false;
        }


        /// <summary>
        /// 两个集合的差集
        /// </summary>
        /// <param name="id1"></param>
        /// <param name="id2"></param>
        /// <param name="resultId"></param>
        /// <returns></returns>
        public static bool SymmetricExceptWith(this long id1, long id2, out long resultId)
        {
            resultId = -1;

            if (id1 == KeyMapStorage.Instance.AnyTypeCode.Id || id2 == KeyMapStorage.Instance.AnyTypeCode.Id) return false;

            if (KeyMapStorage.Instance.TryGetKey(id1, out IKey value1) && KeyMapStorage.Instance.TryGetKey(id2, out IKey value2))
            {

                if (value1 is IBridgeKey _ || value2 is IBridgeKey _) return false;
                else if (value1 is ILevelKey _ || value2 is ILevelKey _) return false;
                else if (value1.IsMultiKey && value2.IsMultiKey && value1.KeyRelateType == value2.KeyRelateType)
                {
                    var inst1singleKeys = (value1 as IMultiKey)!.RelateSingleKeys.ToHashSet();
                    inst1singleKeys.SymmetricExceptWith((value2 as IMultiKey)!.RelateSingleKeys);

                    return KeyMapStorage.Instance.TryRegisterMultiKey(out resultId, value1.KeyRelateType, inst1singleKeys.ToArray());
                }
                else if ((value1.IsMultiKey && !value2.IsMultiKey) || (!value1.IsMultiKey && value2.IsMultiKey))
                {
                    var multiInst = (value1.IsMultiKey) ? (value1 as IMultiKey) : (value2 as IMultiKey);
                    var singleInst = (value1.IsMultiKey) ? value2 : value1;

                    var inst1singleKeys = multiInst!.RelateSingleKeys.ToHashSet();
                    if (multiInst!.RelateSingleKeys.Contains(singleInst.Id)) inst1singleKeys.Remove(singleInst.Id);
                    else inst1singleKeys.Add(singleInst.Id);
                    return KeyMapStorage.Instance.TryRegisterMultiKey(out resultId, multiInst!.KeyRelateType, inst1singleKeys.ToArray());
                }
            }
            return false;
        }

        /// <summary>
        /// 两个集合的交集
        /// </summary>
        /// <param name="id1"></param>
        /// <param name="id2"></param>
        /// <param name="idResult"></param>
        /// <returns></returns>
        public static bool IntersectWith(this long id1, long id2, out long idResult)
        {
            if (id1 == KeyMapStorage.Instance.AnyTypeCode.Id || id2 == KeyMapStorage.Instance.AnyTypeCode.Id)
            {
                idResult = KeyMapStorage.Instance.AnyTypeCode.Id;
                return true;
            }

            idResult = -1;
            if (KeyMapStorage.Instance.TryGetKey(id1, out IKey value1) && KeyMapStorage.Instance.TryGetKey(id2, out IKey value2))
            {
                if (value1 is IBridgeKey _ || value2 is IBridgeKey _) return false;
                else if (value1 is ILevelKey _ || value2 is ILevelKey _) return false;
                else if (value1.IsMultiKey && value2.IsMultiKey && value1.KeyRelateType == value2.KeyRelateType)
                {
                    var value1InstSingleKeys = (value1 as IMultiKey)!.RelateSingleKeys.ToHashSet();
                    value1InstSingleKeys.IntersectWith((value2 as IMultiKey)!.RelateSingleKeys);
                    return KeyMapStorage.Instance.TryRegisterMultiKey(out idResult, value1.KeyRelateType, value1InstSingleKeys.ToArray());
                }
                else if ((value1.IsMultiKey && !value2.IsMultiKey) || (!value1.IsMultiKey && value2.IsMultiKey))
                {
                    var multiInst = (value1.IsMultiKey) ? (value1 as IMultiKey) : (value2 as IMultiKey);
                    var singleInst = (value1.IsMultiKey) ? value2 : value1;
                    if (multiInst!.KeyRelateType == MapKeyType.AND)
                    {
                        if (multiInst!.RelateSingleKeys.Contains(singleInst.Id))
                        {
                            idResult = singleInst.Id;
                            return true;
                        }
                    }
                    else if (multiInst!.KeyRelateType == MapKeyType.OR)
                    {
                        if (multiInst!.Contains(singleInst.Id))
                        {
                            idResult = singleInst.Id;
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 获取两个key的或关系集合[|]
        /// </summary>
        /// <param name="id1"></param>
        /// <param name="resultId"></param>
        /// <param name="id2"></param>
        /// <returns></returns>
        public static bool OrWith(this long id1, long id2, out long resultId)
        {
            if (id1 == KeyMapStorage.Instance.AnyTypeCode.Id || id2 == KeyMapStorage.Instance.AnyTypeCode.Id)
            {
                resultId = KeyMapStorage.Instance.AnyTypeCode.Id;
                return true;
            }

            resultId = -1;
            if (KeyMapStorage.Instance.TryGetKey(id1, out IKey key1) && KeyMapStorage.Instance.TryGetKey(id2, out IKey key2))
            {
                if (key1.KeyRelateType == MapKeyType.BRIDGE || key1.KeyRelateType == MapKeyType.LEVEL || key2.KeyRelateType == MapKeyType.BRIDGE || key2.KeyRelateType == MapKeyType.LEVEL)
                {
                    return false;
                }
                return KMStorageWrapper.TryRegisterMultiKey(out resultId, MapKeyType.OR, id1, id2);
            }
            return false;
        }

        /// <summary>
        /// 获取两个key的和关系集合[&]
        /// </summary>
        /// <param name="id1"></param>
        /// <param name="id2"></param>
        /// <returns></returns>
        public static bool AndWith(this long id1, long id2, out long resultId)
        {
            if (id1 == KeyMapStorage.Instance.AnyTypeCode.Id || id2 == KeyMapStorage.Instance.AnyTypeCode.Id)
            {
                resultId = KeyMapStorage.Instance.AnyTypeCode.Id;
                return true;
            }

            resultId = -1;
            if (KeyMapStorage.Instance.TryGetKey(id1, out IKey value1) && KeyMapStorage.Instance.TryGetKey(id2, out IKey value2))
            {
                if(value1.KeyRelateType==MapKeyType.BRIDGE||value1.KeyRelateType==MapKeyType.LEVEL|| value2.KeyRelateType == MapKeyType.BRIDGE || value2.KeyRelateType == MapKeyType.LEVEL)
                {
                    return false;
                }
                //二者均不为or关系
                else if (
                    !(value1.IsMultiKey && value1.KeyRelateType == MapKeyType.OR) &&
                    !(value2.IsMultiKey && value2.KeyRelateType == MapKeyType.OR)
                    )
                {
                    return KMStorageWrapper.TryRegisterMultiKey(out resultId, MapKeyType.AND, id1, id2);
                }
                //两个为or关系
                //拆开两组单列键进行AND操作的笛卡尔积
                //最终获得的数组之间再进行or关系
                else if (
                    (value1.IsMultiKey && value1.KeyRelateType == MapKeyType.OR) &&
                    (value2.IsMultiKey && value2.KeyRelateType == MapKeyType.OR)
                    )
                {
                    var value1MultiKeyInst = (value1 as IMultiKey)!.RelateSingleKeys;
                    var value2MultiKeyInst = (value2 as IMultiKey)!.RelateSingleKeys;

                    var relateIds = new List<long>();
                    foreach (var key1 in value1MultiKeyInst)
                    {
                        foreach (var key2 in value2MultiKeyInst)
                        {
                            if (KMStorageWrapper.TryRegisterMultiKey(out var _temp_id, MapKeyType.AND, key1, key2)) relateIds.Add(_temp_id);
                            else return false;
                        }
                    }

                    long tempStorage = 0;
                    foreach (var temp_key in relateIds)
                    {
                        if (tempStorage == 0)
                        {
                            tempStorage = temp_key;
                            continue;
                        }
                        if (KMStorageWrapper.TryRegisterMultiKey(out var _temp_id, MapKeyType.OR, tempStorage, temp_key)) tempStorage = _temp_id;
                        else return false;
                    }
                    resultId = tempStorage;
                    return true;
                }
                //一个是or另一个单例键
                else
                {
                    var orKey = (value1.IsMultiKey && value1.KeyRelateType == MapKeyType.OR) ? (value1 as IMultiKey) : (value2 as IMultiKey);
                    var nonOrKey = (value1.IsMultiKey && value1.KeyRelateType == MapKeyType.OR) ? value2 : value1;

                    var orKeyRelateKeys = orKey!.RelateSingleKeys;

                    var result_ids = new List<long>();
                    foreach (var key in orKeyRelateKeys)
                    {
                        if (KMStorageWrapper.TryRegisterMultiKey(out var _temp_id, MapKeyType.AND, nonOrKey.Id, key)) result_ids.Add(_temp_id);
                        else return false;
                    }

                    long tempStorage = 0;

                    foreach (var keyId in result_ids)
                    {
                        if (tempStorage == 0)
                        {
                            tempStorage = keyId;
                            continue;
                        }
                        if (KMStorageWrapper.TryRegisterMultiKey(out var _temp_id, MapKeyType.OR, tempStorage, keyId)) tempStorage = _temp_id;
                        else return false;
                    }
                    resultId = tempStorage;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 获取两个key的和关系集合[&]
        /// <para>仅当key存在时获取,如果不存在则获取失败</para>
        /// </summary>
        /// <param name="id1"></param>
        /// <param name="id2"></param>
        /// <returns></returns>
        public static bool AndWithIfExist(this long id1, long id2, out long resultId)
        {
            if (id1 == KeyMapStorage.Instance.AnyTypeCode.Id || id2 == KeyMapStorage.Instance.AnyTypeCode.Id)
            {
                resultId = KeyMapStorage.Instance.AnyTypeCode.Id;
                return true;
            }
            resultId = -1;
            if (KeyMapStorage.Instance.TryGetKey(id1, out IKey value1) && KeyMapStorage.Instance.TryGetKey(id2, out IKey value2))
            {
                if (value1.KeyRelateType == MapKeyType.BRIDGE || value1.KeyRelateType == MapKeyType.LEVEL || value1.KeyRelateType == MapKeyType.BRIDGE || value1.KeyRelateType == MapKeyType.LEVEL)
                {
                    return false;
                }
                //二者均不为or关系
                else if (
                    !(value1.IsMultiKey && value1.KeyRelateType == MapKeyType.OR) &&
                    !(value2.IsMultiKey && value2.KeyRelateType == MapKeyType.OR)
                    )
                {
                    return KMStorageWrapper.TryRegisterMultiKeyIfExist(out resultId, MapKeyType.AND, id1, id2);
                }
                //两个为or关系
                //拆开两组单列键进行AND操作的笛卡尔积
                //最终获得的数组之间再进行or关系
                else if (
                    (value1.IsMultiKey && value1.KeyRelateType == MapKeyType.OR) &&
                    (value2.IsMultiKey && value2.KeyRelateType == MapKeyType.OR)
                    )
                {
                    var value1MultiKeys = (value1 as IMultiKey)!.RelateSingleKeys;
                    var value2MultiKeys = (value2 as IMultiKey)!.RelateSingleKeys;

                    var relateIds = new List<long>();
                    foreach (var key1 in value1MultiKeys)
                    {
                        foreach (var key2 in value2MultiKeys)
                        {
                            if (KMStorageWrapper.TryRegisterMultiKeyIfExist(out var _temp_id, MapKeyType.AND, key1, key2)) relateIds.Add(_temp_id);
                            else return false;
                        }
                    }
                    long tempStorage = 0;
                    foreach (var keyId in relateIds)
                    {
                        if (tempStorage == 0)
                        {
                            tempStorage = keyId;
                            continue;
                        }
                        if (KMStorageWrapper.TryRegisterMultiKeyIfExist(out var _temp_id, MapKeyType.OR, tempStorage, keyId)) tempStorage = _temp_id;
                        else return false;
                    }
                    resultId = tempStorage;
                    return true;
                }
                //一个是or另一个单例键
                else
                {
                    var orKey = (value1.IsMultiKey && value1.KeyRelateType == MapKeyType.OR) ? (value1 as IMultiKey) : (value2 as IMultiKey);
                    var nonOrKey = (value1.IsMultiKey && value1.KeyRelateType == MapKeyType.OR) ? value2 : value1;

                    var orKeyRelateKeys = orKey!.RelateSingleKeys;

                    var relateIds = new List<long>();
                    foreach (var key in orKeyRelateKeys)
                    {
                        if (KMStorageWrapper.TryRegisterMultiKeyIfExist(out var _temp_id, MapKeyType.AND, nonOrKey.Id, key)) relateIds.Add(_temp_id);
                        else return false;
                    }

                    long tempStorage = 0;

                    foreach (var temp_key in relateIds)
                    {
                        if (tempStorage == 0)
                        {
                            tempStorage = temp_key;
                            continue;
                        }
                        if (KMStorageWrapper.TryRegisterMultiKeyIfExist(out var _temp_id, MapKeyType.OR, tempStorage, temp_key)) tempStorage = _temp_id;
                        else return false;
                    }
                    resultId = tempStorage;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 获取两个桥接key的层级key
        /// </summary>
        /// <param name=""></param>
        /// <param name="id2"></param>
        /// <param name="resultId"></param>
        /// <returns></returns>
        public static bool DivideWith(this long id1, long id2, out long resultId)
        {
            resultId = -1;
            if (KeyMapStorage.Instance.TryGetKey(id1, out IKey value1) && KeyMapStorage.Instance.TryGetKey(id2, out IKey value2))
            {
                //两值都是基本映射,默认生成1层桥并关联1阶层
                if ((value1.KeyRelateType == MapKeyType.AND || value1.KeyRelateType == MapKeyType.NONE || value1.KeyRelateType == MapKeyType.OR)
                    && (value2.KeyRelateType == MapKeyType.AND || value2.KeyRelateType == MapKeyType.NONE || value2.KeyRelateType == MapKeyType.OR))
                {
                    KeyMapStorage.Instance.TryCreateBridgeKey(out var newBridgeKeyId, value1.Id, value2.Id, 1);
                    return KeyMapStorage.Instance.TryRegisterLevelKey(out resultId, newBridgeKeyId);
                }
                //两边都是桥,直接关联为新的层
                else if (value1.KeyRelateType == MapKeyType.BRIDGE && value2.KeyRelateType == MapKeyType.BRIDGE)
                {
                    return KeyMapStorage.Instance.TryRegisterLevelKey(out resultId, value1.Id, value2.Id);
                }
                //左层右层,拼接两个层级
                else if (value1.KeyRelateType == MapKeyType.LEVEL && value2.KeyRelateType == MapKeyType.LEVEL)
                {
                    KeyMapStorage.Instance.TryGetKey(value1.Id, out ILevelKey leftLevelKey);
                    KeyMapStorage.Instance.TryGetKey(value2.Id, out ILevelKey rigthLevelKey);

                    var newKeySeq = new List<long>(leftLevelKey.KeySequence);
                    newKeySeq.AddRange(rigthLevelKey.KeySequence);
                    return KeyMapStorage.Instance.TryRegisterLevelKey(out resultId, newKeySeq.ToArray());
                }

                //左层右单键,单键组装深一层桥然后拼接层级
                else if (value1.KeyRelateType == MapKeyType.LEVEL
                    && (value2.KeyRelateType == MapKeyType.AND || value2.KeyRelateType == MapKeyType.NONE || value2.KeyRelateType == MapKeyType.OR))
                {
                    KeyMapStorage.Instance.TryGetKey(value1.Id, out ILevelKey leftLevelKey);
                    KeyMapStorage.Instance.TryGetKey(leftLevelKey.KeySequence[leftLevelKey.KeySequence.Length - 1], out IBridgeKey leftLevelSeqLastBridgeKey);

                    KeyMapStorage.Instance.TryCreateBridgeKey(out var newBridgeKeyId, leftLevelSeqLastBridgeKey.Next.Id, value2.Id, leftLevelSeqLastBridgeKey.JumpLevel + 1);
                    var newKeySeq = new List<long>(leftLevelKey.KeySequence);
                    newKeySeq.Add(newBridgeKeyId);
                    return KeyMapStorage.Instance.TryRegisterLevelKey(out resultId, newKeySeq.ToArray());
                }
                //左层级右桥,在该层级后接续新的桥
                else if (value1.KeyRelateType == MapKeyType.LEVEL
                    && value2.KeyRelateType == MapKeyType.BRIDGE)
                {
                    KeyMapStorage.Instance.TryGetKey(value1.Id, out ILevelKey leftLevelKey);

                    var newKeySeq = new List<long>(leftLevelKey.KeySequence);
                    newKeySeq.Add(value2.Id);
                    return KeyMapStorage.Instance.TryRegisterLevelKey(out resultId, newKeySeq.ToArray());
                }

                //左桥右单键,单键组装深1层桥然后拼接层级
                else if (value1.KeyRelateType == MapKeyType.BRIDGE
                    && (value2.KeyRelateType == MapKeyType.AND || value2.KeyRelateType == MapKeyType.NONE || value2.KeyRelateType == MapKeyType.OR))
                {
                    KeyMapStorage.Instance.TryGetKey(value1.Id, out IBridgeKey leftBridgeKey);
                    KeyMapStorage.Instance.TryCreateBridgeKey(out var newRightBridgeKeyId, leftBridgeKey.Next.Id, value2.Id, leftBridgeKey.JumpLevel + 1);
                    return KeyMapStorage.Instance.TryRegisterLevelKey(out resultId, leftBridgeKey.Id, newRightBridgeKeyId);
                }
                //左桥右层,在层左端大于1场合,且层左端层数币桥高1,在层前端拼接桥
                else if (value1.KeyRelateType == MapKeyType.BRIDGE
                    && (value2.KeyRelateType == MapKeyType.LEVEL))
                {
                    KeyMapStorage.Instance.TryGetKey(value1.Id, out IBridgeKey leftBridgeKey);
                    KeyMapStorage.Instance.TryGetKey(value2.Id, out ILevelKey rightLevelKey);
                    if (rightLevelKey.StartLevel > 1)
                    {
                        KeyMapStorage.Instance.TryGetKey(rightLevelKey.KeySequence[0], out IBridgeKey rightLevelSeqFirstBridgeKey);
                        if (rightLevelSeqFirstBridgeKey.JumpLevel - leftBridgeKey.JumpLevel == 1)
                        {
                            var newKeySeq = new List<long>() { leftBridgeKey.Id };
                            newKeySeq.AddRange(rightLevelKey.KeySequence);
                            return KeyMapStorage.Instance.TryRegisterLevelKey(out resultId, newKeySeq.ToArray());
                        }
                    }
                }

                //左单键右桥,在桥所在层大于1场合,在桥前端生成单键的桥并拼接层级
                else if ((value1.KeyRelateType == MapKeyType.AND || value1.KeyRelateType == MapKeyType.NONE || value1.KeyRelateType == MapKeyType.OR)
                     && (value2.KeyRelateType == MapKeyType.BRIDGE))
                {
                    KeyMapStorage.Instance.TryGetKey(value2.Id, out IBridgeKey rightBridgeKey);
                    if (rightBridgeKey.JumpLevel > 1)
                    {
                        KeyMapStorage.Instance.TryCreateBridgeKey(out var newLeftKeyId, value1.Id, rightBridgeKey.Current.Id, rightBridgeKey.JumpLevel - 1);
                        return KeyMapStorage.Instance.TryRegisterLevelKey(out resultId, newLeftKeyId,rightBridgeKey.Id);
                    }
                }
                //左单键右层,在层左端大于1场合,在层前端生成单键的桥并拼接层级
                else if ((value1.KeyRelateType == MapKeyType.AND || value1.KeyRelateType == MapKeyType.NONE || value1.KeyRelateType == MapKeyType.OR)
                     && (value2.KeyRelateType == MapKeyType.LEVEL))
                {
                    KeyMapStorage.Instance.TryGetKey(value2.Id, out ILevelKey rightLevelKey);
                    if (rightLevelKey.StartLevel > 1)
                    {
                        KeyMapStorage.Instance.TryGetKey(rightLevelKey.KeySequence[0], out IBridgeKey rightLevelSeqFirstBridgeKey);
                        KeyMapStorage.Instance.TryCreateBridgeKey(out var newLeftBridgeKeyId, value1.Id, rightLevelSeqFirstBridgeKey.Current.Id, rightLevelSeqFirstBridgeKey.JumpLevel - 1);
                        var newKeySeq = new List<long>() { newLeftBridgeKeyId };
                        newKeySeq.AddRange(rightLevelKey.KeySequence);
                        return KeyMapStorage.Instance.TryRegisterLevelKey(out resultId, newKeySeq.ToArray());
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
            id_result = -1;
            if (KeyMapStorage.Instance.TryGetKey(id1, out ISimpleNode value1) && KeyMapStorage.Instance.TryGetKey(id2, out ISimpleNode value2))
            {
                return KeyMapStorage.Instance.TryCreateBridgeKey(out id_result, value1.Id, value2.Id, level);
            }
            return false;
        }
    }
}
