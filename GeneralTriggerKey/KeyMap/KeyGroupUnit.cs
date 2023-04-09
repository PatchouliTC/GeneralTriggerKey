using System;
using System.Collections.Generic;
using System.Text;

namespace GeneralTriggerKey.KeyMap
{
    public abstract class KeyGroupUnit
    {
        public long Id { get; private set; }
        public string? Name { get; private set; }

        public MapKeyType KeyRelateType { get; private set; }
        public List<IKey> DAGParentKeys { get; } = new List<IKey>();
        public HashSet<long> CanTriggerNode { get; } = new HashSet<long>();


        public KeyGroupUnit(long id, MapKeyType keyRelateType, string? name = null)
        {
            Id = id;
            Name = name;
            KeyRelateType = keyRelateType;
        }

        public void NotifyUpperAddNode(long id)
        {
            if (CanTriggerNode.Contains(id))
                return;
            CanTriggerNode.Add(id);
            foreach (var node in DAGParentKeys)
                node.NotifyUpperAddNode(id);
        }

        public virtual string ToGraphvizNodeString()
        {
            return $"{Id} [label=\"{Name}\"];";
        }

        public override string ToString()
        {
            return $"[{Id}]<{Name}>";
        }
    }
}
