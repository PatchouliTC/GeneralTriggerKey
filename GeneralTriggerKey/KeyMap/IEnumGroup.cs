using System;
using System.Collections.Generic;
using System.Text;

namespace GeneralTriggerKey.KeyMap
{
    /// <summary>
    /// 枚举项组
    /// </summary>
    public interface IEnumGroup : IGroup
    {
        public Type? Type { get; }
    }
}
