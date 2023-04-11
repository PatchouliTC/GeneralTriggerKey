using System;
using System.Collections.Generic;
using System.Text;

namespace GeneralTriggerKey.KeyMap
{
    internal abstract class KeyGroupUnit
    {
        public long Id { get; private set; }
        public string? Name { get; private set; }

        public MapKeyType KeyRelateType { get; private set; }
        public HashSet<IKey> DAGParentKeys { get; } = new HashSet<IKey>();
        public HashSet<IKey> DAGChildKeys { get; } = new HashSet<IKey>();
        public HashSet<long> CanTriggerNode { get; }


        protected KeyGroupUnit(long id, MapKeyType keyRelateType, string? name = null)
        {
            Id = id;
            Name = name;
            KeyRelateType = keyRelateType;
            CanTriggerNode = new HashSet<long>() { Id };
        }

        public virtual void NotifyUpperAddNode(long id)
        {
            if (id != Id && CanTriggerNode.Contains(id))
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

    internal abstract class SimpleKeyNode : KeyGroupUnit
    {
        public Dictionary<int, HashSet<long>> LevelTriggerNodes { get; private set; } = new Dictionary<int, HashSet<long>>();


        protected SimpleKeyNode(long id, MapKeyType keyRelateType, string? name = null)
            : base(id, keyRelateType, name)
        {

        }
        public void CollectDAGChildsTriggerNodes(int level)
        {
            //初始化层级
            if (!LevelTriggerNodes.TryGetValue(level, out var _child_trigger_nodes))
            {
                //初始化包含自身
                _child_trigger_nodes = new HashSet<long> { Id };
                LevelTriggerNodes.Add(level, _child_trigger_nodes);
            }

            var _visit_stack = new Stack<SimpleKeyNode>();
            var _checked_nodes = new HashSet<long>();
            _visit_stack.Push(this);

            while (_visit_stack.Count > 0)
            {
                if (_visit_stack.TryPop(out var _visiting))
                {
                    foreach (var node in _visiting.DAGChildKeys)
                    {
                        if (node is SimpleKeyNode _key)
                        {
                            //如果子节点的指定层级有值,说明已经刷新完毕,直接取并集即可
                            if (_key.LevelTriggerNodes.TryGetValue(level, out var _ekey_trigger_nodes))
                                _child_trigger_nodes.UnionWith(_ekey_trigger_nodes);
                            else
                            {
                                if (!_checked_nodes.Contains(_key.Id))
                                    //没有值,需要深度检查
                                    _visit_stack.Push(_key);
                            }
                            _checked_nodes.Add(_key.Id);
                        }
                    }
                }
            }
        }

        public void NotifyDAGUpperNodeToLevel(int level, long id)
        {
            if (!CanTriggerNode.Contains(id))
                return;
            if (LevelTriggerNodes.TryGetValue(level, out var _child_trigger_nodes))
            {
                if (_child_trigger_nodes.Contains(id) && id != Id)
                    return;
                _child_trigger_nodes.Add(id);
            }
            foreach (var node in DAGParentKeys)
            {
                if (node is ISimpleNode _node)
                    _node.NotifyDAGUpperNodeToLevel(level, id);
            }
        }
    }
}
