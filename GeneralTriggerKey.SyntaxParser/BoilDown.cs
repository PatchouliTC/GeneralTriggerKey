using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;

#pragma warning disable CS8600,CS8602,CS8603,CS8604

namespace GeneralTriggerKey.SyntaxParser
{
    /// <summary>
    /// 归结与或关系
    /// </summary>
    internal class BoilDownSyntax : CSharpSyntaxRewriter
    {
        public static readonly HashSet<Type> BoilEndExpressionSyntax = new HashSet<Type> {
            typeof(IdentifierNameSyntax),
            typeof(MemberAccessExpressionSyntax),
        };
        public override SyntaxNode? VisitBinaryExpression(BinaryExpressionSyntax node)
        {
            if (node.IsKind(SyntaxKind.BitwiseAndExpression))
            {
                return BoilDown(node);
            }
            else if (node.IsKind(SyntaxKind.LeftShiftExpression))
            {
                var res = BoilDownAttachInfo(node);
                return base.VisitBinaryExpression(res as BinaryExpressionSyntax);
            }
            return base.VisitBinaryExpression(node);
        }

        private BinaryExpressionSyntax BoilDown(BinaryExpressionSyntax? exp)
        {
            if (exp == null)
                return exp;

            var _left = exp.Left;
            var _right = exp.Right;


            if (BoilEndExpressionSyntax.Contains(_left.GetType()) && BoilEndExpressionSyntax.Contains(_right.GetType()))
                return exp;


            if (_left.IsKind(SyntaxKind.ParenthesizedExpression))
            {
                _left = BoilDown((_left as ParenthesizedExpressionSyntax)?.Expression as BinaryExpressionSyntax);
            }
            else
            {
                _left = BoilDown(_left as BinaryExpressionSyntax);
            }
            if (_left == null)
                _left = exp.Left;

            if (_right.IsKind(SyntaxKind.ParenthesizedExpression))
            {
                _right = BoilDown((_right as ParenthesizedExpressionSyntax)?.Expression as BinaryExpressionSyntax);
                if (_right == null)
                    _right = exp.Right;
            }
            else
            {
                _right = BoilDown(_right as BinaryExpressionSyntax);
            }
            if (_right == null)
                _right = exp.Right;



            if (_left.IsKind(SyntaxKind.BitwiseOrExpression) || _right.IsKind(SyntaxKind.BitwiseOrExpression))
            {
                var _l_b = _left as BinaryExpressionSyntax;
                List<ExpressionSyntax> required_add_left = new List<ExpressionSyntax>();
                List<ExpressionSyntax> required_add_right = new List<ExpressionSyntax>();
                GetAllOrRelation(_left, ref required_add_left);
                GetAllOrRelation(_right, ref required_add_right);
                List<BinaryExpressionSyntax> nodes = new List<BinaryExpressionSyntax>();
                foreach (var l_node in required_add_left)
                {
                    foreach (var r_node in required_add_right)
                    {
                        nodes.Add(SyntaxFactory.BinaryExpression(SyntaxKind.BitwiseAndExpression, l_node, r_node));
                    }
                }


                BinaryExpressionSyntax _new_node = null;
                for (int i = 1; i < nodes.Count; i++)
                {
                    if (_new_node == null)
                    {
                        _new_node = SyntaxFactory.BinaryExpression(SyntaxKind.BitwiseOrExpression, nodes[0], nodes[1]);
                        continue;
                    }
                    _new_node = SyntaxFactory.BinaryExpression(SyntaxKind.BitwiseOrExpression, _new_node, nodes[i]);
                }
                return _new_node;
            }

            return exp;
        }

        private ExpressionSyntax BoilDownAttachInfo(ExpressionSyntax? exp)
        {
            ExpressionSyntax truth_exp = exp;

            if (exp.IsKind(SyntaxKind.ParenthesizedExpression))
            {
                while (truth_exp.IsKind(SyntaxKind.ParenthesizedExpression))
                    truth_exp = (truth_exp as ParenthesizedExpressionSyntax).Expression;
            }


            if (truth_exp.IsKind(SyntaxKind.LeftShiftExpression))
            {
                var next_exp = BoilDownAttachInfo((truth_exp as BinaryExpressionSyntax).Left);

                if (next_exp.IsKind(SyntaxKind.LeftShiftExpression))
                {
                    //a<<b<<c->a<<(b+c)
                    var _temp = next_exp as BinaryExpressionSyntax;
                    var _temp_right = SyntaxFactory.ParenthesizedExpression(SyntaxFactory.BinaryExpression(SyntaxKind.AddExpression, _temp.Right, (truth_exp as BinaryExpressionSyntax).Right));
                    return SyntaxFactory.ParenthesizedExpression(SyntaxFactory.BinaryExpression(_temp.Kind(), _temp.Left, _temp_right));
                }

                else if (next_exp.IsKind(SyntaxKind.AddExpression))
                {
                    var _temp_next = next_exp as BinaryExpressionSyntax;
                    ExpressionSyntax _left = null;
                    ExpressionSyntax _right = null;
                    if (_temp_next.Left.IsKind(SyntaxKind.LeftShiftExpression))
                    {
                        var _temp_left_right = SyntaxFactory.ParenthesizedExpression(SyntaxFactory.BinaryExpression(SyntaxKind.AddExpression, (_temp_next.Left as BinaryExpressionSyntax).Right, (truth_exp as BinaryExpressionSyntax).Right));
                        _left = SyntaxFactory.ParenthesizedExpression(SyntaxFactory.BinaryExpression(SyntaxKind.LeftShiftExpression, (_temp_next.Left as BinaryExpressionSyntax).Left, _temp_left_right));
                    }
                    else
                    {
                        _left = SyntaxFactory.ParenthesizedExpression(SyntaxFactory.BinaryExpression(SyntaxKind.LeftShiftExpression, _temp_next.Left, (truth_exp as BinaryExpressionSyntax).Right));
                    }

                    if (_temp_next.Right.IsKind(SyntaxKind.LeftShiftExpression))
                    {
                        var _temp_right_right = SyntaxFactory.ParenthesizedExpression(SyntaxFactory.BinaryExpression(SyntaxKind.AddExpression, (_temp_next.Right as BinaryExpressionSyntax).Right, (truth_exp as BinaryExpressionSyntax).Right));
                        _right = SyntaxFactory.ParenthesizedExpression(SyntaxFactory.BinaryExpression(SyntaxKind.LeftShiftExpression, (_temp_next.Right as BinaryExpressionSyntax).Left, _temp_right_right));
                    }
                    else
                    {
                        _right = SyntaxFactory.ParenthesizedExpression(SyntaxFactory.BinaryExpression(SyntaxKind.LeftShiftExpression, _temp_next.Right, (truth_exp as BinaryExpressionSyntax).Right));
                    }
                    return SyntaxFactory.BinaryExpression(next_exp.Kind(), _left, _right);
                }

                return truth_exp;

            }
            else if (truth_exp.IsKind(SyntaxKind.AddExpression))
            {
                var left_result = BoilDownAttachInfo((truth_exp as BinaryExpressionSyntax).Left);
                var right_result = BoilDownAttachInfo((truth_exp as BinaryExpressionSyntax).Right);
                return SyntaxFactory.BinaryExpression(truth_exp.Kind(), left_result, right_result);
            }

            return exp;
        }

        private void GetAllOrRelation(ExpressionSyntax? node, ref List<ExpressionSyntax> nlist)
        {
            if (node == null)
                return;
            if (BoilEndExpressionSyntax.Contains(node.GetType()))
            {
                nlist.Add(node);
                return;
            }
            var b_node = node as BinaryExpressionSyntax;
            if (b_node == null)
                return;

            var _left = b_node.Left;
            var _right = b_node.Right;
            if (_left.IsKind(SyntaxKind.BitwiseOrExpression))
            {
                GetAllOrRelation(_left as BinaryExpressionSyntax, ref nlist);
            }
            else
            {
                nlist.Add(_left);
            }

            if (_right.IsKind(SyntaxKind.BitwiseOrExpression))
            {
                GetAllOrRelation(_right as BinaryExpressionSyntax, ref nlist);
            }
            else
            {
                nlist.Add(_right);
            }
        }
    }
}
