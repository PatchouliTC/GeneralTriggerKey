using GeneralTriggerKey.KeyMap;
using System.Collections.Generic;

namespace GeneralTriggerKey.Key
{
    /// <summary>
    /// 运行时手动增添Key
    /// </summary>
    public class RunTimeKey : KeyGroupUnit, IRunTimeKey
    {
        public bool IsMultiKey => false;

        public string DisplayName { get; private set; }

        public bool HasAlias
        {
            get { return !(this.Alias is null || this.Alias.Count == 0); }
        }

        public HashSet<string> Alias { get; private set; } = new HashSet<string>();

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
            return $"[RunTimeKey]({Id})<{DisplayName}>[UseFor:{Range}]";
        }
    }
}
