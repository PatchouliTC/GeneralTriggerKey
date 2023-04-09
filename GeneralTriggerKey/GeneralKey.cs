using GeneralTriggerKey.KeyMap;
using GeneralTriggerKey.Utils.Extensions;
using GeneralTriggerKey.Utils;
using System;
using System.Collections.Generic;
using System.Text;

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

        public GeneralKey(long id, bool isMultiKey, MapKeyType keyType)
        {
            Id = id;
            IsMultiKey = isMultiKey;
            KeyType = keyType;
        }

        /// <summary>
        /// 当前项能否触发right项
        /// <para>已重载*等效该操作</para>
        /// </summary>
        /// <param name="right"></param>
        /// <param name="force_contain_key">必须强制包含的key</param>
        /// <returns></returns>
        public bool CanTrigger(GeneralKey right, GeneralKey? force_contain_key = null)
        {
            if (force_contain_key is null)
                return Id.CanTrigger(right.Id);
            return Id.CanTrigger(right.Id, force_contain_key.Value.Id);
        }

        /// <summary>
        /// 尝试进行与操作,如果与操作后结果并没有被注册过则返回false
        /// </summary>
        /// <param name="right"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public bool AndWithIfExist(GeneralKey right, out GeneralKey result)
        {
            if (Id.AndWith(right.Id, out var _id))
            {
                result = new GeneralKey(_id, true, MapKeyType.AND);
                return true;
            }
            result = default;
            return false;
        }

        public bool Contains(GeneralKey right)
        {
            return Id.Contains(right.Id);
        }

        /// <summary>
        /// *等效于can trigger
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator *(GeneralKey left, GeneralKey right)
        {
            return left.Id.CanTrigger(right.Id);
        }
        public static GeneralKey operator &(GeneralKey left, GeneralKey right)
        {
            if (left.Id.AndWith(right.Id, out var new_id))
                return new GeneralKey(new_id, true, MapKeyType.AND);

            (string l, string r) = MakeErrorString(left.Id, right.Id);
            throw new InvalidOperationException(message: $"Try Do Add operator for {l} and {r} Failed.");
        }
        public static GeneralKey operator |(GeneralKey left, GeneralKey right)
        {
            if (left.Id.OrWith(right.Id, out var new_id))
                return new GeneralKey(new_id, true, MapKeyType.OR);

            (string l, string r) = MakeErrorString(left.Id, right.Id);
            throw new InvalidOperationException(message: $"Try Do Or operator for {l} and {r} Failed.");
        }
        public static GeneralKey operator ^(GeneralKey left, GeneralKey right)
        {
            if (left.Id.SymmetricExceptWith(right.Id, out var new_id))
            {
                KMStorageWrapper.TryGetKey(new_id, out IKey value);
                var _is_multi_key = value as IMultiKey;
                return new GeneralKey(new_id, value.IsMultiKey, _is_multi_key is null ? MapKeyType.None : _is_multi_key.KeyRelateType);
            }

            (string l, string r) = MakeErrorString(left.Id, right.Id);
            throw new InvalidOperationException(message: $"Try Do Xor operator for {l} and {r} Failed.");
        }
        public static bool operator ==(GeneralKey left, GeneralKey right)
        {
            return left.Id == right.Id && left.Id != 0;
        }
        public static bool operator !=(GeneralKey left, GeneralKey right)
        {
            return left.Id != right.Id && left.Id != 0 && right.Id != 0;
        }
        public static bool operator >=(GeneralKey left, GeneralKey right)
        {
            return left.Id.Contains(right.Id);
        }
        public static bool operator <=(GeneralKey left, GeneralKey right)
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
            KMStorageWrapper.TryGetKey(Id, out IKey value);
            return value.ToString();
        }

        public string ToGraphviz()
        {
            KMStorageWrapper.TryGetKey(Id, out IKey value);
            return value.ToGraphvizNodeString();
        }
        private static (string, string) MakeErrorString(long idl, long idr)
        {
            if (idl == idr && idl == 0)
                return ("EmptyKey", "EmptyKey");
            string _lefts = "EmptyKey";
            string _rights = "EmptyKey";
            if (KMStorageWrapper.TryGetKey(idl, out IKey _left))
                _lefts = _left.ToString();
            if (KMStorageWrapper.TryGetKey(idr, out IKey _right))
                _rights = _right.ToString();
            return (_lefts, _rights);
        }
    }
}
