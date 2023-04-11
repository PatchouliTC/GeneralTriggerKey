using System;
using System.Collections.Generic;
using System.Text;

namespace GeneralTriggerKey.KeyMap
{
    internal interface ILevelKey : IKey
    {
        /// <summary>
        /// 当前Level深度
        /// </summary>
        int MaxDepth { get; }
        /// <summary>
        /// 当前KEY对应序列
        /// </summary>
        long[] KeySequence { get; }
    }
}
