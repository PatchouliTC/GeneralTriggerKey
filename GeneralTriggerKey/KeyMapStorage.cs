using EnumsNET;
using GeneralTriggerKey.Attributes;
using GeneralTriggerKey.Group;
using GeneralTriggerKey.Key;
using GeneralTriggerKey.KeyMap;
using GeneralTriggerKey.Utils;
using GeneralTriggerKey.Utils.Extensions;
using HashidsNet;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace GeneralTriggerKey
{
    internal sealed class KeyMapStorage
    {
        #region Instance
        private static KeyMapStorage _instance = default!;
        private ILogger _logger;
        private Hashids _hashids;
        internal static KeyMapStorage Instance
        {
            get
            {
                return _instance ??= new KeyMapStorage();
            }
        }
        private KeyMapStorage()
        {
            _logger = GLogger.Instance.GetLogger<KeyMapStorage>();
            //hashids 协同IDCreator的创建key
            _hashids = new Hashids(IdCreator.Instance.IdTimeSeed.ToString());
            TryGetOrAddSingleKey(out var _node, "ANY", "GLO");
            _any_node = Keys.GetValueOrDefault(_node);
        }
        #endregion

        #region Prop
        internal Dictionary<long, IKey> Keys { get; private set; } = new Dictionary<long, IKey>();
        internal Dictionary<long, IGroup> Groups { get; private set; } = new Dictionary<long, IGroup>();
        #endregion

        #region Field
        private Dictionary<string, IKey> _name_key_map = new Dictionary<string, IKey>();
        private Dictionary<string, IGroup> _name_group_map = new Dictionary<string, IGroup>();
        private Dictionary<long, IMultiKey> _multi_key_map = new Dictionary<long, IMultiKey>();//key是运行时id
        private TypeCode[] _support_enum_type = new TypeCode[] {
            TypeCode.Byte,
            TypeCode.SByte,
            TypeCode.UInt16,
            TypeCode.UInt32,
            TypeCode.UInt64,
            TypeCode.Int16,
            TypeCode.Int32,
            TypeCode.Int64
        };

        private Dictionary<string, long> _created_multi_key_cache = new Dictionary<string, long>();
        private Dictionary<string, long> _created_bridge_key_cache = new Dictionary<string, long>();
        private Dictionary<string, long> _created_level_key_cache = new Dictionary<string, long>();


        private IKey _any_node;
        #endregion
        /// <summary>
        /// 自动发现指定程序集内所有需求映射的枚举表
        /// </summary>
        /// <param name="assembly">目标程序集</param>
        /// <exception cref="ArgumentException"></exception>
        /// <returns></returns>
        internal void AutoInjectEnumsMapping(Assembly assembly)
        {
            _logger.LogDebug($"Scan {assembly.FullName} Enums...");
            var _use_mark_enums_types = assembly.GetClassWithAttributeType<MapEnumAttribute>();
            if (_use_mark_enums_types is null)
            {
                _logger.LogDebug($"No require map enums find in {assembly.FullName}");
                return;
            }
            _logger.LogDebug($"-Detect {_use_mark_enums_types!.Count} Enums To Add.");
            foreach (var _type_info in _use_mark_enums_types)
            {
                var _enumType = _type_info.ClassInfo;

                if (!_support_enum_type.Contains(Enums.GetTypeCode(_enumType)))
                {
                    _logger.LogError($"Enum {nameof(_enumType)} not support value [int16/32/64],it's {Enums.GetTypeCode(_enumType)}");
                    continue;
                }
                _logger.LogDebug($"--Try register {_enumType.FullName}");


                if (_name_group_map.ContainsKey(_enumType.FullName))
                {
                    _logger.LogWarning($"--{_enumType.FullName} already registered,skip");
                    continue;
                }
                var _new_enum_group = new EnumGroup(id: IdCreator.Instance.GetId(), enumType: _enumType, aliaName: _enumType.Name);
                Groups.Add(_new_enum_group.Id, _new_enum_group);
                _name_group_map.Add(_new_enum_group.Name!, _new_enum_group);

                foreach (var _member in Enums.GetMembers(_enumType))
                {
                    var _member_full_name = $"{_enumType.FullName}-{_member.Name}";

                    if (_name_key_map.ContainsKey(_member_full_name))
                    {
                        _logger.LogError($"---{_member_full_name} already registered,skip");
                        continue;
                    }

                    _logger.LogDebug($"---Register {_member_full_name}");
                    var _enum_member = new EnumKey(id: IdCreator.Instance.GetId(), originId: _member.ToInt64(), belongGroup: _new_enum_group, name: _member_full_name);

                    Keys.Add(_enum_member.Id, _enum_member);
                    _new_enum_group.RelateKeys.Add(_enum_member.OriginId, _enum_member);

                    _name_key_map.Add(_enum_member.Name!, _enum_member);

                    //var _tname_member_name = $"{_enumType.Name}_{_member.Name}";

                    if (_name_key_map.TryAdd(_member.Name, _enum_member))
                    {
                        _enum_member.Alias.Add(_member.Name);
                    }
                    else
                    {
                        _logger.LogError($"---Failed register {_member.Name} because it's already exist.");
                    }

                    foreach (var _attribute in _member.Attributes.OfType<EnumAliaAttribute>())
                    {
                        foreach (var name in _attribute.Names)
                        {
                            if (!_name_key_map.TryAdd(name, _enum_member))
                            {
                                _logger.LogError($"---Unable add alia name {name} for {_member_full_name} because it's already exist.");
                                continue;
                            }
                            _enum_member.Alias.Add(name);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 运行时增添key
        /// </summary>
        /// <param name="callName">key名称</param>
        /// <param name="range">Key隶属域</param>
        /// <param name="force_add">是否强制添加</param>
        /// <returns></returns>
        public bool TryGetOrAddSingleKey(out long _runtime_add_key_id, string callName, string range = "GLO", bool force_add = false)
        {
            if (_name_key_map.TryGetValue(callName, out var _exist_key))
            {
                if (_exist_key is RunTimeKey && !force_add)
                {
                    _runtime_add_key_id = _exist_key.Id;
                    return true;
                }
                _logger.LogWarning($"Find exist key for {range}-{callName} runtime key,but not set force add.");
                _runtime_add_key_id = -1;
                return false;
            }

            var _new_single_key = new RunTimeKey(id: IdCreator.Instance.GetId(), name: callName, range: range);

            _name_key_map.Add(_new_single_key.Name!, _new_single_key);
            _name_key_map.Add(callName, _new_single_key);
            Keys.Add(_new_single_key.Id, _new_single_key);

            _runtime_add_key_id = _new_single_key.Id;
            return true;
        }

        /// <summary>
        /// 注册新的联合键值
        /// </summary>
        /// <param name="register_key_ids">运行时id</param>
        /// <returns></returns>
        public bool TryRegisterMultiKey(out long multi_key_runtime_id, MapKeyType keyType, long[] register_key_ids, bool only_try = false)
        {
            multi_key_runtime_id = -1;
            register_key_ids = register_key_ids.Where(x => x != _any_node.Id).ToArray();
            if (register_key_ids.Length == 0)
                return false;

            if (register_key_ids.Length == 1)
            {
                if (Keys.TryGetValue(register_key_ids[0], out var _single_key))
                {
                    multi_key_runtime_id = _single_key.Id;
                    return true;
                }
                else
                {
                    return false;
                }

            }

            var _distinct_and_sort_ids = register_key_ids.Distinct().OrderBy(x => x);

            var _key_hash = $"{Enums.GetName(keyType)}-{_hashids.EncodeLong(_distinct_and_sort_ids)}";

            if (_created_multi_key_cache.TryGetValue(_key_hash, out multi_key_runtime_id))
                return true;
            else
                if (only_try)
                return false;

            //获取实际上的所有单一键
            var _fact_relate_all_single_keys = new HashSet<long>();
            if (keyType == MapKeyType.AND)
            {
                foreach (var _id in _distinct_and_sort_ids)
                {
                    if (Keys.TryGetValue(_id, out var _exist_key))
                    {
                        if (_exist_key is ILevelKey || _exist_key is IBridgeKey)
                            return false;
                        //针对联合key和单一key不同处理
                        if (_exist_key.IsMultiKey)
                        {
                            //AND关系插入时不支持Or关系
                            if ((_exist_key as IMultiKey)!.KeyRelateType != MapKeyType.AND)
                                throw new ArgumentException(message: $"{_id} is [or relate] but in [and relate] list,can't create (A|B)&C multiKey.");
                            //联合key需要获取其所有的关联的非联合key项
                            //取二者并集
                            _fact_relate_all_single_keys.UnionWith((_exist_key as IMultiKey)!.RelateSingleKeys);
                        }
                        else
                        {
                            _fact_relate_all_single_keys.Add(_id);
                        }
                    }
                    else
                    {
                        //不存在的key记录无法操作
                        throw new ArgumentException(message: $"{_id} not an avilable runtime key for map");
                    }
                }
            }
            else if (keyType == MapKeyType.OR)
            {
                //Or关联
                //Single对应了单例键+AND关系的复合键
                //AND关系复合键不会再拆解,而是仅用当前AND复合键
                //获取实际上的所有单例键+AND关系复合键
                foreach (var _id in _distinct_and_sort_ids)
                {
                    if (Keys.TryGetValue(_id, out var _exist_key))
                    {
                        if (_exist_key is ILevelKey || _exist_key is IBridgeKey)
                            return false;
                        //针对联合key和单一key不同处理
                        //Or关系插入时候,如果键是AND,当作单例键看待,如果键是Or则拆开
                        if (_exist_key.IsMultiKey && (_exist_key as IMultiKey)!.KeyRelateType == MapKeyType.OR)
                            //联合key需要获取其所有的关联的非联合key项
                            //取二者并集
                            _fact_relate_all_single_keys.UnionWith((_exist_key as IMultiKey)!.RelateSingleKeys);
                        else
                            _fact_relate_all_single_keys.Add(_id);
                    }
                    else
                    {
                        //不存在的key记录无法操作
                        throw new ArgumentException(message: $"{_id} not an avilable runtime key for map");
                    }
                }
            }
            else
                throw new InvalidOperationException(message: $"Can't register multi key for type {keyType}");

            var _after_order = _fact_relate_all_single_keys.OrderBy(x => x);
            //排序，生成唯一hashid
            string _after_hash = $"M|{Enums.GetName(keyType)}-{_hashids.EncodeLong(_after_order)}";
            //判断是否是已存在联合键值
            if (_name_key_map.TryGetValue(_after_hash, out var _key))
            {
                multi_key_runtime_id = _key.Id;
                return true;
            }

            if (only_try)
            {
                multi_key_runtime_id = -1;
                return false;
            }

            //预创建
            var _new_multi_key = new MulitKey(id: IdCreator.Instance.GetId(),
                keyType: keyType,
                relatesinglekeys: _fact_relate_all_single_keys,
                name: _after_hash,
                _relate_keys: _after_order);

            //注意类别AND OR仅查询对应类别的
            //需要将当前键添加作为关联子键的所有项【超集】
            var _need_add_current_to_child_keys_keys = _multi_key_map.Values
                    .Where(x =>
                    x.KeyRelateType == keyType &&
                    x.RelateSingleKeys.Count > _fact_relate_all_single_keys.Count &&
                    x.RelateSingleKeys.IsSupersetOf(_fact_relate_all_single_keys));

            //需要将当前键添加作为超集键的所有项【子集】
            var _need_add_current_to_parent_keys_keys = _multi_key_map.Values
                    .Where(x =>
                    x.KeyRelateType == keyType &&
                    x.RelateSingleKeys.Count < _fact_relate_all_single_keys.Count &&
                    _fact_relate_all_single_keys.IsSupersetOf(x.RelateSingleKeys));

            //遍历所有超集
            foreach (var _add_key_to_child in _need_add_current_to_child_keys_keys)
            {
                //添加前删除所有即将作为当前键为子集的键，因为哪些键将关联自身
                foreach (var _key_remove in _add_key_to_child.ChildKeys.Values.Where(x => x.IsMultiKey && _need_add_current_to_parent_keys_keys.Any(y => y.Id == x.Id)))
                {
                    _add_key_to_child.ChildKeys.Remove(_key_remove.Id);
                    (_key_remove as IMultiKey)!.ParentKeys.RemoveAll(x => x.Id == _add_key_to_child.Id);

                    //处理DAG视角的关联
                    if (keyType == MapKeyType.AND)
                    {
                        //AND关系,从child中删除所有超集,因为AND关系层遵循小AND<-大AND关系,将当前关系插入
                        _key_remove.DAGParentKeys.Remove(_add_key_to_child);
                        _add_key_to_child.DAGChildKeys.Remove(_key_remove);
                    }
                    else if (keyType == MapKeyType.OR)
                    {
                        //OR关系,DAG关系层反向
                        //Parent断开和child的DAG关系
                        _add_key_to_child.DAGParentKeys.Remove(_key_remove);
                        _key_remove.DAGChildKeys.Remove(_add_key_to_child);
                    }
                }
                //删除已经在当前键中包含的同时在该超集中存在的单键
                foreach (var _key_remove_single in _add_key_to_child.ChildKeys.Values.Where(x => !x.IsMultiKey && _fact_relate_all_single_keys.Contains(x.Id)))
                {
                    _add_key_to_child.ChildKeys.Remove(_key_remove_single.Id);
                    //处理DAG视角的关联
                    if (keyType == MapKeyType.AND)
                    {
                        //AND关系,从child中删除所有超集,因为AND关系层遵循小Single<-大AND关系,将当前关系插入
                        _key_remove_single.DAGParentKeys.Remove(_add_key_to_child);
                        _add_key_to_child.DAGChildKeys.Remove(_key_remove_single);
                    }
                    else if (keyType == MapKeyType.OR)
                    {
                        //OR关系,DAG关系层反向
                        //Parent断开和child的DAG关系
                        _add_key_to_child.DAGParentKeys.Remove(_key_remove_single);
                        _key_remove_single.DAGChildKeys.Remove(_add_key_to_child);
                    }

                }
                //将自身添加进超集项的子键集并将该超集添加到该项的父关联
                _add_key_to_child.ChildKeys.Add(_new_multi_key.Id, _new_multi_key);
                _new_multi_key.ParentKeys.Add(_add_key_to_child);

                //处理DAG视角的关联
                if (keyType == MapKeyType.AND)
                {
                    //AND关系,将自身DAG上级关系关联上该_add_key_to_child
                    _new_multi_key.DAGParentKeys.Add(_add_key_to_child);
                    _add_key_to_child.DAGChildKeys.Add(_new_multi_key);
                }
                else if (keyType == MapKeyType.OR)
                {
                    //OR关系,DAG上级关系反向
                    //parent是DAG的下级,关联上自身作为上级
                    _add_key_to_child.DAGParentKeys.Add(_new_multi_key);
                    _new_multi_key.DAGChildKeys.Add(_add_key_to_child);

                    //继承下级的触发ids
                    _new_multi_key.CanTriggerNode.UnionWith(_add_key_to_child.CanTriggerNode);
                    //囊括下级
                    _new_multi_key.CanTriggerNode.Add(_add_key_to_child.Id);
                }
            }

            var _temp_set = new HashSet<long>();
            //遍历所有的子集,对应的超集都已经删除关联完毕,直接将当前键加入对应父关联
            foreach (var _add_key_to_parent in _need_add_current_to_parent_keys_keys)
            {
                //检查子集之间关系,如果相互存在包含关系则对于更小的子集忽略加入操作
                if (_need_add_current_to_parent_keys_keys.Any(x => x.Contains(_add_key_to_parent.Id)))
                    continue;

                _new_multi_key.ChildKeys.Add(_add_key_to_parent.Id, _add_key_to_parent);
                _add_key_to_parent.ParentKeys.Add(_new_multi_key);
                _temp_set.UnionWith(_add_key_to_parent.RelateSingleKeys);

                //处理DAG视角的关联
                if (keyType == MapKeyType.AND)
                {
                    //AND关系,将自身DAG上级关系关联上该_add_key_to_parent
                    _add_key_to_parent.DAGParentKeys.Add(_new_multi_key);
                    _new_multi_key.DAGChildKeys.Add(_add_key_to_parent);
                    //继承下级的触发ids
                    _new_multi_key.CanTriggerNode.UnionWith(_add_key_to_parent.CanTriggerNode);
                    //囊括下级
                    _new_multi_key.CanTriggerNode.Add(_add_key_to_parent.Id);
                }
                else if (keyType == MapKeyType.OR)
                {
                    //OR关系,DAG上级关系反向
                    //parent是DAG的下级,关联上自身作为上级
                    _new_multi_key.DAGParentKeys.Add(_add_key_to_parent);
                    _add_key_to_parent.DAGChildKeys.Add(_new_multi_key);
                }
            }

            //全部处理完毕,设置当前项的剩余单键关联
            _temp_set.SymmetricExceptWith(_fact_relate_all_single_keys);
            foreach (var key in _temp_set)
            {
                var _key_inst = Keys.GetValueOrDefault(key);
                _new_multi_key.ChildKeys.Add(key, _key_inst);
                //处理DAG视角的关联
                if (keyType == MapKeyType.AND)
                {
                    //AND关系,将自身DAG上级关系关联上该_add_key_to_child
                    _key_inst.DAGParentKeys.Add(_new_multi_key);
                    _new_multi_key.DAGChildKeys.Add(_key_inst);
                    //AND关系新增单键,合并单键的triggernodes
                    _new_multi_key.CanTriggerNode.UnionWith(_key_inst.CanTriggerNode);
                    //并且将该单键加入处理
                    _new_multi_key.CanTriggerNode.Add(_key_inst.Id);
                }
                else if (keyType == MapKeyType.OR)
                {
                    //OR关系,DAG上级关系反向
                    //parent是DAG的下级,关联上自身作为上级
                    _new_multi_key.DAGParentKeys.Add(_key_inst);
                    _key_inst.DAGChildKeys.Add(_new_multi_key);
                }
            }

            //通知该key的DAG父节点添加当前节点ID来更新可触发ID表
            //调用自身,顺带把自己也写入
            _new_multi_key.NotifyUpperAddNode(_new_multi_key.Id);

            //最终注册
            Keys.Add(_new_multi_key.Id, _new_multi_key);
            _name_key_map.Add(_new_multi_key.Name!, _new_multi_key);
            _multi_key_map.Add(_new_multi_key.Id, _new_multi_key);
            multi_key_runtime_id = _new_multi_key.Id;
            _created_multi_key_cache.Add(_key_hash, _new_multi_key.Id);

            return true;

        }

        /// <summary>
        /// 尝试获取层级桥接key
        /// </summary>
        /// <param name="bridge_key_runtime_id"></param>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public bool TryCreateBridgeKey(out long bridge_key_runtime_id, long current, long next, int current_level)
        {
            //0层无效[从1层开始计算]
            if (current_level == 0)
            {
                bridge_key_runtime_id = -1;
                return false;
            }

            if (Keys.TryGetValue(current, out IKey current_key) && Keys.TryGetValue(next, out IKey next_level_key))
            {
                if (current_key is ISimpleNode _nodel && next_level_key is ISimpleNode _noder)
                {
                    var _key_hash = $"B|{_hashids.EncodeLong(current, next)}";
                    if (_created_bridge_key_cache.TryGetValue(_key_hash, out bridge_key_runtime_id))
                    {
                        return true;
                    }

                    var _new_bridge_node = new BridgeKey(IdCreator.Instance.GetId(), _key_hash, current_level, _nodel, _noder);

                    //获取对应当前层的节点的对应当前层级的响应节点表
                    if (!_nodel.LevelTriggerNodes.TryGetValue(current_level, out var relate_nodes_c))
                    {
                        //如果无表,说明未初始化,主动收集对应DAG子节点的当前层响应表
                        _nodel.CollectDAGChildsTriggerNodes(current_level);
                        //写入自身+通知DAG父节点所有跃升到当前层的节点该节点可触发
                        _nodel.NotifyDAGUpperNodeToLevel(current_level, _nodel.Id);
                        _nodel.LevelTriggerNodes.TryGetValue(current_level, out relate_nodes_c);
                    }

                    //获取对应下一层的节点的对应当前层级的响应节点表
                    var _next_level = current_level + 1;
                    if (!_noder.LevelTriggerNodes.TryGetValue(_next_level, out var relate_nodes_n))
                    {
                        _noder.CollectDAGChildsTriggerNodes(_next_level);
                        _noder.NotifyDAGUpperNodeToLevel(_next_level, _noder.Id);
                        _noder.LevelTriggerNodes.TryGetValue(_next_level, out relate_nodes_n);
                    }

                    //获取能唤醒该节点的列表
                    var _will_active_current_nodes = Keys.Values.
                        Where(x =>
                            x is IBridgeKey _bkey
                            && _bkey.JumpLevel == _new_bridge_node.JumpLevel
                            && _bkey.Current.LevelTriggerNodes.TryGetValue(_bkey.JumpLevel, out var _parent_node_list_l)
                            && _parent_node_list_l.Contains(_new_bridge_node.Current.Id)
                            && _bkey.Next.LevelTriggerNodes.TryGetValue(_bkey.JumpLevel + 1, out var _parent_node_list_r)
                            && _parent_node_list_r.Contains(_new_bridge_node.Next.Id)
                        ).Select(x => x as IBridgeKey);

                    //获取该节点可唤醒的节点列表
                    var _will_be_active_by_current_nodes = Keys.Values.
                        Where(x =>
                            x is IBridgeKey _bkey
                            && _bkey.JumpLevel == _new_bridge_node.JumpLevel
                            && relate_nodes_c.Contains(_bkey.Current.Id)
                            && relate_nodes_n.Contains(_bkey.Next.Id)
                        ).Select(x => x as IBridgeKey);

                    //重组关联
                    foreach (var _parent_node in _will_active_current_nodes)
                    {
                        //如果该父节点不是其他任何父节点的子节点
                        if (!_will_active_current_nodes.Any(x => x!.Id != _parent_node!.Id && x.DAGChildKeys.Contains(_parent_node)))
                        {
                            //从父节点去除即将与该节点关联的节点
                            foreach (var _remove_node in _will_be_active_by_current_nodes.Where(x => x!.DAGParentKeys.Contains(_parent_node!)))
                            {
                                _parent_node!.DAGChildKeys.Remove(_remove_node!);
                                _remove_node!.DAGParentKeys.Remove(_parent_node!);
                            }
                            //将当前节点注册到对应的父节点的子节点下
                            _parent_node!.DAGChildKeys.Add(_new_bridge_node);
                            _new_bridge_node.DAGParentKeys.Add(_parent_node!);
                        }
                    }

                    _new_bridge_node.NotifyUpperAddNode(_new_bridge_node.Id);

                    foreach (var _child_node in _will_be_active_by_current_nodes)
                    {
                        _child_node!.DAGParentKeys.Add(_new_bridge_node);
                        _new_bridge_node.DAGChildKeys.Add(_child_node);
                        _new_bridge_node.CanTriggerNode.UnionWith(_child_node.CanTriggerNode);
                    }

                    Keys.Add(_new_bridge_node.Id, _new_bridge_node);
                    bridge_key_runtime_id = _new_bridge_node.Id;
                    _created_bridge_key_cache.Add(_key_hash, _new_bridge_node.Id);
                    return true;
                }
                else
                    throw new ArgumentException(message: "Not Allow create bridge key with bridge key node");
            }

            bridge_key_runtime_id = -1;
            return false;
        }

        /// <summary>
        /// 注册新的层级关系键
        /// </summary>
        /// <param name="level_key_runtime_id"></param>
        /// <param name="register_bridge_key_ids">层级关联--顺序等价于层级联系</param>
        /// <returns></returns>
        public bool TryRegisterLevelKey(out long level_key_runtime_id, params long[] register_bridge_key_ids)
        {
            level_key_runtime_id = -1;
            for (int i = 0; i < register_bridge_key_ids.Length - 1; i++)
                if (Keys.TryGetValue(register_bridge_key_ids[i], out var key1) && Keys.TryGetValue(register_bridge_key_ids[i + 1], out var key2))
                    if (key1 is IBridgeKey _key1 && key2 is IBridgeKey _key2)
                        if (_key1.Next.Id != _key2.Current.Id)
                            throw new InvalidOperationException(message: $"Not support connect diff key link({_key1}-{_key2})");
                        else
                            return false;
                    else
                        return false;
            var key_hash = $"L-{_hashids.EncodeLong(register_bridge_key_ids)}";
            if (_created_level_key_cache.TryGetValue(key_hash, out level_key_runtime_id))
                return true;

            Keys.TryGetValue(register_bridge_key_ids.Last(), out var last_key);

            var _new_level_node = new LevelKey(IdCreator.Instance.GetId(), key_hash, (last_key as IBridgeKey)!.JumpLevel + 1, register_bridge_key_ids);

            //重构关系
            //获取即将成为子关系的父集键列表
            //var _will_active_current_nodes = Keys.Values.
            //    Where(x =>
            //    x is ILevelKey _lkey//必须是层阶关系键
            //    && _lkey.KeySequence.Length >= _new_level_node.KeySequence.Length//通路长度大于等于当前节点的才考虑,小于的一定无法触发
            //    //循环当前节点的key序列,每个序列key的位置和目标键的同位置序列进行比较,必须全部是目标键key序列对应位置key包含当前key的对应位置的key
            //    && !(_new_level_node.KeySequence
            //        .Select((x, i) => (value: x, index: i))
            //        .Any(y =>
            //            !Keys.TryGetValue(_lkey.KeySequence[y.index], out var _target_key)
            //            || !_target_key.CanTriggerNode.Contains(y.value)))
            //    );

            var _parent_nodes = new HashSet<long>();
            var _ignore_parents_nodes = new HashSet<long>();
            var _stack_cache = new Stack<ILevelKey>();
            var _child_nodes = new HashSet<long>();
            var _ignore_child_nodes = new HashSet<long>();
            var _new_node_seq = _new_level_node.KeySequence.Select(x => { Keys.TryGetValue(x, out var _truth_key); return _truth_key as IBridgeKey; }).ToArray();

            foreach (var node in Keys.Values.OfType<ILevelKey>())
            {
                //可能为父节点
                //最终将统计出当前节点所有可能的最近父集节点组
                if (node.KeySequence.Length > _new_level_node.KeySequence.Length)
                {
                    if (_ignore_parents_nodes.Contains(node.Id))
                        //存在于要求忽略的父节点中,跳过
                        continue;
                    //无论当前节点能否通过,将其父节点全部丢入忽略的parentnodes中
                    _stack_cache.Push(node);
                    while (_stack_cache.Count > 0)
                    {
                        if (_stack_cache.TryPop(out var _pnode))
                        {
                            foreach (var _parent in _pnode.DAGParentKeys)
                            {
                                if (_parent is ILevelKey _parent_level)
                                {
                                    if (_ignore_parents_nodes.Contains(_parent.Id))
                                        continue;
                                    _stack_cache.Push(_parent_level);
                                    _ignore_parents_nodes.Add(_parent_level.Id);
                                    //Keys无序,检查是否之前有存在,有的话删了
                                    if (_parent_nodes.Contains(_parent_level.Id))
                                        _parent_nodes.Remove(_parent_level.Id);
                                }
                            }
                        }
                    }

                    _parent_nodes.Add(node.Id);
                    for (int i = 0; i < _new_level_node.KeySequence.Length; i++)
                    {
                        //节点ID相同,直接通过
                        if (node.KeySequence[i] == _new_node_seq[i]!.Id)
                            continue;
                        if (Keys.TryGetValue(node.KeySequence[i], out var _parent_seq_key))
                        {
                            //任意序列位置不包含,直接跳出
                            if (!_parent_seq_key.CanTriggerNode.Contains(_new_level_node.KeySequence[i]))
                            {
                                _parent_nodes.Remove(node.Id);
                                _ignore_parents_nodes.Add(node.Id);
                                break;
                            }
                        }
                    }
                }
                //可能为子节点
                //最终将统计出当前节点所有可能的最近子集节点组
                else if (node.KeySequence.Length < _new_level_node.KeySequence.Length)
                {
                    if (_ignore_child_nodes.Contains(node.Id))
                        //存在于要求忽略的子节点中,跳过
                        continue;
                    //无论当前节点能否通过,将其子节点全部丢入忽略的parentnodes中
                    _stack_cache.Push(node);
                    while (_stack_cache.Count > 0)
                    {
                        if (_stack_cache.TryPop(out var _pnode))
                        {
                            foreach (var _child in _pnode.DAGChildKeys)
                            {
                                if (_child is ILevelKey _child_level)
                                {
                                    if (_ignore_child_nodes.Contains(_child.Id))
                                        continue;
                                    _stack_cache.Push(_child_level);
                                    _ignore_child_nodes.Add(_child_level.Id);
                                    //Keys无序,检查是否之前有存在,有的话删了
                                    if (_child_nodes.Contains(_child_level.Id))
                                        _child_nodes.Remove(_child_level.Id);
                                }
                            }
                        }
                    }

                    _child_nodes.Add(node.Id);
                    for (int i = 0; i < node.KeySequence.Length; i++)
                    {
                        //节点ID相同,直接通过
                        if (node.KeySequence[i] == _new_node_seq[i]!.Id)
                            continue;
                        //任意序列位置不包含,直接跳出
                        if (!_new_node_seq[i]!.CanTriggerNode.Contains(node.KeySequence[i]))
                        {
                            _child_nodes.Remove(node.Id);
                            _ignore_child_nodes.Add(node.Id);
                            break;
                        }
                    }
                }
                //序列长度相等 不好说
                else
                {
                    if (_ignore_parents_nodes.Contains(node.Id))
                        continue;

                    //-1=child,1=parent
                    int _add_to_where = 0;

                    for (int i = 0; i < node.KeySequence.Length; i++)
                    {
                        //节点ID相同,直接通过
                        if (node.KeySequence[i] == _new_node_seq[i]!.Id)
                            continue;

                        bool _be_parent = false;
                        //当前节点的序列X包含目标节点的序列X,该节点应该作child
                        bool _be_child = _new_node_seq[i]!.CanTriggerNode.Contains(node.KeySequence[i]);
                        if (!_be_child)
                        {
                            Keys.TryGetValue(node.KeySequence[i], out var _parent_seq_key);
                            //目标节点的序列X包含当前节点的序列X,该节点应该作parent
                            _be_parent = _parent_seq_key.CanTriggerNode.Contains(_new_node_seq[i]!.Id);
                        }
                        //和当前节点无关联,跳出
                        //只能判断出该节点和此节点无关
                        if (!_be_child && !_be_parent)
                        {
                            _ignore_parents_nodes.Add(node.Id);
                            break;
                        }

                        if (_add_to_where != 0)
                        {
                            //前者节点关系是子当前是父,或者前者是父当前是子,存在通路交叉,跳出
                            if ((_add_to_where < 0 && _be_parent) || (_add_to_where > 0 && _be_child))
                            {
                                _ignore_parents_nodes.Add(node.Id);
                                break;
                            }
                        }
                        else
                        {
                            _add_to_where = _be_child ? -1 : 1;
                        }
                    }
                    if (!_ignore_parents_nodes.Contains(node.Id))
                    {
                        if (_add_to_where > 0)
                            _parent_nodes.Add(node.Id);
                        else if (_add_to_where < 0)
                            _child_nodes.Add(node.Id);
                        else
                            throw new InvalidCastException(message: "Connect level key error:equal compare boom!");
                    }
                }

                //构建关系树
                foreach (var parent_node in _parent_nodes)
                {
                    Keys.TryGetValue(parent_node, out var _parent);
                    foreach (var _disconnect_node in _parent.DAGChildKeys.Where(x => _child_nodes.Contains(x.Id)))
                    {
                        _disconnect_node.DAGParentKeys.Remove(_parent);
                        _parent.DAGChildKeys.Remove(_disconnect_node);
                    }
                    _parent.DAGChildKeys.Add(_new_level_node);
                    _new_level_node.DAGParentKeys.Add(_parent);
                }

                foreach (var child_node in _child_nodes)
                {
                    Keys.TryGetValue(child_node, out var _child);
                    _child.DAGParentKeys.Add(_new_level_node);
                    _new_level_node.DAGChildKeys.Add(_child);
                    _new_level_node.CanTriggerNode.UnionWith(_child.CanTriggerNode);
                }

                Keys.Add(_new_level_node.Id, _new_level_node);
                level_key_runtime_id = _new_level_node.Id;
                _created_bridge_key_cache.Add(key_hash, _new_level_node.Id);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 给定枚举类型转换为运行时ID
        /// </summary>
        /// <typeparam name="T">枚举项</typeparam>
        /// <param name="value">实际ID</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        internal long Convert<T>(T value)
            where T : struct, Enum
        {
            if (!_name_group_map.TryGetValue(typeof(T).FullName, out var group))
                throw new ArgumentException(message: $"{typeof(T)} not be injected into global map.");

            var _origin_value = Enums.GetMember(value)?.ToInt64();

            if (_origin_value is null)
                throw new ArgumentException(message: $"{value} not Find in Global Enums Cache,it should't be happened.");

            if (!(group as IEnumGroup)!.RelateKeys.TryGetValue(_origin_value.Value, out var key))
                throw new ArgumentException(message: $"{value} not Find in Register Enum {(group as KeyGroupUnit)!.Name} Cache.");
            return key.Id;
        }

        /// <summary>
        /// 给定项名称转换为运行时ID
        /// </summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        internal long Convert(string name)
        {
            if (!_name_key_map.TryGetValue(name, out var key))
                throw new ArgumentException(message: $"Not Find {name} enum value in convert cache");
            return key.Id;
        }

        /// <summary>
        /// 给定枚举类型转换为运行时ID
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        internal bool TryConvert<T>(T value, out long id)
            where T : struct, Enum
        {
            id = -1;
            if (!_name_group_map.TryGetValue(typeof(T).FullName, out var group))
            {
                _logger.LogWarning(message: $"{typeof(T)} not be injected into global map.");
                return false;
            }


            var _origin_value = Enums.GetMember(value)?.ToInt64();

            if (_origin_value is null)
            {
                _logger.LogWarning(message: $"{value} not Find in Global Enums Cache,it should't be happened.");
                return false;
            }


            if (!(group as IEnumGroup)!.RelateKeys.TryGetValue(_origin_value.Value, out var key))
            {
                _logger.LogWarning(message: $"{value} not Find in Register Enum {(group as KeyGroupUnit)!.Name} Cache.");
                return false;
            }

            id = key.Id;
            return true;
        }

        /// <summary>
        /// 给定枚举类型转换为运行时ID并返回实际key
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        internal bool TryConvert<T>(T value, out IKey key)
            where T : struct, Enum
        {
            if (!_name_group_map.TryGetValue(typeof(T).FullName, out var group))
            {
                _logger.LogWarning(message: $"{typeof(T)} not be injected into global map.");
                key = default!;
                return false;
            }

            var _origin_value = Enums.GetMember(value)?.ToInt64();

            if (_origin_value is null)
            {
                _logger.LogWarning(message: $"{value} not Find in Global Enums Cache,it should't be happened.");
                key = default!;
                return false;
            }


            if (!(group as IEnumGroup)!.RelateKeys.TryGetValue(_origin_value.Value, out key))
            {
                _logger.LogWarning(message: $"{value} not Find in Register Enum {(group as KeyGroupUnit)!.Name} Cache.");
                key = default!;
                return false;
            }
            return true;
        }

        /// <summary>
        /// 给定项名称转换为运行时ID
        /// </summary>
        /// <param name="name"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        internal bool TryConvert(string name, out long id)
        {
            if (!_name_key_map.TryGetValue(name, out var key))
            {
                _logger.LogWarning(message: $"Not Find {name} enum value in convert cache");
                id = -1;
                return false;
            }

            id = key.Id;
            return true;
        }

        /// <summary>
        /// 给定项名称转换为运行时ID并返回实际key
        /// </summary>
        /// <param name="name"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        internal bool TryConvert(string name, out IKey key)
        {
            if (!_name_key_map.TryGetValue(name, out key))
            {
                _logger.LogWarning(message: $"Not Find {name} enum value in convert cache");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 导出所有注册的Key详情
        /// </summary>
        public string OutPutKeysInfo()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("************Node List************");
            foreach (var key in Keys.Values)
                sb.AppendLine(key.ToString());
            return sb.ToString();
        }

        /// <summary>
        /// 绘制整体注册的Key和他们唤醒关系
        /// </summary>
        /// <param name="ignore_single_node">忽略无关联的单一节点</param>
        /// <returns>对应graphviz的节点图表代码</returns>
        public string OutPutGraphviz(bool ignore_single_node = false)
        {
            StringBuilder _graphviz_graph = new StringBuilder();
            StringBuilder _graphviz_nodes=new StringBuilder();
            StringBuilder _graphviz_connection_line = new StringBuilder();
            _graphviz_graph.AppendLine("************Copy Code************");
            _graphviz_graph.AppendLine("digraph G {");

            if (ignore_single_node)
            {
                HashSet<long> _has_line_keys = new HashSet<long>();

                foreach (var key in Keys.Values)
                {
                    foreach (var _line in key.DAGParentKeys)
                    {
                        _graphviz_connection_line.AppendLine($"{_line.Id} -> {key.Id};");
                        _has_line_keys.Add(_line.Id);
                        _has_line_keys.Add(key.Id);
                    }
                }
                foreach(var key in Keys.Values)
                {
                    if(_has_line_keys.Contains(key.Id))
                        _graphviz_nodes.AppendLine(key.ToGraphvizNodeString());
                }
            }
            else
            {
                foreach (var key in Keys.Values)
                {
                    _graphviz_nodes.AppendLine(key.ToGraphvizNodeString());
                    foreach (var _line in key.DAGParentKeys)
                        _graphviz_connection_line.AppendLine($"{_line.Id} -> {key.Id};");
                }
            }

            _graphviz_graph.Append(_graphviz_nodes);
            _graphviz_graph.Append(_graphviz_connection_line);
            _graphviz_graph.AppendLine("}");

            return _graphviz_graph.ToString();
        }
    }
}
