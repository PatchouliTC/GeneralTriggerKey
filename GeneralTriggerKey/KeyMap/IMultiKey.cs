using System;
using System.Collections.Generic;
using System.Text;

namespace GeneralTriggerKey.KeyMap
{
    /// <summary>
    /// 组合key
    /// </summary>
    internal interface IMultiKey : IKey
    {
        /// <summary>
        /// 是否包含了其他联合键
        /// </summary>
        public bool HasOtherMultiKey { get; }
        /// <summary>
        /// 联合键类型
        /// </summary>
        public MapKeyType KeyRelateType { get; }
        /// <summary>
        /// 关联的子键
        /// </summary>
        public Dictionary<long, IKey> ChildKeys { get; }
        /// <summary>
        /// 关联了自身的key父节点
        /// </summary>
        public List<IMultiKey> ParentKeys { get; }//能包含联合键自身的也是联合键

        /// <summary>
        /// 关联的实际所有非联合键的总和快照
        /// </summary>
        public HashSet<long> RelateSingleKeys { get; }
        /// <summary>
        /// 当前键关联的键ids
        /// </summary>
        public long[] MultiKeys { get; }
        public bool Contains(long id);
        public bool IsRSupersetOf(IMultiKey key);
        public bool ROverlaps(IMultiKey key);

    }
}
