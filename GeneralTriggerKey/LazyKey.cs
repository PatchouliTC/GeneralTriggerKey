using GeneralTriggerKey.KeyMap;
using GeneralTriggerKey.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GeneralTriggerKey
{
    /// <summary>
    /// 延时执行GeneralKey
    /// </summary>
    public readonly struct LazyKey
    {
        /// <summary>
        /// 当前节点组类型
        /// </summary>
        public readonly MapKeyType KeyType;

        public readonly List<long> CacheKeyIds;
        public LazyKey(MapKeyType keyType, params long[] keys)
        {
            KeyType = keyType;
            CacheKeyIds = keys.ToList();
        }

        public LazyKey(MapKeyType keyType, List<long> keys)
        {
            KeyType = keyType;
            CacheKeyIds = keys;
        }

        public LazyKey(MapKeyType keyType)
        {
            KeyType = keyType;
            CacheKeyIds = new List<long>();
        }

        public static implicit operator GeneralKey(in LazyKey d)
        {
            return d.Compile();
        }

        public GeneralKey Compile()
        {
            return Compile(this);
        }
        /// <summary>
        /// 计算最终Key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private static GeneralKey Compile(in LazyKey key)
        {
            if (key.CacheKeyIds.Count == 0) throw new ArgumentException(message: "Not allow compile for none ids");
            else if (key.CacheKeyIds.Count == 1)
            {
                if(KeyMapStorage.Instance.TryGetKey(key.CacheKeyIds[0], out IKey keyInst))
                {
                    return new GeneralKey(keyInst.Id, keyInst.IsMultiKey, keyInst.KeyRelateType);
                }
            }
            else
            {
                if (key.KeyType == MapKeyType.AND||key.KeyType==MapKeyType.OR)
                {
                    if(KeyMapStorage.Instance.TryRegisterMultiKey(out var multiKeyRuntimeId, MapKeyType.AND, key.CacheKeyIds.ToArray()) && KeyMapStorage.Instance.TryGetKey(multiKeyRuntimeId, out IKey keyInst))
                    {
                        return new GeneralKey(keyInst.Id, keyInst.IsMultiKey, keyInst.KeyRelateType);
                    }
                }
            }
            throw new ArgumentException(message:$"UnKnown Reason for compile id(s) to key instance.({key.KeyType}-[{String.Join(", ", key.CacheKeyIds.ToArray())}])");
        }
    }
}
