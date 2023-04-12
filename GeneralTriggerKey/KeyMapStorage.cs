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
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;

namespace GeneralTriggerKey
{
    internal sealed class KeyMapStorage
    {
        #region Instance
        private static KeyMapStorage s_instance = default!;
        private readonly ILogger _logger;
        private readonly Hashids _hashIds;
        internal static KeyMapStorage Instance
        {
            get
            {
                return s_instance ??= new KeyMapStorage();
            }
        }
        private KeyMapStorage()
        {
            _logger = GLogger.Instance.GetLogger<KeyMapStorage>();
            //hashids 协同IDCreator的创建key
            _hashIds = new Hashids(IdCreator.Instance.IdTimeSeed.ToString());
            //注册恒定的ANY节点
            AutoInjectEnumsMapping(typeof(KeyMapStorage).Assembly);
            TryConvert(StorageKey.ANY,out IKey addNode);
            AnyTypeCode = addNode;
            AnyTypeCode.CanTriggerNode.Add(addNode.Id);
        }
        #endregion

        #region Prop
        internal Dictionary<long, IKey> Keys { get; private set; } = new Dictionary<long, IKey>();
        internal Dictionary<long, IGroup> Groups { get; private set; } = new Dictionary<long, IGroup>();
        internal IKey AnyTypeCode { get;private set; }
        #endregion

        #region Field
        private readonly Dictionary<string, IKey> nameKeyMap = new Dictionary<string, IKey>();
        private readonly Dictionary<string, IGroup> _nameGroupMap = new Dictionary<string, IGroup>();
        private readonly Dictionary<long, IMultiKey> _multiKeyMap = new Dictionary<long, IMultiKey>();//key是运行时id
        private readonly TypeCode[] _supportEnumType = new TypeCode[] {
            TypeCode.Byte,
            TypeCode.SByte,
            TypeCode.UInt16,
            TypeCode.UInt32,
            TypeCode.UInt64,
            TypeCode.Int16,
            TypeCode.Int32,
            TypeCode.Int64
        };
        private readonly Dictionary<string, long> _createdMultiKeyCache = new Dictionary<string, long>();
        private readonly Dictionary<string, long> _createdBridgeKeyCache = new Dictionary<string, long>();
        private readonly Dictionary<string, long> _createdLevelKeyCache = new Dictionary<string, long>();
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
            var useMarkEnumsTypes = assembly.GetClassWithAttributeType<MapEnumAttribute>();
            if (useMarkEnumsTypes is null)
            {
                _logger.LogDebug($"No require map enums find in {assembly.FullName}");
                return;
            }
            _logger.LogDebug($"-Detect {useMarkEnumsTypes!.Count} Enums To Add.");
            foreach (var typeInfo in useMarkEnumsTypes)
            {
                var enumType = typeInfo.ClassInfo;

                if (!_supportEnumType.Contains(Enums.GetTypeCode(enumType)))
                {
                    _logger.LogError($"Enum {nameof(enumType)} not support value [int16/32/64],it's {Enums.GetTypeCode(enumType)}");
                    continue;
                }
                _logger.LogDebug($"--Try register {enumType.FullName}");


                if (_nameGroupMap.ContainsKey(enumType.FullName))
                {
                    _logger.LogWarning($"--{enumType.FullName} already registered,skip");
                    continue;
                }
                var newEnumGroup = new EnumGroup(id: IdCreator.Instance.GetId(), enumType: enumType, aliaName: enumType.Name);
                Groups.Add(newEnumGroup.Id, newEnumGroup);
                _nameGroupMap.Add(newEnumGroup.Name!, newEnumGroup);

                foreach (var member in Enums.GetMembers(enumType))
                {
                    var memberFullName = $"{enumType.FullName}-{member.Name}";

                    if (nameKeyMap.ContainsKey(memberFullName))
                    {
                        _logger.LogError($"---{memberFullName} already registered,skip");
                        continue;
                    }

                    _logger.LogDebug($"---Register {memberFullName}");
                    var enumMember = new EnumKey(id: IdCreator.Instance.GetId(), originId: member.ToInt64(), belongGroup: newEnumGroup, name: memberFullName);

                    Keys.Add(enumMember.Id, enumMember);
                    newEnumGroup.RelateKeys.Add(enumMember.OriginId, enumMember);

                    nameKeyMap.Add(enumMember.Name!, enumMember);

                    //var _tname_member_name = $"{_enumType.Name}_{_member.Name}";

                    if (nameKeyMap.TryAdd(member.Name, enumMember))
                    {
                        enumMember.Alias.Add(member.Name);
                    }
                    else
                    {
                        _logger.LogError($"---Failed register {member.Name} because it's already exist.");
                    }

                    foreach (var attribute in member.Attributes.OfType<EnumAliaAttribute>())
                    {
                        foreach (var name in attribute.Names)
                        {
                            if (!nameKeyMap.TryAdd(name, enumMember))
                            {
                                _logger.LogError($"---Unable add alia name {name} for {memberFullName} because it's already exist.");
                                continue;
                            }
                            enumMember.Alias.Add(name);
                        }
                    }

                    if (AnyTypeCode != null)
                    {
                        //默认挂载ANY节点
                        enumMember.DAGChildKeys.Add(AnyTypeCode);
                        enumMember.CanTriggerNode.Add(AnyTypeCode.Id);
                        AnyTypeCode.DAGParentKeys.Add(enumMember);
                    }
                }
            }
        }

        /// <summary>
        /// 运行时增添key
        /// </summary>
        /// <param name="callName">key名称</param>
        /// <param name="range">Key隶属域</param>
        /// <param name="forceAdd">是否强制添加</param>
        /// <returns></returns>
        public bool TryGetOrAddSingleKey(out long runtimeAddKeyId, string callName, string range = "GLO", bool forceAdd = false)
        {
            if (nameKeyMap.TryGetValue(callName, out var existKey))
            {
                if (existKey is RunTimeKey && !forceAdd)
                {
                    runtimeAddKeyId = existKey.Id;
                    return true;
                }
                _logger.LogWarning($"Find exist key for {range}-{callName} runtime key,but not set force add.");
                runtimeAddKeyId = -1;
                return false;
            }

            var newRuntimeSingleKey = new RunTimeKey(id: IdCreator.Instance.GetId(), name: callName, range: range);

            //默认挂载ANY节点
            if (AnyTypeCode != null)
            {
                newRuntimeSingleKey.DAGChildKeys.Add(AnyTypeCode);
                newRuntimeSingleKey.CanTriggerNode.Add(AnyTypeCode.Id);
                AnyTypeCode.DAGParentKeys.Add(newRuntimeSingleKey);
            }
            nameKeyMap.Add(newRuntimeSingleKey.Name!, newRuntimeSingleKey);
            nameKeyMap.Add(callName, newRuntimeSingleKey);
            Keys.Add(newRuntimeSingleKey.Id, newRuntimeSingleKey);

            runtimeAddKeyId = newRuntimeSingleKey.Id;
            return true;
        }

        /// <summary>
        /// 注册新的联合键值
        /// </summary>
        /// <param name="registerKeyIds">运行时id</param>
        /// <returns></returns>
        public bool TryRegisterMultiKey(out long multiKeyRuntimeId, MapKeyType keyType, long[] registerKeyIds, bool onlyTry = false)
        {
            multiKeyRuntimeId = -1;
            if (registerKeyIds.Length == 0)
                return false;

            if (registerKeyIds.Length == 1)
            {
                if (Keys.TryGetValue(registerKeyIds[0], out var outSingleKey))
                {
                    multiKeyRuntimeId = outSingleKey.Id;
                    return true;
                }
                else return false;
            }

            //获取实际上的所有单一键
            var factRelateAllSingleKeys = new HashSet<long>();
            if (keyType == MapKeyType.AND)
            {
                foreach (var rKeyId in registerKeyIds)
                {
                    if (Keys.TryGetValue(rKeyId, out var exist_key))
                    {
                        if (exist_key is ILevelKey || exist_key is IBridgeKey)
                            return false;
                        if (exist_key.Id == AnyTypeCode.Id)
                        {
                            multiKeyRuntimeId = AnyTypeCode.Id;
                            return true;
                        }

                        //针对联合key和单一key不同处理
                        if (exist_key.IsMultiKey)
                        {
                            //AND关系插入时不支持Or关系
                            if ((exist_key as IMultiKey)!.KeyRelateType != MapKeyType.AND)
                                throw new ArgumentException(message: $"{rKeyId} is [or relate] but in [and relate] list,can't create (A|B)&C multiKey.");
                            //联合key需要获取其所有的关联的非联合key项
                            //取二者并集
                            factRelateAllSingleKeys.UnionWith((exist_key as IMultiKey)!.RelateSingleKeys);
                        }
                        else
                        {
                            factRelateAllSingleKeys.Add(rKeyId);
                        }
                    }
                    else
                    {
                        //不存在的key记录无法操作
                        throw new ArgumentException(message: $"{rKeyId} not an avilable runtime key for map");
                    }
                }
            }
            else if (keyType == MapKeyType.OR)
            {
                //Or关联
                //Single对应了单例键+AND关系的复合键
                //AND关系复合键不会再拆解,而是仅用当前AND复合键
                //获取实际上的所有单例键+AND关系复合键
                foreach (var rKeyId in registerKeyIds)
                {
                    if (Keys.TryGetValue(rKeyId, out var exist_key))
                    {
                        if (exist_key is ILevelKey || exist_key is IBridgeKey)
                            return false;
                        if (exist_key.Id == AnyTypeCode.Id)
                        {
                            multiKeyRuntimeId = AnyTypeCode.Id;
                            return true;
                        }
                        //针对联合key和单一key不同处理
                        //Or关系插入时候,如果键是AND,当作单例键看待,如果键是Or则拆开
                        if (exist_key.IsMultiKey && (exist_key as IMultiKey)!.KeyRelateType == MapKeyType.OR)
                            //联合key需要获取其所有的关联的非联合key项
                            //取二者并集
                            factRelateAllSingleKeys.UnionWith((exist_key as IMultiKey)!.RelateSingleKeys);
                        else
                            factRelateAllSingleKeys.Add(rKeyId);
                    }
                    else
                    {
                        //不存在的key记录无法操作
                        throw new ArgumentException(message: $"{rKeyId} not an avilable runtime key for map");
                    }
                }
            }
            else
                throw new InvalidOperationException(message: $"Can't register multi key for type {keyType}");

            var afterOrderKeys = factRelateAllSingleKeys.OrderBy(x => x);
            //排序，生成唯一hashid
            string keysAfterHash = $"M|{Enums.GetName(keyType)}-{_hashIds.EncodeLong(afterOrderKeys)}";
            //判断是否是已存在联合键值
            if (nameKeyMap.TryGetValue(keysAfterHash, out var existRegisteredKey))
            {
                multiKeyRuntimeId = existRegisteredKey.Id;
                return true;
            }

            if (onlyTry)
            {
                multiKeyRuntimeId = -1;
                return false;
            }

            //预创建
            var newMultiKey = new MulitKey(id: IdCreator.Instance.GetId(),
                keyType: keyType,
                relatesinglekeys: factRelateAllSingleKeys,
                name: keysAfterHash,
                relateKeys: afterOrderKeys);

            var parentCache = new HashSet<long>();
            var childCache = new HashSet<long>();
            var circulateCache = new Stack<IKey>();
            var ignoreCache = new HashSet<long>();
            var shouldIgnoreKeys=new HashSet<long>();

            //获取联合键与当前键的关系
            foreach (var node in _multiKeyMap.Values)
            {
                if (node.KeyRelateType != newMultiKey.KeyRelateType)
                    continue;
                if (ignoreCache.Contains(node.Id))
                    continue;
                //该node是当前节点的超集
                if(node.RelateSingleKeys.Count>newMultiKey.RelateSingleKeys.Count 
                    && node.RelateSingleKeys.IsSupersetOf(newMultiKey.RelateSingleKeys))
                {
                    parentCache.Add(node.Id);
                    //循环所有该节点的祖先节点并将其全部加入忽略节点,同时如果之前加入过父节点记录的话对其进行删除操作
                    circulateCache.Push(node);
                    while (circulateCache.TryPop(out var topSearchNode))
                    {
                        foreach(var parent in topSearchNode.DAGParentKeys)
                        {
                            circulateCache.Push(parent);
                            ignoreCache.Add(parent.Id);
                            if (parentCache.Contains(parent.Id)) parentCache.Remove(parent.Id);
                        }
                    }
                }
                //该node是当前节点的子集
                else if (node.RelateSingleKeys.Count < newMultiKey.RelateSingleKeys.Count
                    && newMultiKey.RelateSingleKeys.IsSupersetOf(node.RelateSingleKeys))
                {
                    childCache.Add(node.Id);
                    //循环所有该节点的孙子节点并将其全部加入忽略节点,同时如果之前加入过子节点记录的话对其进行删除操作
                    circulateCache.Push(node);
                    while (circulateCache.TryPop(out var topSearchNode))
                    {
                        foreach (var child in topSearchNode.DAGChildKeys)
                        {
                            circulateCache.Push(child);
                            ignoreCache.Add(child.Id);
                            if (childCache.Contains(child.Id)) childCache.Remove(child.Id);
                        }
                    }
                }
            }




            //如果是or关系,对应的parent/child关系组反转
            if (newMultiKey.KeyRelateType == MapKeyType.OR) (parentCache, childCache) = (childCache, parentCache);

            var relateChildKeyIgnoreKeyIds = new HashSet<long>();

            //遍历实际父节点
            foreach(var parentId in parentCache)
            {
                Keys.TryGetValue(parentId, out var parent);
                //如果父节点的子节点中存在即将转换为当前节点子节点的节点,断开其连接
                foreach(var needDisconnectChild in parent.DAGChildKeys.Where(x => childCache.Contains(x.Id)))
                {
                    needDisconnectChild.DAGParentKeys.Remove(parent);
                    parent.DAGChildKeys.Remove(needDisconnectChild);
                }
                //当前节点写入父节点
                parent.DAGChildKeys.Add(newMultiKey);
                newMultiKey.DAGParentKeys.Add(parent);

                //如果是OR关系,此时写入的是超集关系的childCache
                //获取对应的Child(Parent)的relatenodes
                if (newMultiKey.KeyRelateType == MapKeyType.OR)
                {
                    //如果是联合键,取其所有关联单键
                    if (parent is IMultiKey multiParent)
                        relateChildKeyIgnoreKeyIds.UnionWith(multiParent.RelateSingleKeys);
                    else
                        relateChildKeyIgnoreKeyIds.Add(parent.Id);
                }
            }
            foreach(var childId in childCache)
            {
                Keys.TryGetValue(childId, out var child);

                child.DAGParentKeys.Add(newMultiKey);
                newMultiKey.DAGChildKeys.Add(child);
                if (newMultiKey.KeyRelateType == MapKeyType.AND)
                {
                    //如果是联合键,取其所有关联单键
                    if (child is IMultiKey multiChild)
                        relateChildKeyIgnoreKeyIds.UnionWith(multiChild.RelateSingleKeys);
                    else
                        relateChildKeyIgnoreKeyIds.Add(child.Id);
                }
            }

            //取交集,此时获取的是不存在于以上连接的节点的列表
            relateChildKeyIgnoreKeyIds.SymmetricExceptWith(newMultiKey.RelateSingleKeys);
            foreach(var keyId in relateChildKeyIgnoreKeyIds)
            {
                //存在单键没有过滤的情况,需要额外判断此轮增添的key是否隶属于父节点,是的话需要丢弃
                Keys.TryGetValue(keyId, out var keyInst);
                if (newMultiKey.KeyRelateType == MapKeyType.AND)
                {
                    keyInst.DAGParentKeys.Add(newMultiKey);
                    newMultiKey.DAGChildKeys.Add(keyInst);
                    foreach(var parent in newMultiKey.DAGParentKeys)
                    {
                        if (parent.DAGChildKeys.Contains(keyInst))
                        {
                            parent.DAGChildKeys.Remove(keyInst);
                            keyInst.DAGParentKeys.Remove(parent);
                        }
                    }


                }
                else if (newMultiKey.KeyRelateType == MapKeyType.OR)
                {
                    keyInst.DAGChildKeys.Add(newMultiKey);
                    newMultiKey.DAGParentKeys.Add(keyInst);
                    foreach (var child in newMultiKey.DAGParentKeys)
                    {
                        if (child.DAGParentKeys.Contains(keyInst))
                        {
                            child.DAGParentKeys.Remove(keyInst);
                            keyInst.DAGChildKeys.Remove(child);
                        }
                    }
                }
            }
            newMultiKey.NotifyUpperAddNode(newMultiKey.Id);

            //如果是OR关系,并且最终重新关联之后自身子节点为空,需要添加一个ANY节点作为触发
            if (newMultiKey.KeyRelateType == MapKeyType.OR && newMultiKey.DAGChildKeys.Count == 0)
            {
                newMultiKey.DAGChildKeys.Add(AnyTypeCode);
                newMultiKey.CanTriggerNode.Add(AnyTypeCode.Id);
                AnyTypeCode.DAGParentKeys.Add(newMultiKey);
                //和当前节点关联的所有节点与ANY的关联全部消去
                foreach(var parent in newMultiKey.DAGParentKeys)
                {
                    parent.DAGChildKeys.Remove(AnyTypeCode);
                    AnyTypeCode.DAGParentKeys.Remove(parent);
                }
            }

            ////注意类别AND OR仅查询对应类别的
            ////需要将当前键添加作为关联子键的所有项【超集】
            //var needAddCurrent2ParentKeys = _multiKeyMap.Values
            //        .Where(x =>
            //        x.KeyRelateType == keyType &&
            //        x.RelateSingleKeys.Count > factRelateAllSingleKeys.Count &&
            //        x.RelateSingleKeys.IsSupersetOf(factRelateAllSingleKeys));

            ////需要将当前键添加作为超集键的所有项【子集】
            //var needAddCurrent2ChildKeys = _multiKeyMap.Values
            //        .Where(x =>
            //        x.KeyRelateType == keyType &&
            //        x.RelateSingleKeys.Count < factRelateAllSingleKeys.Count &&
            //        factRelateAllSingleKeys.IsSupersetOf(x.RelateSingleKeys));




            ////遍历所有超集
            //foreach (var parentKey in needAddCurrent2ParentKeys)
            //{
            //    //添加前删除所有即将作为当前键为子集的键，因为哪些键将关联自身
            //    foreach (var needRemoveChildKey in parentKey.ChildKeys.Values.Where(x => x.IsMultiKey && needAddCurrent2ChildKeys.Any(y => y.Id == x.Id)))
            //    {
            //        parentKey.ChildKeys.Remove(needRemoveChildKey.Id);
            //        (needRemoveChildKey as IMultiKey)!.ParentKeys.RemoveAll(x => x.Id == parentKey.Id);

            //        //处理DAG视角的关联
            //        if (keyType == MapKeyType.AND)
            //        {
            //            //AND关系,从child中删除所有超集,因为AND关系层遵循小AND<-大AND关系,将当前关系插入
            //            needRemoveChildKey.DAGParentKeys.Remove(parentKey);
            //            parentKey.DAGChildKeys.Remove(needRemoveChildKey);
            //        }
            //        else if (keyType == MapKeyType.OR)
            //        {
            //            //OR关系,DAG关系层反向
            //            //Parent断开和child的DAG关系
            //            parentKey.DAGParentKeys.Remove(needRemoveChildKey);
            //            needRemoveChildKey.DAGChildKeys.Remove(parentKey);
            //        }
            //    }
            //    //删除已经在当前键中包含的同时在该超集中存在的单键
            //    foreach (var needRemoveSingleKey in parentKey.ChildKeys.Values.Where(x => !x.IsMultiKey && factRelateAllSingleKeys.Contains(x.Id)))
            //    {
            //        parentKey.ChildKeys.Remove(needRemoveSingleKey.Id);
            //        //处理DAG视角的关联
            //        if (keyType == MapKeyType.AND)
            //        {
            //            //AND关系,从child中删除所有超集,因为AND关系层遵循小Single<-大AND关系,将当前关系插入
            //            needRemoveSingleKey.DAGParentKeys.Remove(parentKey);
            //            parentKey.DAGChildKeys.Remove(needRemoveSingleKey);
            //        }
            //        else if (keyType == MapKeyType.OR)
            //        {
            //            //OR关系,DAG关系层反向
            //            //Parent断开和child的DAG关系
            //            parentKey.DAGParentKeys.Remove(needRemoveSingleKey);
            //            needRemoveSingleKey.DAGChildKeys.Remove(parentKey);
            //        }

            //    }
            //    //将自身添加进超集项的子键集并将该超集添加到该项的父关联
            //    parentKey.ChildKeys.Add(newMultiKey.Id, newMultiKey);
            //    newMultiKey.ParentKeys.Add(parentKey);



            //    //处理DAG视角的关联
            //    if (keyType == MapKeyType.AND)
            //    {
            //        //AND关系,将自身DAG上级关系关联上该_add_key_to_child
            //        newMultiKey.DAGParentKeys.Add(parentKey);
            //        parentKey.DAGChildKeys.Add(newMultiKey);
            //    }
            //    else if (keyType == MapKeyType.OR)
            //    {
            //        //OR关系,DAG上级关系反向
            //        //parent是DAG的下级,关联上自身作为上级
            //        parentKey.DAGParentKeys.Add(newMultiKey);
            //        newMultiKey.DAGChildKeys.Add(parentKey);

            //        //继承下级的触发ids
            //        newMultiKey.CanTriggerNode.UnionWith(parentKey.CanTriggerNode);
            //        //囊括下级
            //        newMultiKey.CanTriggerNode.Add(parentKey.Id);
            //    }
            //}

            //var truthNeedAddedSingleKeys = new HashSet<long>();
            ////遍历所有的子集,对应的超集都已经删除关联完毕,直接将当前键加入对应父关联
            //foreach (var parentKey in needAddCurrent2ChildKeys)
            //{
            //    //检查子集之间关系,如果相互存在包含关系则对于更小的子集忽略加入操作
            //    if (needAddCurrent2ChildKeys.Any(x => x.Contains(parentKey.Id)))
            //        continue;

            //    newMultiKey.ChildKeys.Add(parentKey.Id, parentKey);
            //    parentKey.ParentKeys.Add(newMultiKey);
            //    truthNeedAddedSingleKeys.UnionWith(parentKey.RelateSingleKeys);

            //    //处理DAG视角的关联
            //    if (keyType == MapKeyType.AND)
            //    {
            //        //AND关系,将自身DAG上级关系关联上该_add_key_to_parent
            //        parentKey.DAGParentKeys.Add(newMultiKey);
            //        newMultiKey.DAGChildKeys.Add(parentKey);
            //        //继承下级的触发ids
            //        newMultiKey.CanTriggerNode.UnionWith(parentKey.CanTriggerNode);
            //        //囊括下级
            //        newMultiKey.CanTriggerNode.Add(parentKey.Id);
            //    }
            //    else if (keyType == MapKeyType.OR)
            //    {
            //        //OR关系,DAG上级关系反向
            //        //parent是DAG的下级,关联上自身作为上级
            //        newMultiKey.DAGParentKeys.Add(parentKey);
            //        parentKey.DAGChildKeys.Add(newMultiKey);
            //    }
            //}

            ////全部处理完毕,设置当前项的剩余单键关联
            //truthNeedAddedSingleKeys.SymmetricExceptWith(factRelateAllSingleKeys);
            //foreach (var key in truthNeedAddedSingleKeys)
            //{
            //    var keyInst = Keys.GetValueOrDefault(key);
            //    newMultiKey.ChildKeys.Add(key, keyInst);
            //    //处理DAG视角的关联
            //    if (keyType == MapKeyType.AND)
            //    {
            //        //AND关系,将自身DAG上级关系关联上该_add_key_to_child
            //        keyInst.DAGParentKeys.Add(newMultiKey);
            //        newMultiKey.DAGChildKeys.Add(keyInst);
            //        //AND关系新增单键,合并单键的triggernodes
            //        newMultiKey.CanTriggerNode.UnionWith(keyInst.CanTriggerNode);
            //        //并且将该单键加入处理
            //        newMultiKey.CanTriggerNode.Add(keyInst.Id);
            //    }
            //    else if (keyType == MapKeyType.OR)
            //    {
            //        //OR关系,DAG上级关系反向
            //        //parent是DAG的下级,关联上自身作为上级
            //        newMultiKey.DAGParentKeys.Add(keyInst);
            //        keyInst.DAGChildKeys.Add(newMultiKey);
            //    }
            //}

            ////如果是OR关系,并且最终重新关联之后自身子节点为空,需要添加一个ANY节点作为触发
            //if (newMultiKey.KeyRelateType == MapKeyType.OR && newMultiKey.DAGChildKeys.Count == 0)
            //{
            //    newMultiKey.DAGChildKeys.Add(AnyTypeCode);
            //    newMultiKey.CanTriggerNode.Add(AnyTypeCode.Id);
            //    AnyTypeCode.DAGParentKeys.Add(newMultiKey);
            //}

            ////通知该key的DAG父节点添加当前节点ID来更新可触发ID表
            ////调用自身,顺带把自己也写入
            //newMultiKey.NotifyUpperAddNode(newMultiKey.Id);

            //最终注册
            Keys.Add(newMultiKey.Id, newMultiKey);
            nameKeyMap.Add(newMultiKey.Name!, newMultiKey);
            _multiKeyMap.Add(newMultiKey.Id, newMultiKey);
            multiKeyRuntimeId = newMultiKey.Id;
            _createdMultiKeyCache.Add(keysAfterHash, newMultiKey.Id);

            return true;

        }

        /// <summary>
        /// 尝试获取层级桥接key
        /// </summary>
        /// <param name="bridgeKeyRuntimeId"></param>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public bool TryCreateBridgeKey(out long bridgeKeyRuntimeId, long current, long next, int currentLevel)
        {
            //0层无效[从1层开始计算]
            if (currentLevel <= 0)
            {
                bridgeKeyRuntimeId = -1;
                return false;
            }

            if (Keys.TryGetValue(current, out IKey currentKeyInst) && Keys.TryGetValue(next, out IKey nextLevelKeyInst))
            {
                if (currentKeyInst is ISimpleNode simpleKeyInst && nextLevelKeyInst is ISimpleNode nextLevelSimpleKeyInst)
                {
                    var currentKeyHash = $"B|{_hashIds.EncodeLong(current, next)}";
                    if (_createdBridgeKeyCache.TryGetValue(currentKeyHash, out bridgeKeyRuntimeId))
                        return true;

                    var newRegisterBridgeNode = new BridgeKey(IdCreator.Instance.GetId(), currentKeyHash, currentLevel, simpleKeyInst, nextLevelSimpleKeyInst);

                    //获取对应当前层的节点的对应当前层级的响应节点表
                    if (!simpleKeyInst.LevelTriggerNodes.TryGetValue(currentLevel, out var currentKeyTriggerNodes))
                    {
                        //如果无表,说明未初始化,主动收集对应DAG子节点的当前层响应表
                        simpleKeyInst.CollectDAGChildsTriggerNodes(currentLevel);
                        //写入自身+通知DAG父节点所有跃升到当前层的节点该节点可触发
                        simpleKeyInst.NotifyDAGUpperNodeToLevel(currentLevel, simpleKeyInst.Id);
                        simpleKeyInst.LevelTriggerNodes.TryGetValue(currentLevel, out currentKeyTriggerNodes);
                    }

                    //获取对应下一层的节点的对应当前层级的响应节点表
                    var nextLevel = currentLevel + 1;
                    if (!nextLevelSimpleKeyInst.LevelTriggerNodes.TryGetValue(nextLevel, out var nextLevelKeyTriggerNodes))
                    {
                        nextLevelSimpleKeyInst.CollectDAGChildsTriggerNodes(nextLevel);
                        nextLevelSimpleKeyInst.NotifyDAGUpperNodeToLevel(nextLevel, nextLevelSimpleKeyInst.Id);
                        nextLevelSimpleKeyInst.LevelTriggerNodes.TryGetValue(nextLevel, out nextLevelKeyTriggerNodes);
                    }

                    //获取能唤醒该节点的列表
                    var willBeCurrentParentNodes = Keys.Values.
                        Where(x =>
                            x is IBridgeKey bridgeKeyInst
                            && bridgeKeyInst.JumpLevel == newRegisterBridgeNode.JumpLevel
                            && bridgeKeyInst.Current.LevelTriggerNodes.TryGetValue(bridgeKeyInst.JumpLevel, out var currentLevelTriggerNodes)
                            && currentLevelTriggerNodes.Contains(newRegisterBridgeNode.Current.Id)
                            && bridgeKeyInst.Next.LevelTriggerNodes.TryGetValue(bridgeKeyInst.JumpLevel + 1, out var nextLevelTriggerNodes)
                            && nextLevelTriggerNodes.Contains(newRegisterBridgeNode.Next.Id)
                        ).Select(x => x as IBridgeKey);

                    //获取该节点可唤醒的节点列表
                    var willBeCurrentChildNodes = Keys.Values.
                        Where(x =>
                            x is IBridgeKey bridgeKeyInst
                            && bridgeKeyInst.JumpLevel == newRegisterBridgeNode.JumpLevel
                            && currentKeyTriggerNodes.Contains(bridgeKeyInst.Current.Id)
                            && nextLevelKeyTriggerNodes.Contains(bridgeKeyInst.Next.Id)
                        ).Select(x => x as IBridgeKey);

                    //重组关联
                    foreach (var parent in willBeCurrentParentNodes)
                    {
                        //如果该父节点不是其他任何父节点的父节点
                        //说明该节点是距离当前节点最近的一批节点之一
                        if (!willBeCurrentParentNodes.Any(x => x!.Id != parent!.Id && x.DAGParentKeys.Contains(parent)))
                        {
                            //从父节点去除即将与该节点关联的子节点
                            foreach (var needDisconnectNode in willBeCurrentChildNodes.Where(x => x!.DAGParentKeys.Contains(parent!)))
                            {
                                parent!.DAGChildKeys.Remove(needDisconnectNode!);
                                needDisconnectNode!.DAGParentKeys.Remove(parent!);
                            }
                            //将当前节点注册到对应的父节点的子节点下
                            parent!.DAGChildKeys.Add(newRegisterBridgeNode);
                            newRegisterBridgeNode.DAGParentKeys.Add(parent!);
                        }
                    }

                    newRegisterBridgeNode.NotifyUpperAddNode(newRegisterBridgeNode.Id);

                    foreach (var child in willBeCurrentChildNodes)
                    {
                        child!.DAGParentKeys.Add(newRegisterBridgeNode);
                        newRegisterBridgeNode.DAGChildKeys.Add(child);
                        newRegisterBridgeNode.CanTriggerNode.UnionWith(child.CanTriggerNode);
                    }

                    Keys.Add(newRegisterBridgeNode.Id, newRegisterBridgeNode);
                    bridgeKeyRuntimeId = newRegisterBridgeNode.Id;
                    _createdBridgeKeyCache.Add(currentKeyHash, newRegisterBridgeNode.Id);
                    return true;
                }
                else
                    throw new ArgumentException(message: $"Not Allow create bridge key with non simple key node {currentKeyInst},{nextLevelKeyInst}");
            }
            bridgeKeyRuntimeId = -1;
            return false;
        }

        /// <summary>
        /// 注册新的层级关系键
        /// </summary>
        /// <param name="levelKeyRuntimeId"></param>
        /// <param name="registerBridggKeyIds">层级关联--顺序等价于层级联系</param>
        /// <returns></returns>
        public bool TryRegisterLevelKey(out long levelKeyRuntimeId, params long[] registerBridggKeyIds)
        {
            levelKeyRuntimeId = -1;
            if (registerBridggKeyIds.Length == 0) return false;

            var keySeqStart = 0;
            var keySeqEnd = 0;

            if (registerBridggKeyIds.Length == 1)
            {
                if(Keys.TryGetValue(registerBridggKeyIds[0], out var keyInst))
                {
                    if (keyInst is IBridgeKey bridgeKeyInst)
                    {
                        keySeqStart = bridgeKeyInst.JumpLevel;
                        keySeqEnd = bridgeKeyInst.JumpLevel + 1;
                    }
                    else throw new InvalidOperationException(message: $"Not support connect non bridge key link({keyInst})");
                }
            }
            if (registerBridggKeyIds.Length > 1)
            {
                //节点检查,确保所有节点都是bridge节点,同时每个bridge节点之间是连续的
                for (int i = 0; i < registerBridggKeyIds.Length - 1; i++)
                    if (Keys.TryGetValue(registerBridggKeyIds[i], out var keyInst1) && Keys.TryGetValue(registerBridggKeyIds[i + 1], out var keyInst2))
                        if (keyInst1 is IBridgeKey bridgeKeyInst1 && keyInst2 is IBridgeKey bridgeKeyInst2)
                        {
                            if (bridgeKeyInst1.Next.Id != bridgeKeyInst2.Current.Id) throw new InvalidOperationException(message: $"Not support connect diff key link({bridgeKeyInst1}-{bridgeKeyInst2})");
                            if (keySeqStart == 0) keySeqStart = bridgeKeyInst1.JumpLevel;
                            if(i==registerBridggKeyIds.Length-2) keySeqEnd = bridgeKeyInst2.JumpLevel + 1;
                        }
                        else throw new InvalidOperationException(message: $"Not support connect non bridge key link({keyInst1}-{keyInst2})");
            }

            var newLevelKeyHash = $"L|{keySeqStart}-{keySeqEnd}|{_hashIds.EncodeLong(registerBridggKeyIds)}";
            if (_createdLevelKeyCache.TryGetValue(newLevelKeyHash, out levelKeyRuntimeId))
                return true;

            Keys.TryGetValue(registerBridggKeyIds.Last(), out var last_key);

            var newLevelNode = new LevelKey(IdCreator.Instance.GetId(), newLevelKeyHash, registerBridggKeyIds);

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

            var parentCache = new HashSet<long>();
            var ignoreParentCache = new HashSet<long>();
            var circulateCache = new Stack<ILevelKey>();
            var childCache = new HashSet<long>();
            var ignoreChildCache = new HashSet<long>();
            var newNodeBridgeKeyInstSequence = newLevelNode.KeySequence.Select(x => { Keys.TryGetValue(x, out var _truth_key); return _truth_key as IBridgeKey; }).ToArray();


            foreach (var node in Keys.Values.OfType<ILevelKey>())
            {
                //可能为父节点
                //最终将统计出当前节点所有可能的最近父集节点组
                if (node.KeySequence.Length > newLevelNode.KeySequence.Length)
                {
                    if (ignoreParentCache.Contains(node.Id))
                        continue;
                    //无论当前节点能否通过,将其父节点全部丢入忽略的parentnodes中
                    circulateCache.Push(node);
                    while (circulateCache.Count > 0)
                    {
                        if (circulateCache.TryPop(out var topParentNode))
                        {
                            foreach (var parent in topParentNode.DAGParentKeys)
                            {
                                if (parent is ILevelKey parentInst)
                                {
                                    if (ignoreParentCache.Contains(parent.Id))
                                        continue;
                                    circulateCache.Push(parentInst);
                                    ignoreParentCache.Add(parentInst.Id);
                                    //Keys无序,检查是否之前有存在,有的话删了
                                    if (parentCache.Contains(parentInst.Id)) parentCache.Remove(parentInst.Id);
                                }
                            }
                        }
                    }

                    parentCache.Add(node.Id);
                    for (int i = 0; i < newLevelNode.KeySequence.Length; i++)
                    {
                        //节点ID相同,直接通过
                        if (node.KeySequence[i] == newNodeBridgeKeyInstSequence[i]!.Id)
                            continue;
                        if (Keys.TryGetValue(node.KeySequence[i], out var BridgeKeysInst))
                        {
                            //任意序列位置不包含,直接跳出
                            if (!BridgeKeysInst.CanTriggerNode.Contains(newLevelNode.KeySequence[i]))
                            {
                                parentCache.Remove(node.Id);
                                ignoreParentCache.Add(node.Id);
                                break;
                            }
                        }
                    }
                }
                //可能为子节点
                //最终将统计出当前节点所有可能的最近子集节点组
                else if (node.KeySequence.Length < newLevelNode.KeySequence.Length)
                {
                    if (ignoreChildCache.Contains(node.Id))
                        //存在于要求忽略的子节点中,跳过
                        continue;
                    //无论当前节点能否通过,将其子节点全部丢入忽略的parentnodes中
                    circulateCache.Push(node);
                    while (circulateCache.Count > 0)
                    {
                        if (circulateCache.TryPop(out var childNode))
                        {
                            foreach (var child in childNode.DAGChildKeys)
                            {
                                if (child is ILevelKey childInst)
                                {
                                    if (ignoreChildCache.Contains(child.Id))
                                        continue;
                                    circulateCache.Push(childInst);
                                    ignoreChildCache.Add(childInst.Id);
                                    //Keys无序,检查是否之前有存在,有的话删了
                                    if (childCache.Contains(childInst.Id))
                                        childCache.Remove(childInst.Id);
                                }
                            }
                        }
                    }

                    childCache.Add(node.Id);
                    for (int i = 0; i < node.KeySequence.Length; i++)
                    {
                        //节点ID相同,直接通过
                        if (node.KeySequence[i] == newNodeBridgeKeyInstSequence[i]!.Id)
                            continue;
                        //任意序列位置不包含,直接跳出
                        if (!newNodeBridgeKeyInstSequence[i]!.CanTriggerNode.Contains(node.KeySequence[i]))
                        {
                            childCache.Remove(node.Id);
                            ignoreChildCache.Add(node.Id);
                            break;
                        }
                    }
                }
                //序列长度相等 不好说
                else
                {
                    if (ignoreParentCache.Contains(node.Id))
                        continue;

                    //-1=child,1=parent
                    int addThisNodeToChildOrParent = 0;

                    for (int i = 0; i < node.KeySequence.Length; i++)
                    {
                        //节点ID相同,直接通过
                        if (node.KeySequence[i] == newNodeBridgeKeyInstSequence[i]!.Id)
                            continue;

                        bool beParent = false;
                        //当前节点的序列X包含目标节点的序列X,该节点应该作child
                        bool beChild = newNodeBridgeKeyInstSequence[i]!.CanTriggerNode.Contains(node.KeySequence[i]);
                        if (!beChild)
                        {
                            Keys.TryGetValue(node.KeySequence[i], out var _parent_seq_key);
                            //目标节点的序列X包含当前节点的序列X,该节点应该作parent
                            beParent = _parent_seq_key.CanTriggerNode.Contains(newNodeBridgeKeyInstSequence[i]!.Id);
                        }
                        //和当前节点无关联,跳出
                        //只能判断出该节点和此节点无关
                        if (!beChild && !beParent)
                        {
                            ignoreParentCache.Add(node.Id);
                            break;
                        }

                        if (addThisNodeToChildOrParent != 0)
                        {
                            //前者节点关系是子当前是父,或者前者是父当前是子,存在通路交叉,跳出
                            if ((addThisNodeToChildOrParent < 0 && beParent) || (addThisNodeToChildOrParent > 0 && beChild))
                            {
                                ignoreParentCache.Add(node.Id);
                                break;
                            }
                        }
                        else
                        {
                            addThisNodeToChildOrParent = beChild ? -1 : 1;
                        }
                    }
                    if (!ignoreParentCache.Contains(node.Id))
                    {
                        if (addThisNodeToChildOrParent > 0)
                            parentCache.Add(node.Id);
                        else if (addThisNodeToChildOrParent < 0)
                            childCache.Add(node.Id);
                        else
                            throw new InvalidCastException(message: "Connect level key error:equal compare boom!");
                    }
                }

            }
            //构建关系树
            foreach (var parent in parentCache)
            {
                Keys.TryGetValue(parent, out var parentInst);
                foreach (var needDisconnectNode in parentInst.DAGChildKeys.Where(x => childCache.Contains(x.Id)))
                {
                    needDisconnectNode.DAGParentKeys.Remove(parentInst);
                    parentInst.DAGChildKeys.Remove(needDisconnectNode);
                }
                parentInst.DAGChildKeys.Add(newLevelNode);
                newLevelNode.DAGParentKeys.Add(parentInst);
            }

            newLevelNode.NotifyUpperAddNode(newLevelNode.Id);

            foreach (var child in childCache)
            {
                Keys.TryGetValue(child, out var childInst);
                childInst.DAGParentKeys.Add(newLevelNode);
                newLevelNode.DAGChildKeys.Add(childInst);
                newLevelNode.CanTriggerNode.UnionWith(childInst.CanTriggerNode);
            }

            Keys.Add(newLevelNode.Id, newLevelNode);
            levelKeyRuntimeId = newLevelNode.Id;
            _createdBridgeKeyCache.Add(newLevelKeyHash, newLevelNode.Id);
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
            if (!_nameGroupMap.TryGetValue(typeof(T).FullName, out var group))
                throw new ArgumentException(message: $"{typeof(T)} not be injected into global map.");

            var enumOriginValue = Enums.GetMember(value)?.ToInt64();

            if (enumOriginValue is null)
                throw new ArgumentException(message: $"{value} not Find in Global Enums Cache,it should't be happened.");

            if (!(group as IEnumGroup)!.RelateKeys.TryGetValue(enumOriginValue.Value, out var key))
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
            if (!nameKeyMap.TryGetValue(name, out var key))
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
            if (!_nameGroupMap.TryGetValue(typeof(T).FullName, out var group))
            {
                _logger.LogWarning(message: $"{typeof(T)} not be injected into global map.");
                return false;
            }


            var enumOriginValue = Enums.GetMember(value)?.ToInt64();

            if (enumOriginValue is null)
            {
                _logger.LogWarning(message: $"{value} not Find in Global Enums Cache,it should't be happened.");
                return false;
            }


            if (!(group as IEnumGroup)!.RelateKeys.TryGetValue(enumOriginValue.Value, out var key))
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
            if (!_nameGroupMap.TryGetValue(typeof(T).FullName, out var group))
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
            if (!nameKeyMap.TryGetValue(name, out var key))
            {
                _logger.LogWarning(message: $"Not Find {name} enum value in convert cache");
                id = -1;
                return false;
            }

            id = key.Id;
            return true;
        }
        internal bool TryConvert<T>(string name, out T key)
            where T:IKey
        {
            if (nameKeyMap.TryGetValue(name, out var keyInst))
            {
                if (keyInst is T targetTypeKey)
                {
                    key = targetTypeKey;
                    return true;
                }
            }
            _logger.LogWarning(message: $"Not Find {name} enum value in convert cache");
            key=default!;
            return false;
        }
        /// <summary>
        /// 给定id转换为运行时Key
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        internal bool TryGetKey<T>(long id, out T value)
            where T : IKey
        {
            if (Keys.TryGetValue(id, out var key))
            {
                if (key is T keyInst)
                {
                    value = keyInst;
                    return true;
                }
            }
            value = default!;
            return false;
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
            StringBuilder graphvizGraph = new StringBuilder();
            StringBuilder graphvizNodes=new StringBuilder();
            StringBuilder graphvizConnectionline = new StringBuilder();
            graphvizGraph.AppendLine("************Copy Code************");
            graphvizGraph.AppendLine("digraph G {");

            if (ignore_single_node)
            {
                HashSet<long> _has_line_keys = new HashSet<long>();

                foreach (var key in Keys.Values)
                {
                    foreach (var _line in key.DAGParentKeys)
                    {
                        if (_line.KeyRelateType == MapKeyType.NONE && _line.DAGParentKeys.Count == 0)
                            continue;
                        graphvizConnectionline.AppendLine($"{_line.Id} -> {key.Id};");
                        _has_line_keys.Add(_line.Id);
                        _has_line_keys.Add(key.Id);
                    }
                }
                foreach(var key in Keys.Values)
                {
                    if(_has_line_keys.Contains(key.Id))
                        graphvizNodes.AppendLine(key.ToGraphvizNodeString());
                }
            }
            else
            {
                foreach (var key in Keys.Values)
                {
                    graphvizNodes.AppendLine(key.ToGraphvizNodeString());
                    foreach (var _line in key.DAGParentKeys)
                        graphvizConnectionline.AppendLine($"{_line.Id} -> {key.Id};");
                }
            }

            graphvizGraph.Append(graphvizNodes);
            graphvizGraph.Append(graphvizConnectionline);
            graphvizGraph.AppendLine("}");

            return graphvizGraph.ToString();
        }
    }
}
