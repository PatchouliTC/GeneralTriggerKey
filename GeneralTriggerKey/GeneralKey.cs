using GeneralTriggerKey.KeyMap;
using GeneralTriggerKey.Utils.Extensions;
using GeneralTriggerKey.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using IdGen;

namespace GeneralTriggerKey
{
    /// <summary>
    /// 运行时key封装
    /// </summary>
    public readonly struct GeneralKey
    {
        /// <summary>
        /// 运行时ID
        /// </summary>
        public readonly long Id;
        /// <summary>
        /// 是否为联合键
        /// </summary>
        public readonly bool IsMultiKey;
        /// <summary>
        /// 键类型
        /// </summary>
        public readonly MapKeyType KeyType;

        public readonly int Depth;

        public GeneralKey(long id, bool isMultiKey, MapKeyType keyType)
        {
            Id = id;
            IsMultiKey = isMultiKey;
            KeyType = keyType;
            Depth = 0;
        }

        public GeneralKey(long id, bool isMultiKey, MapKeyType keyType, int depth)
        {
            Id = id;
            IsMultiKey = isMultiKey;
            KeyType = keyType;
            Depth = depth;
        }

        /// <summary>
        /// 当前项能否触发right项
        /// <para>已重载*等效该操作</para>
        /// </summary>
        /// <param name="right"></param>
        /// <param name="force_contain_key">必须强制包含的key</param>
        /// <returns></returns>
        public bool CanTrigger(in GeneralKey right, in GeneralKey? force_contain_key = null)
        {
            if (force_contain_key is null)
                return Id.CanTrigger(right.Id);
            return Id.CanTrigger(right.Id, force_contain_key.Value.Id);
        }

        /// <summary>
        /// 将两个键关联为指定层的桥
        /// </summary>
        /// <param name="key"></param>
        /// <param name="level">层级</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public GeneralKey Connect(in GeneralKey key, int level = 1)
        {
            if (Id.ConnectWith(key.Id, level, out var new_id))
            {
                KeyMapStorage.Instance.TryGetKey(new_id, out IBridgeKey value);
                return new GeneralKey(new_id, value.IsMultiKey, value.KeyRelateType, value.JumpLevel);
            }
            (string l, string r) = MakeErrorString(Id, key.Id);
            throw new InvalidOperationException(message: $"Try Do connect operator for {l} and {r} Failed.");
        }

        /// <summary>
        /// 尝试进行与操作,如果与操作后结果并没有被注册过则返回false
        /// </summary>
        /// <param name="right"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public bool AndWithIfExist(in GeneralKey right, out GeneralKey result)
        {
            if (Id.AndWith(right.Id, out var id) && KeyMapStorage.Instance.TryGetKey(id,out IKey newKey))
            {
                result = new GeneralKey(id, newKey.IsMultiKey, newKey.KeyRelateType);
                return true;
            }
            result = default;
            return false;
        }

        public bool Contains(in GeneralKey right)
        {
            return Id.Contains(right.Id);
        }

        /// <summary>
        /// *等效于can trigger
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator *(in GeneralKey left,in GeneralKey right)
        {
            return left.Id.CanTrigger(right.Id);
        }
        public static GeneralKey operator &(in GeneralKey left,in GeneralKey right)
        {
            if (!(left.KeyType == MapKeyType.AND || right.KeyType == MapKeyType.NONE || left.KeyType == MapKeyType.OR) ||
                !(right.KeyType == MapKeyType.AND || right.KeyType == MapKeyType.NONE || right.KeyType == MapKeyType.OR)
            )
                throw new InvalidOperationException(message: "Not Support or/and with non or/and");

            if (left.Id.AndWith(right.Id, out var new_id) && KeyMapStorage.Instance.TryGetKey(new_id, out IKey newKey))
                return new GeneralKey(new_id, newKey.IsMultiKey, newKey.KeyRelateType);

            (string l, string r) = MakeErrorString(left.Id, right.Id);
            throw new InvalidOperationException(message: $"Try Do Add operator for {l} and {r} Failed.");
        }

        public static GeneralKey operator |(in GeneralKey left, in GeneralKey right)
        {
            if (!(left.KeyType == MapKeyType.AND || right.KeyType == MapKeyType.NONE || left.KeyType == MapKeyType.OR) ||
                !(right.KeyType == MapKeyType.AND || right.KeyType == MapKeyType.NONE || right.KeyType == MapKeyType.OR)
            )
                throw new InvalidOperationException(message: "Not Support or/and with non or/and");

            if (left.Id.OrWith(right.Id, out var new_id) && KeyMapStorage.Instance.TryGetKey(new_id, out IKey newKey))
                return new GeneralKey(new_id, newKey.IsMultiKey, newKey.KeyRelateType);

            (string l, string r) = MakeErrorString(left.Id, right.Id);
            throw new InvalidOperationException(message: $"Try Do Or operator for {l} and {r} Failed.");
        }

        public static GeneralKey operator ^(in GeneralKey left, in GeneralKey right)
        {
            if (!(left.KeyType == MapKeyType.AND || right.KeyType == MapKeyType.NONE || left.KeyType == MapKeyType.OR) ||
                !(right.KeyType == MapKeyType.AND || right.KeyType == MapKeyType.NONE || right.KeyType == MapKeyType.OR)
            )
                throw new InvalidOperationException(message: "Not Support or/and with non or/and");
            if (left.Id.SymmetricExceptWith(right.Id, out var new_id))
            {
                KeyMapStorage.Instance.TryGetKey(new_id, out IKey value);
                return new GeneralKey(new_id, value.IsMultiKey, value.KeyRelateType);
            }

            (string l, string r) = MakeErrorString(left.Id, right.Id);
            throw new InvalidOperationException(message: $"Try Do Xor operator for {l} and {r} Failed.");
        }

        public static GeneralKey operator /(in GeneralKey left, in GeneralKey right)
        {
            if (left.Id.DivideWith(right.Id, out var new_id))
            {
                KeyMapStorage.Instance.TryGetKey(new_id, out ILevelKey value);
                return new GeneralKey(new_id, value.IsMultiKey, value.KeyRelateType, value.EndLevel);
            }
            (string l, string r) = MakeErrorString(left.Id, right.Id);
            throw new InvalidOperationException(message: $"Try Do divide operator for {l} and {r} Failed.");
        }

        public static bool operator ==(in GeneralKey left,  in GeneralKey right)
        {
            return left.Id == right.Id && left.Id != 0;
        }
        public static bool operator !=(in GeneralKey left, in GeneralKey right)
        {
            return left.Id != right.Id && left.Id != 0 && right.Id != 0;
        }
        public static bool operator >=(in GeneralKey left, in GeneralKey right)
        {
            return left.Id.Contains(right.Id);
        }
        public static bool operator <=(in GeneralKey left,in GeneralKey right)
        {
            return right.Id.Contains(left.Id);
        }
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (!(obj is GeneralKey))
                return false;
            return Id.Equals(((GeneralKey)obj)!.Id);
        }
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
        public override string ToString()
        {
            KeyMapStorage.Instance.TryGetKey(Id, out IKey value);
            return value.ToString();
        }

        public string ToGraphviz()
        {
            KeyMapStorage.Instance.TryGetKey(Id, out IKey value);
            return value.ToGraphvizNodeString();
        }
        private static (string, string) MakeErrorString(long idl, long idr)
        {
            if (idl == idr && idl == 0)
                return ("EmptyKey", "EmptyKey");
            string leftStr = "EmptyKey";
            string rightStr = "EmptyKey";
            if (KeyMapStorage.Instance.TryGetKey(idl, out IKey left))
                leftStr = left.ToString();
            if (KeyMapStorage.Instance.TryGetKey(idr, out IKey right))
                rightStr = right.ToString();
            return (leftStr, rightStr);
        }
    }
}
