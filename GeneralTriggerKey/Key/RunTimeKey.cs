using GeneralTriggerKey.KeyMap;
using System.Collections.Generic;

namespace GeneralTriggerKey.Key
{
    /// <summary>
    /// 运行时手动增添Key
    /// </summary>
    internal sealed class RunTimeKey : SimpleKeyNode, IRunTimeKey
    {
        public bool IsMultiKey => false;

        public string DisplayName { get; private set; }

        public string Range { get; private set; }

        public RunTimeKey(long id, string name, string range)
            : base(id, MapKeyType.None, $"R.{range}-{name}")
        {
            Range = range;
            DisplayName = name;
        }

        public override string ToGraphvizNodeString()
        {
            return $"{Id} [label=\"[R]({Range}){DisplayName}\"];";
        }

        public override string ToString()
        {
            return $"[RunTimeKey]({Id})<{DisplayName}>-[UseFor:{Range}]";
        }
    }
}
