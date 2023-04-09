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
        /// <summary>
        /// 显示名称
        /// </summary>
        public string DisplayName { get; }
        /// <summary>
        /// 是否有别名
        /// </summary>
        public bool HasAlias { get; }
        public HashSet<string>? Alias { get; }

        /// <summary>
        /// 作为有向图映射中该节点的上级节点
        /// </summary>
        public List<IKey> DAGParentKeys { get; }
        /// <summary>
        /// 当前节点可触发节点
        /// </summary>
        public HashSet<long> CanTriggerNode { get; }

        /// <summary>
        /// 通知该层级和上级节点添加指定Id为可触发Id
        /// </summary>
        /// <param name="id"></param>
        public void NotifyUpperAddNode(long id);

        public string ToGraphvizNodeString();
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
