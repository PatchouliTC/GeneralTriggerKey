using GeneralTriggerKey.KeyMap;
using IdGen;
using System;
using System.Collections.Generic;
using System.Text;

namespace GeneralTriggerKey.Utils
{
    /// <summary>
    /// 
    /// </summary>
    public static class KMStorageWrapper
    {
        /// <summary>
        /// 运行时增添key
        /// </summary>
        /// <param name="callName">key名称</param>
        /// <param name="range">Key隶属域</param>
        /// <param name="force_add">是否强制添加</param>
        /// <returns></returns>
        public static long GetOrAddSingleKey(string callName, string range = "GLOBAL", bool force_add = false)
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
        public static long Convert<T>(T value, bool exception_ignore = false)
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
        /// 给定项名称转换为运行时ID
        /// </summary>
        /// <param name="name">名称</param>
        /// <param name="exception_ignore">是否忽略异常【忽略后失败返回-1】</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static long Convert(string name, bool exception_ignore = false)
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

        public static bool TryRegisterRunTimeKey(out long runtime_key_id, string callName, string range = "GLO")
        {
            return KeyMapStorage.Instance.TryGetOrAddSingleKey(out runtime_key_id, callName, range);
        }
        public static bool TryRegisterMultiKey(out long multi_key_runtime_id, MapKeyType keyType, params long[] register_key_ids)
        {
            return KeyMapStorage.Instance.TryRegisterMultiKey(out multi_key_runtime_id, keyType, register_key_ids, false);
        }

        public static bool TryRegisterMultiKeyIfExist(out long multi_key_runtime_id, MapKeyType keyType, params long[] register_key_ids)
        {
            return KeyMapStorage.Instance.TryRegisterMultiKey(out multi_key_runtime_id, keyType, register_key_ids, true);
        }
    }
}
