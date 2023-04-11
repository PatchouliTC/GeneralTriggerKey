using GeneralTriggerKey.KeyMap;
using GeneralTriggerKey.Utils.Extensions;
using GeneralTriggerKey.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

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
            if (!(left.KeyType == MapKeyType.AND || right.KeyType == MapKeyType.None || left.KeyType == MapKeyType.OR) ||
                !(right.KeyType == MapKeyType.AND || right.KeyType == MapKeyType.None || right.KeyType == MapKeyType.OR)
            )
                throw new ArgumentException(message: "Not Support or/and with non or/and");

            if (left.Id.AndWith(right.Id, out var new_id))
                return new GeneralKey(new_id, true, MapKeyType.AND);

            (string l, string r) = MakeErrorString(left.Id, right.Id);
            throw new InvalidOperationException(message: $"Try Do Add operator for {l} and {r} Failed.");
        }

        public static GeneralKey operator |(GeneralKey left, GeneralKey right)
        {
            if (!(left.KeyType == MapKeyType.AND || right.KeyType == MapKeyType.None || left.KeyType == MapKeyType.OR) ||
                !(right.KeyType == MapKeyType.AND || right.KeyType == MapKeyType.None || right.KeyType == MapKeyType.OR)
            )
                throw new ArgumentException(message: "Not Support or/and with non or/and");

            if (left.Id.OrWith(right.Id, out var new_id))
                return new GeneralKey(new_id, true, MapKeyType.OR);

            (string l, string r) = MakeErrorString(left.Id, right.Id);
            throw new InvalidOperationException(message: $"Try Do Or operator for {l} and {r} Failed.");
        }

        public static GeneralKey operator ^(GeneralKey left, GeneralKey right)
        {
            if (!(left.KeyType == MapKeyType.AND || right.KeyType == MapKeyType.None || left.KeyType == MapKeyType.OR) ||
                !(right.KeyType == MapKeyType.AND || right.KeyType == MapKeyType.None || right.KeyType == MapKeyType.OR)
            )
                throw new ArgumentException(message: "Not Support or/and with non or/and");
            if (left.Id.SymmetricExceptWith(right.Id, out var new_id))
            {
                KMStorageWrapper.TryGetKey(new_id, out IKey value);
                var _is_multi_key = value as IMultiKey;
                return new GeneralKey(new_id, value.IsMultiKey, _is_multi_key is null ? MapKeyType.None : _is_multi_key.KeyRelateType);
            }

            (string l, string r) = MakeErrorString(left.Id, right.Id);
            throw new InvalidOperationException(message: $"Try Do Xor operator for {l} and {r} Failed.");
        }

        public static GeneralKey operator /(GeneralKey left, GeneralKey right)
        {
            if ((left.KeyType == MapKeyType.AND || right.KeyType == MapKeyType.None || left.KeyType == MapKeyType.OR) &&
                (right.KeyType == MapKeyType.AND || right.KeyType == MapKeyType.None || right.KeyType == MapKeyType.OR)
            )
            {
                KeyMapStorage.Instance.TryCreateBridgeKey(out var bridge_key_runtime_id, left.Id, right.Id, 1);
                KeyMapStorage.Instance.TryRegisterLevelKey(out var level_key_runtime_id, bridge_key_runtime_id);
                return new GeneralKey(level_key_runtime_id, false, MapKeyType.LEVEL, 2);
            }
            else if (left.KeyType == MapKeyType.Bridge && right.KeyType == MapKeyType.Bridge)
            {
                KeyMapStorage.Instance.TryRegisterLevelKey(out var level_key_runtime_id, left.Id, right.Id);
                KMStorageWrapper.TryGetKey(level_key_runtime_id, out ILevelKey value);
                return new GeneralKey(level_key_runtime_id, false, MapKeyType.LEVEL, value.MaxDepth);
            }
            else if (left.KeyType == MapKeyType.LEVEL && right.KeyType == MapKeyType.Bridge)
            {
                KMStorageWrapper.TryGetKey(left.Id, out ILevelKey _lkey);

                var _temp = new List<long>(_lkey.KeySequence)
                {
                    right.Id
                };
                KeyMapStorage.Instance.TryRegisterLevelKey(out var level_key_runtime_id, _temp.ToArray());
                KMStorageWrapper.TryGetKey(level_key_runtime_id, out ILevelKey value);
                return new GeneralKey(level_key_runtime_id, false, MapKeyType.LEVEL, value.MaxDepth);
            }
            else if (left.KeyType == MapKeyType.LEVEL && right.KeyType == MapKeyType.LEVEL)
            {
                KMStorageWrapper.TryGetKey(left.Id, out ILevelKey _lkey1);
                KMStorageWrapper.TryGetKey(right.Id, out ILevelKey _lkey2);

                var _temp = new List<long>(_lkey1.KeySequence);
                _temp.AddRange(_lkey2.KeySequence);
                KeyMapStorage.Instance.TryRegisterLevelKey(out var level_key_runtime_id, _temp.ToArray());
                KMStorageWrapper.TryGetKey(level_key_runtime_id, out ILevelKey value);
                return new GeneralKey(level_key_runtime_id, false, MapKeyType.LEVEL, value.MaxDepth);
            }
            else if (left.KeyType == MapKeyType.Bridge &&
                (right.KeyType == MapKeyType.AND || right.KeyType == MapKeyType.None || right.KeyType == MapKeyType.OR))
            {
                KMStorageWrapper.TryGetKey(left.Id, out IBridgeKey _lbkey);
                KeyMapStorage.Instance.TryCreateBridgeKey(out var bridge_key_runtime_id, _lbkey.Next.Id, right.Id, _lbkey.JumpLevel + 1);
                KeyMapStorage.Instance.TryRegisterLevelKey(out var level_key_runtime_id, left.Id, bridge_key_runtime_id);
                KMStorageWrapper.TryGetKey(level_key_runtime_id, out IKey value);
                return new GeneralKey(level_key_runtime_id, false, MapKeyType.LEVEL, (value as ILevelKey)!.MaxDepth);
            }

            else if (left.KeyType == MapKeyType.LEVEL &&
                (right.KeyType == MapKeyType.AND || right.KeyType == MapKeyType.None || right.KeyType == MapKeyType.OR))
            {
                KMStorageWrapper.TryGetKey(left.Id, out ILevelKey _lkey1);

                KMStorageWrapper.TryGetKey(_lkey1.KeySequence.Last(), out IBridgeKey _bkey);

                KeyMapStorage.Instance.TryCreateBridgeKey(out var bridge_key_runtime_id, _bkey.Next.Id, right.Id, _bkey.JumpLevel + 1);
                var _temp = new List<long>(_lkey1.KeySequence)
                {
                    bridge_key_runtime_id
                };
                KeyMapStorage.Instance.TryRegisterLevelKey(out var level_key_runtime_id, _temp.ToArray());
                KMStorageWrapper.TryGetKey(level_key_runtime_id, out IKey value);
                return new GeneralKey(level_key_runtime_id, false, MapKeyType.LEVEL, (value as ILevelKey)!.MaxDepth);
            }
            else if ((left.KeyType == MapKeyType.AND || right.KeyType == MapKeyType.None || left.KeyType == MapKeyType.OR) &&
                (right.KeyType == MapKeyType.Bridge))
            {
                KMStorageWrapper.TryGetKey(right.Id, out IBridgeKey _lbkey);
                if (_lbkey.JumpLevel > 1)
                {
                    KeyMapStorage.Instance.TryCreateBridgeKey(out var bridge_key_runtime_id, left.Id, _lbkey.Current.Id, _lbkey.JumpLevel - 1);
                    KeyMapStorage.Instance.TryRegisterLevelKey(out var level_key_runtime_id, left.Id, bridge_key_runtime_id);
                    KMStorageWrapper.TryGetKey(level_key_runtime_id, out IKey value);
                    return new GeneralKey(level_key_runtime_id, false, MapKeyType.LEVEL, (value as ILevelKey)!.MaxDepth);
                }
            }
            throw new ArgumentException(message: "Unsupport Operator for /");
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
