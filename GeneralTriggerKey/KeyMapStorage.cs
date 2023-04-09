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
        internal bool TryGetOrAddSingleKey(out long _runtime_add_key_id, string callName, string range = "GLOBAL", bool force_add = false)
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
        /// <param name="keyType">联合键类型</param>
        /// <param name="only_try">仅限尝试注册获取,如果不存在则返回失败</param>
        /// <returns></returns>
        internal bool TryRegisterMultiKey(out long multi_key_runtime_id, MapKeyType keyType, long[] register_key_ids, bool only_try = false)
        {
            if (register_key_ids.Length == 0)
            {
                multi_key_runtime_id = -1;
                return false;
            }

            var _distinct_and_sort_ids = register_key_ids.Distinct().OrderBy(x => x);

            var _key_hash = $"{Enums.GetName(keyType)}-{_hashids.EncodeLong(_distinct_and_sort_ids)}";
            if (_created_multi_key_cache.TryGetValue(_key_hash, out multi_key_runtime_id))
                return true;
            else
            {
                if (only_try)
                {
                    {
                        multi_key_runtime_id = -1;
                        return false;
                    }
                }
            }

            var _fact_relate_all_single_keys = new HashSet<long>();

            if (keyType == MapKeyType.AND)
            {
                foreach (var _id in _distinct_and_sort_ids)
                {
                    if (Keys.TryGetValue(_id, out var _exist_key))
                    {

                        if (_exist_key.IsMultiKey)
                        {
                            //AND关系插入时不支持Or关系
                            if ((_exist_key as IMultiKey)!.KeyRelateType != MapKeyType.AND)
                                throw new ArgumentException(message: $"{_id} is [or relate] but in [and relate] list,can't create (A|B)&C multiKey.");
                            _fact_relate_all_single_keys.UnionWith((_exist_key as IMultiKey)!.RelateSingleKeys);
                        }
                        else
                            _fact_relate_all_single_keys.Add(_id);
                    }
                    else
                        throw new ArgumentException(message: $"{_id} not an avilable runtime key for map");
                }
            }
            else if (keyType == MapKeyType.OR)
            {
                foreach (var _id in _distinct_and_sort_ids)
                {
                    if (Keys.TryGetValue(_id, out var _exist_key))
                    {
                        if (_exist_key.IsMultiKey && (_exist_key as IMultiKey)!.KeyRelateType == MapKeyType.OR)
                            _fact_relate_all_single_keys.UnionWith((_exist_key as IMultiKey)!.RelateSingleKeys);
                        else
                            _fact_relate_all_single_keys.Add(_id);
                    }
                    else
                        throw new ArgumentException(message: $"{_id} not an avilable runtime key for map");
                }
            }
            else
                throw new InvalidOperationException(message: $"Can't register multi key for type {keyType}");


            var _after_order = _fact_relate_all_single_keys.OrderBy(x => x);
            //排序，生成唯一hashid
            string _after_hash = $"M={Enums.GetName(keyType)}-{_hashids.EncodeLong(_after_order)}";
            //判断是否是已存在联合键值
            if (_name_key_map.TryGetValue(_after_hash, out var _key))
            {
                multi_key_runtime_id = _key.Id;
                return true;
            }


            var _new_multi_key = new MulitKey(id: IdCreator.Instance.GetId(), keyType: keyType, relatesinglekeys: _fact_relate_all_single_keys, name: _after_hash, _relate_keys: _after_order);


            var _need_add_current_to_child_keys_keys = _multi_key_map.Values
                    .Where(x =>
                    x.KeyRelateType == keyType &&
                    x.RelateSingleKeys.Count > _fact_relate_all_single_keys.Count &&
                    x.RelateSingleKeys.IsSupersetOf(_fact_relate_all_single_keys));

            var _need_add_current_to_parent_keys_keys = _multi_key_map.Values
                    .Where(x =>
                    x.KeyRelateType == keyType &&
                    x.RelateSingleKeys.Count < _fact_relate_all_single_keys.Count &&
                    _fact_relate_all_single_keys.IsSupersetOf(x.RelateSingleKeys));


            foreach (var _add_key_to_child in _need_add_current_to_child_keys_keys)
            {

                foreach (var _key_remove in _add_key_to_child.ChildKeys.Values.Where(x => x.IsMultiKey && _need_add_current_to_parent_keys_keys.Any(y => y.Id == x.Id)))
                {
                    _add_key_to_child.ChildKeys.Remove(_key_remove.Id);
                    (_key_remove as IMultiKey)!.ParentKeys.RemoveAll(x => x.Id == _add_key_to_child.Id);
                    if (keyType == MapKeyType.AND)
                        _key_remove.DAGParentKeys.RemoveAll(x => x.Id == _add_key_to_child.Id);
                    else if (keyType == MapKeyType.OR)
                        _add_key_to_child.DAGParentKeys.RemoveAll(x => x.Id == _key_remove.Id);
                }

                foreach (var _key_remove_single in _add_key_to_child.ChildKeys.Values.Where(x => !x.IsMultiKey && _fact_relate_all_single_keys.Contains(x.Id)))
                {
                    _add_key_to_child.ChildKeys.Remove(_key_remove_single.Id);
                    if (keyType == MapKeyType.AND)
                        _key_remove_single.DAGParentKeys.RemoveAll(x => x.Id == _add_key_to_child.Id);
                    else if (keyType == MapKeyType.OR)
                        _add_key_to_child.DAGParentKeys.RemoveAll(x => x.Id == _key_remove_single.Id);
                }

                _add_key_to_child.ChildKeys.Add(_new_multi_key.Id, _new_multi_key);
                _new_multi_key.ParentKeys.Add(_add_key_to_child);

                if (keyType == MapKeyType.AND)
                    _new_multi_key.DAGParentKeys.Add(_add_key_to_child);
                else if (keyType == MapKeyType.OR)
                {
                    _add_key_to_child.DAGParentKeys.Add(_new_multi_key);
                    _new_multi_key.CanTriggerNode.UnionWith(_add_key_to_child.CanTriggerNode);
                    _new_multi_key.CanTriggerNode.Add(_add_key_to_child.Id);
                }
            }

            var _temp_set = new HashSet<long>();

            foreach (var _add_key_to_parent in _need_add_current_to_parent_keys_keys)
            {
                if (_need_add_current_to_parent_keys_keys.Any(x => x.Contains(_add_key_to_parent.Id)))
                    continue;
                _new_multi_key.ChildKeys.Add(_add_key_to_parent.Id, _add_key_to_parent);
                _add_key_to_parent.ParentKeys.Add(_new_multi_key);
                _temp_set.UnionWith(_add_key_to_parent.RelateSingleKeys);

                if (keyType == MapKeyType.AND)
                {
                    _add_key_to_parent.DAGParentKeys.Add(_new_multi_key);
                    _new_multi_key.CanTriggerNode.UnionWith(_add_key_to_parent.CanTriggerNode);
                    _new_multi_key.CanTriggerNode.Add(_add_key_to_parent.Id);
                }
                else if (keyType == MapKeyType.OR)
                    _new_multi_key.DAGParentKeys.Add(_add_key_to_parent);
            }

            _temp_set.SymmetricExceptWith(_fact_relate_all_single_keys);
            foreach (var key in _temp_set)
            {
                var _key_inst = Keys.GetValueOrDefault(key);
                _new_multi_key.ChildKeys.Add(key, _key_inst);
                if (keyType == MapKeyType.AND)
                {
                    _key_inst.DAGParentKeys.Add(_new_multi_key);
                    _new_multi_key.CanTriggerNode.UnionWith(_key_inst.CanTriggerNode);
                    _new_multi_key.CanTriggerNode.Add(_key_inst.Id);
                }
                else if (keyType == MapKeyType.OR)
                    _new_multi_key.DAGParentKeys.Add(_key_inst);
            }

            _new_multi_key.NotifyUpperAddNode(_new_multi_key.Id);

            Keys.Add(_new_multi_key.Id, _new_multi_key);
            _name_key_map.Add(_new_multi_key.Name!, _new_multi_key);
            _multi_key_map.Add(_new_multi_key.Id, _new_multi_key);
            multi_key_runtime_id = _new_multi_key.Id;
            _created_multi_key_cache.Add(_key_hash, _new_multi_key.Id);

            return true;
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
