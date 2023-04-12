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
                            if (ignoreCache.Contains(parent.Id)) continue;
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
                            if (ignoreCache.Contains(child.Id)) continue;
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
                    newMultiKey.DAGParentKeys.Add(keyInst);
                    keyInst.DAGChildKeys.Add(newMultiKey);
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
                        simpleKeyInst.CollectDAGChildsTriggerNodes(currentLevel);
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

                    var parentCache = new HashSet<long>();
                    var childCache = new HashSet<long>();
                    var circulateCache = new Stack<IKey>();
                    var ignoreCache = new HashSet<long>();

                    foreach(var node in Keys.Values.Where(x => x is IBridgeKey bkey && bkey.JumpLevel==newRegisterBridgeNode.JumpLevel))
                    {
                        if (ignoreCache.Contains(node.Id))
                            continue;
                        var bridgeNode=node as IBridgeKey;

                        //该节点可触发该节点
                        if (bridgeNode!.Current.LevelTriggerNodes.TryGetValue(bridgeNode.JumpLevel,out var currentLevelTriggerNodes)
                            &&bridgeNode!.Next.LevelTriggerNodes.TryGetValue(bridgeNode.JumpLevel+1,out var nextLevelTriggerNodes)
                            && currentLevelTriggerNodes.Contains(newRegisterBridgeNode.Current.Id)
                            && nextLevelTriggerNodes.Contains(newRegisterBridgeNode.Current.Id))
                        {
                            parentCache.Add(node.Id);
                            //循环所有该节点的祖先节点并将其全部加入忽略节点,同时如果之前加入过父节点记录的话对其进行删除操作
                            circulateCache.Push(node);
                            while (circulateCache.TryPop(out var topSearchNode))
                            {
                                foreach (var parent in topSearchNode.DAGParentKeys)
                                {
                                    circulateCache.Push(parent);
                                    ignoreCache.Add(parent.Id);
                                    if (parentCache.Contains(parent.Id)) parentCache.Remove(parent.Id);
                                }
                            }
                        }
                        else if (currentKeyTriggerNodes.Contains(bridgeNode.Current.Id)
                            && nextLevelKeyTriggerNodes.Contains(bridgeNode.Next.Id))
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

                    //重组关联
                    foreach(var parent in parentCache)
                    {
                        Keys.TryGetValue(parent, out var parentInst);
                        foreach(var child in parentInst.DAGChildKeys.Where(x => childCache.Contains(x.Id)))
                        {
                            child.DAGParentKeys.Remove(parentInst);
                            parentInst.DAGChildKeys.Remove(child);
                        }
                        parentInst.DAGChildKeys.Add(newRegisterBridgeNode);
                        newRegisterBridgeNode.DAGParentKeys.Add(parentInst);
                    }

                    foreach(var child in childCache)
                    {
                        Keys.TryGetValue(child, out var childInst);
                        childInst.DAGParentKeys.Add(newRegisterBridgeNode);
                        newRegisterBridgeNode.DAGChildKeys.Add(childInst);

                        newRegisterBridgeNode.CanTriggerNode.UnionWith(childInst.CanTriggerNode);
                    }

                    newRegisterBridgeNode.NotifyUpperAddNode(newRegisterBridgeNode.Id);

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
            else if (registerBridggKeyIds.Length > 1)
            {
                IKey startKey=null!;
                //节点检查,确保所有节点都是bridge节点,同时每个bridge节点之间是连续的
                for (int i = 0; i < registerBridggKeyIds.Length - 1; i++)
                    if (Keys.TryGetValue(registerBridggKeyIds[i], out var keyInst1) && Keys.TryGetValue(registerBridggKeyIds[i + 1], out var keyInst2))
                    {
                        if (keyInst1 is IBridgeKey bridgeKeyInst1 && keyInst2 is IBridgeKey bridgeKeyInst2)
                        {
                            if (bridgeKeyInst1.Next.Id != bridgeKeyInst2.Current.Id) throw new InvalidOperationException(message: $"Not support connect diff key link({bridgeKeyInst1}-{bridgeKeyInst2})");
                            if (keySeqStart == 0)
                            {
                                keySeqStart = bridgeKeyInst1.JumpLevel;
                                startKey = bridgeKeyInst1;
                            }
                            if (i == registerBridggKeyIds.Length - 2) keySeqEnd = bridgeKeyInst2.JumpLevel + 1;
                        }
                        else throw new InvalidOperationException(message: $"Not support connect non bridge key link({keyInst1}-{keyInst2})");
                    }
                //临时决定:左端桥高于1时,用ANY节点桥补齐
                if (keySeqStart > 1)
                {
                    List<long> tempAttachBridgeNodes=new List<long>();
                    for(int i=1;i<keySeqStart; i++)
                    {
                        long tempbridgekey = -1;
                        if (i == keySeqStart - 1)TryCreateBridgeKey(out tempbridgekey, AnyTypeCode.Id, startKey.Id, i);
                        else TryCreateBridgeKey(out tempbridgekey, AnyTypeCode.Id, AnyTypeCode.Id, i);
                        tempAttachBridgeNodes.Add(tempbridgekey);
                    }
                    tempAttachBridgeNodes.AddRange(registerBridggKeyIds);
                    registerBridggKeyIds = tempAttachBridgeNodes.ToArray();
                }
            }

            var newLevelKeyHash = $"L|{keySeqStart}-{keySeqEnd}|{_hashIds.EncodeLong(registerBridggKeyIds)}";
            if (_createdLevelKeyCache.TryGetValue(newLevelKeyHash, out levelKeyRuntimeId))
                return true;

            Keys.TryGetValue(registerBridggKeyIds.Last(), out var last_key);

            var newLevelNode = new LevelKey(IdCreator.Instance.GetId(), newLevelKeyHash, registerBridggKeyIds);

            var parentCache = new HashSet<long>();
            var ignoreCache = new HashSet<long>();
            var circulateCache = new Stack<ILevelKey>();
            var childCache = new HashSet<long>();
            var newNodeBridgeKeyInstSequence = newLevelNode.KeySequence.Select(x => { Keys.TryGetValue(x, out var _truth_key); return _truth_key as IBridgeKey; }).ToArray();

            foreach (var node in Keys.Values.OfType<ILevelKey>())
            {
                if (ignoreCache.Contains(node.Id))
                    continue;
                //可能为父节点
                //最终将统计出当前节点所有可能的最近父集节点组
                if (node.KeySequence.Length > newLevelNode.KeySequence.Length)
                {
                    //无论当前节点能否通过,将其父节点全部丢入忽略的parentnodes中
                    circulateCache.Push(node);
                    while (circulateCache.TryPop(out var topParentNode))
                    {
                        foreach (var parent in topParentNode.DAGParentKeys)
                        {
                            if (parent is ILevelKey parentInst)
                            {
                                if (ignoreCache.Contains(parent.Id)) continue;
                                circulateCache.Push(parentInst);
                                ignoreCache.Add(parentInst.Id);
                                //Keys无序,检查是否之前有存在,有的话删了
                                if (parentCache.Contains(parentInst.Id)) parentCache.Remove(parentInst.Id);
                            }
                        }
                    }
                    
                    //逐位比较,必须每位均可触发才可认为是父节点
                    for (int i = 0; i < newLevelNode.KeySequence.Length; i++)
                    {
                        //节点ID相同,直接通过
                        if (node.KeySequence[i] == newNodeBridgeKeyInstSequence[i]!.Id) continue;
                        if (Keys.TryGetValue(node.KeySequence[i], out var BridgeKeysInst))
                        {
                            //任意序列位置不包含,直接跳出
                            if (!BridgeKeysInst.CanTriggerNode.Contains(newLevelNode.KeySequence[i])) break;
                            //如果执行到最后一位也包含,加入考量父节点列表
                            if (i == newLevelNode.KeySequence.Length - 1) parentCache.Add(node.Id);
                        }
                        else break;
                    }
                }
                //可能为子节点
                //最终将统计出当前节点所有可能的最近子集节点组
                else if (node.KeySequence.Length < newLevelNode.KeySequence.Length)
                {
                    //无论当前节点能否通过,将其子节点全部丢入忽略的parentnodes中
                    circulateCache.Push(node);
                    while (circulateCache.TryPop(out var childNode))
                    {
                        foreach (var child in childNode.DAGChildKeys)
                        {
                            if (child is ILevelKey childInst)
                            {
                                if (ignoreCache.Contains(child.Id)) continue;
                                circulateCache.Push(childInst);
                                ignoreCache.Add(childInst.Id);
                                //Keys无序,检查是否之前有存在,有的话删了
                                if (childCache.Contains(childInst.Id)) childCache.Remove(childInst.Id);
                            }
                        }
                    }
                    for (int i = 0; i < node.KeySequence.Length; i++)
                    {
                        //节点ID相同,直接通过
                        if (node.KeySequence[i] == newNodeBridgeKeyInstSequence[i]!.Id) continue;
                        //任意序列位置不包含,直接跳出
                        if (!newNodeBridgeKeyInstSequence[i]!.CanTriggerNode.Contains(node.KeySequence[i])) break;
                        if (i == node.KeySequence.Length - 1) childCache.Add(node.Id);
                    }
                }
                //序列长度相等 不好说
                else
                {
                    //-1=child,1=parent
                    int addThisNodeToChildOrParent = 0;
                    for (int i = 0; i < node.KeySequence.Length; i++)
                    {
                        //节点ID相同,直接通过
                        if (node.KeySequence[i] == newNodeBridgeKeyInstSequence[i]!.Id) continue;

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
                            ignoreCache.Add(node.Id);
                            break;
                        }

                        if (addThisNodeToChildOrParent == 0) addThisNodeToChildOrParent = beChild ? -1 : 1;
                        else
                        {
                            //前者节点关系是子当前是父,或者前者是父当前是子,存在通路交叉,跳出
                            if ((addThisNodeToChildOrParent < 0 && beParent) || (addThisNodeToChildOrParent > 0 && beChild))
                            {
                                ignoreCache.Add(node.Id);
                                break;
                            }
                        }
                    }
                    if (!ignoreCache.Contains(node.Id))
                    {
                        if (addThisNodeToChildOrParent > 0)
                        {
                            parentCache.Add(node.Id);

                            circulateCache.Push(node);
                            while (circulateCache.TryPop(out var topParentNode))
                            {
                                foreach (var parent in topParentNode.DAGParentKeys)
                                {
                                    if (parent is ILevelKey parentInst)
                                    {
                                        circulateCache.Push(parentInst);
                                        ignoreCache.Add(parentInst.Id);
                                        //Keys无序,检查是否之前有存在,有的话删了
                                        if (parentCache.Contains(parentInst.Id)) parentCache.Remove(parentInst.Id);
                                    }
                                }
                            }
                        }
                        else if (addThisNodeToChildOrParent < 0)
                        {
                            childCache.Add(node.Id);
                            circulateCache.Push(node);
                            while (circulateCache.TryPop(out var topChildNode))
                            {
                                foreach (var child in topChildNode.DAGChildKeys)
                                {
                                    if (child is ILevelKey childInst)
                                    {
                                        circulateCache.Push(childInst);
                                        ignoreCache.Add(childInst.Id);
                                        //Keys无序,检查是否之前有存在,有的话删了
                                        if (childCache.Contains(childInst.Id)) childCache.Remove(childInst.Id);
                                    }
                                }
                            }
                        }   
                        else
                            throw new InvalidCastException(message: "Connect level key error:equal compare boom!");
                    }
                }
            }

            //构建关系树
            foreach (var parent in parentCache)
            {
                Keys.TryGetValue(parent, out var parentInst);
                foreach (var child in parentInst.DAGChildKeys.Where(x => childCache.Contains(x.Id)))
                {
                    child.DAGParentKeys.Remove(parentInst);
                    parentInst.DAGChildKeys.Remove(child);
                }
                parentInst.DAGChildKeys.Add(newLevelNode);
                newLevelNode.DAGParentKeys.Add(parentInst);
            }

            foreach (var child in childCache)
            {
                Keys.TryGetValue(child, out var childInst);
                childInst.DAGParentKeys.Add(newLevelNode);
                newLevelNode.DAGChildKeys.Add(childInst);

                newLevelNode.CanTriggerNode.UnionWith(childInst.CanTriggerNode);
            }

            newLevelNode.NotifyUpperAddNode(newLevelNode.Id);

            Keys.Add(newLevelNode.Id, newLevelNode);
            levelKeyRuntimeId = newLevelNode.Id;
            _createdLevelKeyCache.Add(newLevelKeyHash, newLevelNode.Id);
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
