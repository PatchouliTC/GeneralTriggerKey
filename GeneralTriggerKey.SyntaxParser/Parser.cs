using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace GeneralTriggerKey.SyntaxParser
{
    public static class SyntaxParser
    {
        private static IlligalCheck _check = new IlligalCheck();
        private static ParenthesesNestRemoval _parentheseremover = new ParenthesesNestRemoval();
        private static BoilDownSyntax _boildown = new BoilDownSyntax();

        //string TriggerCode = "((((((D|Y)&(C|U)&(F|(G|H&(K|T)))))/(L&M|N)<<(X+Y))+((D|Y)&(C|U))/(R&E))<<K";
        //string test_con = "~/A/C/~~/D";

        public static SyntaxNode? ParseTextToSyntax(string text)
        {
            //& | () only
            //+ / << support in future
            if (text == null || text == String.Empty)
                //throw new ArgumentNullException("Not Support parese empty string");
                return null;
            ExpressionSyntax? codesyntax = null;
            try
            {
                codesyntax = SyntaxFactory.ParseExpression(text);
            }
            catch (Exception e)
            {
                throw new InvalidCastException($"Parse Error.({e})");
            }
            _check.Visit(codesyntax);
            var _result = _boildown.Visit(_parentheseremover.Visit(codesyntax));
            return _parentheseremover.Visit(_result);
        }
    }
}
