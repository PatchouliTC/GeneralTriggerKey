using GeneralTriggerKey.Attributes;
using GeneralTriggerKey.Utils;
using Microsoft.Extensions.Logging;
using static GeneralTriggerKey.SyntaxParser.SyntaxParser;
using static GeneralTriggerKey.Operator;

namespace GeneralTriggerKey.UnitTest
{
    [MapEnum]
    public enum SystaxToKeyTestEnum
    {
        STKey1,
        STKey2,
        STKey3,
        STKey4
    }

    [TestClass]
    public class SyntaxParserTest
    {
        public SyntaxParserTest()
        {
            //GLogger.Instance.SetFactory(new NLogLoggerFactory());
            GLogger.Instance.SetFactory(LoggerFactory.Create(builder => builder.AddConsole()));
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                GLogger.Instance.GetLogger<Program>().LogCritical(e.ExceptionObject as Exception, "CriticalHappened");
            };

            InjectEnumsFromAssembly(typeof(SyntaxParserTest).Assembly);
        }

        [TestMethod]
        public void TestReadStringToSyntaxNode()
        {
            string triggercode = "((((((D|Y)&(C|U)&(F|(G|H&(K|T)))))";

            var result = ParseTextToSyntax(triggercode);

            Console.WriteLine(result);
        }

        [TestMethod]
        public void TestReadStringToSyntaxNodeWithDivide()
        {
            string triggercode = "((A&B)/(C&D))/(E&F)";
            var result = ParseTextToSyntax(triggercode);

            Console.WriteLine(result);
        }

        [TestMethod]
        public void TestStrToKeyNode()
        {
            string key_string = "((STKey4&STKey3)|(STKey1 & STKey2))/(STKey3|STKey1)";
            var key=G(key_string);
            Assert.IsTrue(key.Id > 0);
            var key2=G("(STKey4&STKey3)/STKey1");
            var key3 = G("ANY/STKey3");
            var key4 = G("(STKey4&STKey3)/(STKey3&STKey1)");
            var key6 = G("ANY/ANY");
            var key5 = G("(STKey4&STKey3)/(STKey3|STKey1)");
            Console.WriteLine(ToGraphvizCode());
        }

        [TestMethod]
        public void TestStrToKeyNode_2()
        {
            
            var key2 = G("(STKey4&STKey3)/STKey1");
            var key3 = G("ANY/STKey3");
            var key4 = G("(STKey4&STKey3)/(STKey3&STKey1)");
            var key6 = G("ANY/ANY");


            Console.WriteLine(ToGraphvizCode());
        }
    }
}