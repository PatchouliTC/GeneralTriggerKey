using System;
using System.Collections.Generic;
using System.Text;

namespace GeneralTriggerKey.KeyMap
{
    /// <summary>
    /// 运行时动态添加的自定义key
    /// </summary>
    internal interface IRunTimeKey : IKey
    {
        /// <summary>
        /// 限定组名
        /// </summary>
        public string Range { get; }
    }
}
