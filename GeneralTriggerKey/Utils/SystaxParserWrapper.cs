using GeneralTriggerKey.KeyMap;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using static GeneralTriggerKey.SyntaxParser.SyntaxParser;

namespace GeneralTriggerKey.Utils
{
    internal static class SystaxParserWrapper
    {

        internal static GeneralKey TransFormStringToKeyInst(string key_string)
        {
            //语法检查器校验完毕
            var _after_systax_parser = ParseTextToSyntax(key_string);
            return TransFormNode(_after_systax_parser!);
        }

        private static readonly Regex _sWhitespace = new Regex(@"\s+");
        private static GeneralKey TransFormNode(SyntaxNode node)
        {
            //已经过滤重组,剩余内容只存在& | ( )
            //暂不考虑+ <<
            //( )继续往深处递归
            if (node is ParenthesizedExpressionSyntax _parenthesizedNode)
            {
                return TransFormNode(_parenthesizedNode.Expression);
            }
            //& | 逻辑符号,继续往深处递归左右两边
            else if (node is BinaryExpressionSyntax _binaryNode)
            {
                var left = TransFormNode(_binaryNode.Left);
                var right = TransFormNode(_binaryNode.Right);
                return _binaryNode.Kind() switch
                {
                    SyntaxKind.BitwiseAndExpression => left & right,
                    SyntaxKind.BitwiseOrExpression => left | right,
                    SyntaxKind.DivideExpression => left / right,
                    _ => throw new ArgumentException(message: $"Not support {_binaryNode.Kind()} operator")
                };
            }
            else if (node is IdentifierNameSyntax _identifierNode)
            {
                var _name = _sWhitespace.Replace(_identifierNode.Identifier.Text, "");
                if (KMStorageWrapper.TryConvert(_name, out IKey key))
                {
                    var _is_multi_key = key as IMultiKey;
                    return new GeneralKey(key.Id, key.IsMultiKey, _is_multi_key is null ? MapKeyType.None : _is_multi_key.KeyRelateType);
                }
                throw new KeyNotFoundException(message: $"Unable find {_name} GeneralKey instance");
            }
            else
            {
                throw new ArgumentException(message: $"Not support {node.Kind()} operator");
            }
        }
    }
}
