using GeneralTriggerKey.KeyMap;
using System;
using System.Collections.Generic;
using System.Text;

namespace GeneralTriggerKey.Utils
{
    internal static class KMStorageWrapper
    {
        /// <summary>
        /// 运行时增添key
        /// </summary>
        /// <param name="callName">key名称</param>
        /// <param name="range">Key隶属域</param>
        /// <param name="force_add">是否强制添加</param>
        /// <returns></returns>
        internal static long GetOrAddSingleKey(string callName, string range = "GLOBAL", bool force_add = false)
        {
            if (KeyMapStorage.Instance.TryGetOrAddSingleKey(out var _id, callName, range, force_add))
                return _id;
            return -1;
        }

        /// <summary>
        /// 给定枚举类型转换为运行时ID
        /// </summary>
        /// <typeparam name="T">枚举项</typeparam>
        /// <param name="value">实际ID</param>
        /// <param name="exception_ignore">是否忽略异常【忽略后失败返回-1】</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        internal static long Convert<T>(T value, bool exception_ignore = false)
            where T : struct, Enum
        {
            if (exception_ignore)
            {
                var status = KeyMapStorage.Instance.TryConvert(value, out long id);
                return id;
            }
            else
            {
                return KeyMapStorage.Instance.Convert(value);
            }

        }
        /// <summary>
        /// 给定枚举类型转换为运行时ID
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        internal static bool TryConvert<T>(T value, out long id)
            where T : struct, Enum
        {
            return KeyMapStorage.Instance.TryConvert(value, out id);
        }
        /// <summary>
        /// 给定枚举类型转换为运行时ID并返回实际key
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        internal static bool TryConvert<T>(T value, out IKey key)
            where T : struct, Enum
        {
            return KeyMapStorage.Instance.TryConvert(value, out key);
        }
        /// <summary>
        /// 给定项名称转换为运行时ID
        /// </summary>
        /// <param name="name">名称</param>
        /// <param name="exception_ignore">是否忽略异常【忽略后失败返回-1】</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        internal static long Convert(string name, bool exception_ignore = false)
        {
            if (exception_ignore)
            {
                var status = KeyMapStorage.Instance.TryConvert(name, out long id);
                return id;
            }
            else
            {
                return KeyMapStorage.Instance.Convert(name);
            }
        }
        /// <summary>
        /// 给定项名称转换为运行时ID
        /// </summary>
        /// <param name="name"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        internal static bool TryConvert(string name, out long id)
        {
            return KeyMapStorage.Instance.TryConvert(name, out id);
        }
        /// <summary>
        /// 给定项名称转换为运行时ID并返回实际key
        /// </summary>
        /// <param name="name"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        internal static bool TryConvert(string name, out IKey key)
        {
            return KeyMapStorage.Instance.TryConvert(name, out key);
        }

        internal static bool TryRegisterRunTimeKey(out long runtime_key_id, string callName, string range = "GLOBAL")
        {
            return KeyMapStorage.Instance.TryGetOrAddSingleKey(out runtime_key_id, callName, range);
        }
        internal static bool TryRegisterMultiKey(out long multi_key_runtime_id, MapKeyType keyType = default, params long[] register_key_ids)
        {
            return KeyMapStorage.Instance.TryRegisterMultiKey(out multi_key_runtime_id, keyType, register_key_ids, false);
        }

        internal static bool TryRegisterMultiKeyIfExist(out long multi_key_runtime_id, MapKeyType keyType = default, params long[] register_key_ids)
        {
            return KeyMapStorage.Instance.TryRegisterMultiKey(out multi_key_runtime_id, keyType, register_key_ids, true);
        }

        internal static bool TryGetKey<T>(long id, out T value)
            where T : IKey
        {
            if (KeyMapStorage.Instance.Keys.TryGetValue(id, out var key))
            {
                if (key is T)
                {
                    value = (T)key;
                    return true;
                }
            }
            value = default;
            return false;
        }
    }
}
