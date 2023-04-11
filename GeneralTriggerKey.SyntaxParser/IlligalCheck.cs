using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;

namespace GeneralTriggerKey.SyntaxParser
{
    /// <summary>
    /// 非法内容检查
    /// </summary>
    internal class IlligalCheck : CSharpSyntaxWalker
    {
        public static readonly HashSet<SyntaxKind> SupportBinaryExpressionSyntax = new HashSet<SyntaxKind> {
            SyntaxKind.BitwiseOrExpression,
            SyntaxKind.BitwiseAndExpression,
            //SyntaxKind.AddExpression,
            SyntaxKind.DivideExpression,
            //SyntaxKind.LeftShiftExpression,
        };
        public static readonly HashSet<SyntaxKind> SupportConcatExpressionSyntax = new HashSet<SyntaxKind> {
            SyntaxKind.AddExpression,
            SyntaxKind.DivideExpression,
            SyntaxKind.LeftShiftExpression,
            SyntaxKind.ParenthesizedExpression
        };
        public static readonly HashSet<Type> SupportExpressionSyntax = new HashSet<Type> {
            typeof(BinaryExpressionSyntax),
            typeof(ParenthesizedExpressionSyntax),
            typeof(IdentifierNameSyntax),
            typeof(MemberAccessExpressionSyntax),
        };

        public static readonly HashSet<Type> EndExpressionSyntax = new HashSet<Type> {
            typeof(IdentifierNameSyntax),
            typeof(MemberAccessExpressionSyntax),
        };

        public override void VisitBinaryExpression(BinaryExpressionSyntax node)
        {
            //仅接受& |
            //+ / << support in future...
            if (!SupportBinaryExpressionSyntax.Contains(node.Kind()))
                throw new InvalidOperationException($"Not Support Binary operator:{node}");

            if (node.IsKind(SyntaxKind.DivideExpression))
            {
                // /必须作为&/单元外层连接使用
                // /可以与/任意链接,不能成为& |子节点
                if (node.Parent != null && !SupportConcatExpressionSyntax.Contains(node.Parent.Kind()))
                    throw new InvalidOperationException($"'/' must connect with other '/' '&' '<<' unit ({node.Parent ?? node})");
                // /只能作为+子节点(a/b+c/d)
                if (node.Left.IsKind(SyntaxKind.AddExpression) || node.Right.IsKind(SyntaxKind.AddExpression))
                    throw new InvalidOperationException($"'/' can't link '+' node ({node})");
            }
            else if (node.IsKind(SyntaxKind.LeftShiftExpression))
            {

                foreach (var n in node.Right.DescendantNodes())
                {
                    if (n.IsKind(SyntaxKind.AddExpression) || n.IsKind(SyntaxKind.ParenthesizedExpression) || EndExpressionSyntax.Contains(n.GetType()))
                        continue;
                    throw new InvalidOperationException($"Not allow any symbol at right '<<' except '+' '()' ({n})");
                }

                bool _has_add_exp = false;
                foreach (var n in node.Ancestors())
                {
                    if (n.IsKind(SyntaxKind.AddExpression))
                        _has_add_exp = true;

                    if (n.IsKind(SyntaxKind.AddExpression) || n.IsKind(SyntaxKind.ParenthesizedExpression))
                        continue;

                    if (n.IsKind(SyntaxKind.LeftShiftExpression))
                    {
                        if (_has_add_exp)
                        {
                            _has_add_exp = false;
                            continue;
                        }
                    }
                    throw new InvalidOperationException($"Not allow any symbol at header << except + () ({n})");
                }

                // 可以作为根节点(a<<Q),可以作为+子节点(a<<Q+b<<Q),不能相互连接
                if (node.Parent != null && !(node.Parent.IsKind(SyntaxKind.AddExpression) || node.Parent.IsKind(SyntaxKind.ParenthesizedExpression)))
                    throw new InvalidOperationException($"'/' must connect with other '/' '&' unit ({node.Parent ?? node})");
            }

            base.VisitBinaryExpression(node);
        }
    }
}
