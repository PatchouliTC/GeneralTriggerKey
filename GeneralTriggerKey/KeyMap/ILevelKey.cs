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
        int EndLevel { get; }
        /// <summary>
        /// 起始深度
        /// </summary>
        int StartLevel { get; }
        /// <summary>
        /// 当前KEY对应序列
        /// </summary>
        long[] KeySequence { get; }
    }
}
