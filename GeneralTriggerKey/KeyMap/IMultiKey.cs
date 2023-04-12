using System;
using System.Collections.Generic;
using System.Text;

namespace GeneralTriggerKey.KeyMap
{
    internal interface IMultiKey : ISimpleNode
    {
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
