using System;
using System.Collections.Generic;
using System.Text;

namespace GeneralTriggerKey.KeyMap
{
    public interface IKey
    {
        /// <summary>
        /// 对应key的运行时实际Id
        /// </summary>
        public long Id { get; }
        /// <summary>
        /// 是否是复合key
        /// </summary>
        public bool IsMultiKey { get; }
        public string DisplayName { get; }
        /// <summary>
        /// 联合键类型
        /// </summary>
        public MapKeyType KeyRelateType { get; }
        /// <summary>
        /// 作为有向图映射中该节点的上级节点
        /// </summary>
        public HashSet<IKey> DAGParentKeys { get; }
        /// <summary>
        /// 作为有向图映射该节点的下级节点
        /// </summary>
        public HashSet<IKey> DAGChildKeys { get; }
        /// <summary>
        /// 当前节点可触发节点
        /// </summary>
        public HashSet<long> CanTriggerNode { get; }

        /// <summary>
        /// 通知上层节点可触发列表
        /// </summary>
        /// <param name="id"></param>
        public void NotifyUpperAddNode(long id);

        public string ToGraphvizNodeString();
    }

    public interface ISimpleNode : IKey
    {
        /// <summary>
        /// 不同层级的实际触发节点
        /// </summary>
        public Dictionary<int, HashSet<long>> LevelTriggerNodes { get; }
        /// <summary>
        /// 根据指定层级,收集所有子节点的指定层级的可用节点列表
        /// </summary>
        /// <param name="level"></param>
        public void CollectDAGChildsTriggerNodes(int level);
        /// <summary>
        /// 根据指定层级,通知所有父节点[如果该父节点同样存在于该层]写入该子节点响应
        /// </summary>
        /// <param name="level"></param>
        /// <param name="id"></param>
        public void NotifyDAGUpperNodeToLevel(int level, long id);
    }

    public interface IGroup
    {
        /// <summary>
        /// 这里的KEY是对应关联的原始key
        /// </summary>
        public Dictionary<long, IKey> RelateKeys { get; }
        /// <summary>
        /// 组别称
        /// </summary>
        public string? GroupAlia { get; }
    }
}
