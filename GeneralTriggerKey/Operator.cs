using GeneralTriggerKey.KeyMap;
using GeneralTriggerKey.Utils;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace GeneralTriggerKey
{
    /// <summary>
    /// 封装操作
    /// </summary>
    public static class Operator
    {
        /// <summary>
        /// 字符串表达式转KEY
        /// </summary>
        /// <param name="key_expression">对应字符串表达式</param>
        /// <returns></returns>
        public static GeneralKey G(string key_expression)
        {
            return SystaxParserWrapper.TransFormStringToKeyInst(key_expression);
        }

        /// <summary>
        /// 运行时增添key
        /// </summary>
        /// <param name="callname">key名称</param>
        /// <param name="group">Key隶属域</param>
        /// <param name="force_add">是否强制添加</param>
        /// <returns></returns>
        public static GeneralKey R(string callname,string group = "GLO", bool force_add = false)
        {
            var _id = KMStorageWrapper.GetOrAddSingleKey(callname, group, force_add);
            if (_id > 0)
            {
                KMStorageWrapper.TryGetKey(_id, out IKey key);
                var _is_multi_key = key as IMultiKey;
                return new GeneralKey(key.Id, key.IsMultiKey, _is_multi_key is null ? MapKeyType.None : _is_multi_key.KeyRelateType);
            }
            return default;
        }

        /// <summary>
        /// 映射KEY的快速获取
        /// </summary>
        /// <param name="name">类型名称</param>
        /// <returns></returns>
        public static GeneralKey Q(string name)
        {

            if (KMStorageWrapper.TryConvert(name, out IKey key))
            {
                var _is_multi_key = key as IMultiKey;
                return new GeneralKey(key.Id, key.IsMultiKey, _is_multi_key is null ? MapKeyType.None : _is_multi_key.KeyRelateType);
            }
            return default;
        }

        /// <summary>
        /// 映射KEY的快速获取
        /// </summary>
        /// <typeparam name="T">枚举类型</typeparam>
        /// <param name="enum_key">枚举字段</param>
        /// <returns></returns>
        public static GeneralKey Q<T>(T enum_key)
            where T : struct, Enum
        {
            if (KMStorageWrapper.TryConvert(enum_key, out IKey key))
            {
                var _is_multi_key = key as IMultiKey;
                return new GeneralKey(key.Id, key.IsMultiKey, _is_multi_key is null ? MapKeyType.None : _is_multi_key.KeyRelateType);
            }
            return default;
        }

        /// <summary>
        /// 绘制整体注册的Key和他们唤醒关系
        /// </summary>
        /// <param name="ignore_single_node">忽略无关联的单一节点</param>
        /// <returns>对应graphviz的节点图表代码</returns>
        public static string ToGraphvizCode(bool ignore_single_node = false)
        {
            return KeyMapStorage.Instance.OutPutGraphviz(ignore_single_node);
        }

        /// <summary>
        /// 导出所有注册的Key详情
        /// </summary>
        public static string ShowAllNodes()
        {
            return KeyMapStorage.Instance.OutPutKeysInfo();
        }

        /// <summary>
        /// 注册指定程序集中所有被标记的枚举列表
        /// </summary>
        /// <param name="assembly"></param>
        public static void InjectEnumsFromAssembly(Assembly assembly)
        {
            KeyMapStorage.Instance.AutoInjectEnumsMapping(assembly);
        }

    }
}
