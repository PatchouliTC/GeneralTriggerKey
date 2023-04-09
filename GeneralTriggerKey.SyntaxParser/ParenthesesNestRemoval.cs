using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

#pragma warning disable CS8600,CS8602,CS8603,CS8604

namespace GeneralTriggerKey.SyntaxParser
{
    internal class ParenthesesNestRemoval : CSharpSyntaxRewriter
    {
        public static readonly HashSet<SyntaxKind> SupportBinaryExpressionSyntax = new HashSet<SyntaxKind> {
            SyntaxKind.BitwiseOrExpression,
            SyntaxKind.BitwiseAndExpression
        };

        public override SyntaxNode? VisitParenthesizedExpression(ParenthesizedExpressionSyntax node)
        {

            if (node.Parent == null)
            {
                ParenthesizedExpressionSyntax _temp = node;
                ExpressionSyntax _t = node.Expression;
                while (_temp != null)
                {
                    _temp = _temp.Expression as ParenthesizedExpressionSyntax;
                    _t = _temp?.Expression ?? _t;
                }
                return base.Visit(_t);
            }

            var new_node = RemoveParenthesesNesting(node);
            if (new_node.IsKind(SyntaxKind.ParenthesizedExpression))
            {
                var _temp_node = (ParenthesizedExpressionSyntax)new_node;

                if (
                    (_temp_node.Parent.IsKind(SyntaxKind.DivideExpression) || _temp_node.Parent.IsKind(SyntaxKind.AddExpression)) &&
                    (_temp_node.Expression.IsKind(SyntaxKind.DivideExpression) || _temp_node.Expression.IsKind(SyntaxKind.AddExpression))
                    )
                    return base.Visit((new_node as ParenthesizedExpressionSyntax).Expression);
                return base.VisitParenthesizedExpression(new_node as ParenthesizedExpressionSyntax);
            }

            return base.Visit(new_node);
        }

        public override SyntaxNode? VisitBinaryExpression(BinaryExpressionSyntax node)
        {
            BinaryExpressionSyntax _visit = null;
            //&>|
            //e.g:a|b&c->a|(b&c)


            if (SupportBinaryExpressionSyntax.Contains(node.Kind()))
            {

                //e.g:a|(b|(c+d))->a|b|(c+d)
                _visit = RemoveSameBitWiseParenthese(node);

                if (_visit != node)
                {
                    return base.VisitBinaryExpression(_visit);
                }

                ////e.g:a|b&c->a|(b&c)
                //if (node.IsKind(SyntaxKind.BitwiseOrExpression))
                //{
                //    ParenthesizedExpressionSyntax _left = null;
                //    ParenthesizedExpressionSyntax _right = null;
                //    if (node.Left.IsKind(SyntaxKind.BitwiseAndExpression))
                //    {
                //        _left = SyntaxFactory.ParenthesizedExpression(node.Left);
                //    }
                //    if (node.Right.IsKind(SyntaxKind.BitwiseAndExpression))
                //    {
                //        _right = SyntaxFactory.ParenthesizedExpression(node.Right);
                //    }
                //    if (_left != null | _right != null)
                //    {
                //        return base.VisitBinaryExpression(SyntaxFactory.BinaryExpression(_visit.Kind(), _left ?? _visit.Left, _right ?? _visit.Right));
                //    }
                //}

            }
            else if (node.IsKind(SyntaxKind.AddExpression) || node.IsKind(SyntaxKind.DivideExpression))
            {
                _visit = RemoveSameBitWiseParenthese(node);
                if (_visit != node)
                {
                    return base.VisitBinaryExpression(_visit);
                }
            }
            else if (node.IsKind(SyntaxKind.LeftShiftExpression))
            {

                if (!node.Left.IsKind(SyntaxKind.ParenthesizedExpression))
                {
                    var _temp_l = SyntaxFactory.ParenthesizedExpression(node.Left);
                    _visit = SyntaxFactory.BinaryExpression(node.Kind(), _temp_l, node.Right);
                    return base.VisitBinaryExpression(_visit);
                }
            }
            return base.VisitBinaryExpression(node);
        }

        /// <summary>
        /// 去除多层括号嵌套
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private ExpressionSyntax RemoveParenthesesNesting(ParenthesizedExpressionSyntax node)
        {
            if (node is null)
                return null;
            //e.g:(((xx)))->(xx)
            ExpressionSyntax currentNode = node;
            ExpressionSyntax nextExpression = node.Expression;

            while (currentNode.IsKind(SyntaxKind.ParenthesizedExpression) && nextExpression.IsKind(SyntaxKind.ParenthesizedExpression))
            {
                currentNode = nextExpression;
                nextExpression = ((ParenthesizedExpressionSyntax)nextExpression).Expression;
            }
            if (nextExpression.IsKind(SyntaxKind.SimpleMemberAccessExpression) || nextExpression.IsKind(SyntaxKind.IdentifierName))
            {
                currentNode = nextExpression;
            }

            return currentNode;
        }

        /// <summary>
        /// 去除相同符号类型连续使用时括号
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private BinaryExpressionSyntax RemoveSameBitWiseParenthese(BinaryExpressionSyntax? node)
        {
            if (SupportBinaryExpressionSyntax.Contains(node.Kind()))
            {
                var _left = node.Left as ParenthesizedExpressionSyntax;
                var _right = node.Right as ParenthesizedExpressionSyntax;

                BinaryExpressionSyntax _left_node = null;
                BinaryExpressionSyntax _right_node = null;

                if (_left != null &&
                    (_left.Expression.IsKind(node.Kind()) ||
                    SupportBinaryExpressionSyntax.Contains(node.Kind()) && (_left.Expression.IsKind(SyntaxKind.BitwiseAndExpression) && node.IsKind(SyntaxKind.BitwiseOrExpression))))
                {
                    _left_node = RemoveSameBitWiseParenthese(_left.Expression as BinaryExpressionSyntax);
                }

                if (_right != null &&
                    (_right.Expression.IsKind(node.Kind()) ||
                    SupportBinaryExpressionSyntax.Contains(node.Kind()) && (_right.Expression.IsKind(SyntaxKind.BitwiseAndExpression) && node.IsKind(SyntaxKind.BitwiseOrExpression))))
                {
                    _right_node = RemoveSameBitWiseParenthese(_right.Expression as BinaryExpressionSyntax);
                }

                if (_right_node != null || _left_node != null)
                    return SyntaxFactory.BinaryExpression(node.Kind(), _left_node ?? node.Left, _right_node ?? node.Right);

            }
            return node;
        }
    }
}
