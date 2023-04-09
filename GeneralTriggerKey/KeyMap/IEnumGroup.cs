using System;
using System.Collections.Generic;
using System.Text;

namespace GeneralTriggerKey.KeyMap
{
    /// <summary>
    /// 枚举项组
    /// </summary>
    internal interface IEnumGroup : IGroup
    {
        public Type? Type { get; }
    }
}
