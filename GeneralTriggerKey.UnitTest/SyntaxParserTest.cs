using static GeneralTriggerKey.SyntaxParser.SyntaxParser;

namespace GeneralTriggerKey.UnitTest
{
    [TestClass]
    public class SyntaxParserTest
    {
        [TestMethod]
        public void TestReadStringToSyntaxNode()
        {
            string triggercode = "((((((D|Y)&(C|U)&(F|(G|H&(K|T)))))";

            var result = ParseTextToSyntax(triggercode);

            Console.WriteLine(result);
        }
    }
}