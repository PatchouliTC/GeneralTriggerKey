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
            var afterCheckSyntaxNodes = ParseTextToSyntax(key_string);
            return TransFormNode(afterCheckSyntaxNodes!);
        }

        private static readonly Regex _sWhiteSpace = new Regex(@"\s+");
        private static GeneralKey TransFormNode(SyntaxNode node)
        {
            //已经过滤重组,剩余内容只存在& | ( )
            //暂不考虑+ <<
            //( )继续往深处递归
            if (node is ParenthesizedExpressionSyntax parenthesizedNode)
            {
                return TransFormNode(parenthesizedNode.Expression);
            }
            //& | 逻辑符号,继续往深处递归左右两边
            else if (node is BinaryExpressionSyntax binaryNode)
            {
                
                var left = TransFormNode(binaryNode.Left);
                var right = TransFormNode(binaryNode.Right);
                return binaryNode.Kind() switch
                {
                    SyntaxKind.BitwiseAndExpression => left & right,
                    SyntaxKind.BitwiseOrExpression => left | right,
                    SyntaxKind.DivideExpression => left / right,
                    _ => throw new ArgumentException(message: $"Not support {binaryNode.Kind()} operator")
                };
            }
            else if (node is IdentifierNameSyntax identifierNode)
            {
                var filteredName = _sWhiteSpace.Replace(identifierNode.Identifier.Text, "");
                if (KeyMapStorage.Instance.TryConvert(filteredName, out IKey key))
                {
                    return new GeneralKey(key.Id, key.IsMultiKey, key.KeyRelateType);
                }
                throw new KeyNotFoundException(message: $"Unable find {filteredName} GeneralKey instance");
            }
            else
            {
                throw new ArgumentException(message: $"Not support {node.Kind()} operator");
            }
        }
    }
}
