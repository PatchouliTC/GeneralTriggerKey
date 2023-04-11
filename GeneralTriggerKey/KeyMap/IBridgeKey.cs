using System;
using System.Collections.Generic;
using System.Text;

namespace GeneralTriggerKey.KeyMap
{
    internal interface IBridgeKey : IKey
    {
        /// <summary>
        /// 跳跃层阶
        /// </summary>
        int JumpLevel { get; }
        /// <summary>
        /// 当前层阶
        /// </summary>
        ISimpleNode Current { get; }
        /// <summary>
        /// 下一层阶
        /// </summary>
        ISimpleNode Next { get; }
    }
}
