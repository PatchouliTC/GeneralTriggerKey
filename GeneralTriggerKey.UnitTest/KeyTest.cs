using GeneralTriggerKey.Attributes;
using GeneralTriggerKey.KeyMap;
using GeneralTriggerKey.Utils;
using GeneralTriggerKey.Utils.Extensions;
using Microsoft.Extensions.Logging;
using static GeneralTriggerKey.Operator;

namespace GeneralTriggerKey.UnitTest
{
    [MapEnum]
    public enum MapTestEnum
    {
        [EnumAlia("testA")]
        A = 1,
        [EnumAlia("testB", "TESTB")]
        B = 2,
        C = 3,
        D = 4,
        E = 5,
        F = 6,
        G = 7,
        H = 8,
        宣言,
        选择,
        发动,
    }


    [TestClass]
    public class EnumMapTest
    {
        public EnumMapTest()
        {
            //GLogger.Instance.SetFactory(new NLogLoggerFactory());
            GLogger.Instance.SetFactory(LoggerFactory.Create(builder => builder.AddConsole()));
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                GLogger.Instance.GetLogger<Program>().LogCritical(e.ExceptionObject as Exception, "CriticalHappened");
            };

            InjectEnumsFromAssembly(typeof(EnumMapTest).Assembly);
        }

        [TestMethod]
        public void TestAutoInjectEnums()
        {

            var id = KMStorageWrapper.Convert(MapTestEnum.A);
            Assert.IsTrue(id>0);
        }

        [TestMethod]
        public void TestConvertByName()
        {

            var id = KMStorageWrapper.Convert("testA");
            Assert.IsTrue(id > 0);
        }

        [TestMethod]
        public void TestConvertByNameForMulitNameField()
        {

            var id = KMStorageWrapper.Convert("TESTB");
            Assert.IsTrue(id > 0);
        }

        [TestMethod]
        public void TestAddAndRelateMultiKey()
        {

            var idA = KMStorageWrapper.Convert(MapTestEnum.A);
            var idB = KMStorageWrapper.Convert(MapTestEnum.B);
            var idC = KMStorageWrapper.Convert(MapTestEnum.C);
            var idD = KMStorageWrapper.Convert(MapTestEnum.D);
            var idE = KMStorageWrapper.Convert(MapTestEnum.E);
            var idF = KMStorageWrapper.Convert(MapTestEnum.F);
            var idG = KMStorageWrapper.Convert(MapTestEnum.G);
            var idH = KMStorageWrapper.Convert(MapTestEnum.H);

            KMStorageWrapper.TryRegisterRunTimeKey(out var r1, "testRun");

            KMStorageWrapper.TryRegisterMultiKey(out var id6, MapKeyType.AND, idD, idC, idE, idF);
            Assert.IsTrue(id6 > 0);
            KMStorageWrapper.TryRegisterMultiKey(out var id1, MapKeyType.AND, idA, idB);
            Assert.IsTrue(id1 > 0);
            KMStorageWrapper.TryRegisterMultiKey(out var id2, MapKeyType.AND, idA, idB);
            Assert.IsTrue(id2 == id1);
            KMStorageWrapper.TryRegisterMultiKey(out var id3, MapKeyType.AND, idC, idE);
            Assert.IsTrue(id3 > 0);
            KMStorageWrapper.TryRegisterMultiKey(out var id4, MapKeyType.AND, idC, idD);
            Assert.IsTrue(id4 > 0);
            KMStorageWrapper.TryRegisterMultiKey(out var id5, MapKeyType.AND, idA, idB, idC, idE, idF);
            Assert.IsTrue(id5 > 0);

            KMStorageWrapper.TryRegisterMultiKey(out var id7, MapKeyType.AND, idC, idE, idF);
            Assert.IsTrue(id7 > 0);

            KMStorageWrapper.TryRegisterMultiKey(out var id8, MapKeyType.AND, idD, idC, idE, idF, idG);

            KMStorageWrapper.TryRegisterMultiKey(out var id9, MapKeyType.AND, idA, idB, idH, idG);

            KMStorageWrapper.TryRegisterMultiKey(out var id10, MapKeyType.AND, idA, idB, idH);

            KMStorageWrapper.TryRegisterMultiKey(out var id11, MapKeyType.AND, idB, idH);


            KMStorageWrapper.TryRegisterMultiKey(out var id12, MapKeyType.OR, id1, id3);

            KMStorageWrapper.TryRegisterMultiKey(out var id13, MapKeyType.OR, id3, id4);

            KMStorageWrapper.TryRegisterMultiKey(out var id14, MapKeyType.OR, id1, id3, id4);
            KMStorageWrapper.TryRegisterMultiKey(out var id15, MapKeyType.OR, id14, id3, id4, r1);


            Assert.IsTrue(id14.Contains(id1));
            Assert.IsTrue(id14.Contains(id12));
            Assert.IsTrue(idA.Contains(idA));

            Assert.IsTrue(idA.Contains(idA));

        }
        [TestMethod]
        public void TestKeyMapContainExtensions()
        {
            var idA = KMStorageWrapper.Convert(MapTestEnum.A);
            var idB = KMStorageWrapper.Convert(MapTestEnum.B);
            var idC = KMStorageWrapper.Convert(MapTestEnum.C);
            var idD = KMStorageWrapper.Convert(MapTestEnum.D);
            var idE = KMStorageWrapper.Convert(MapTestEnum.E);
            var idF = KMStorageWrapper.Convert(MapTestEnum.F);
            var idG = KMStorageWrapper.Convert(MapTestEnum.G);
            var idH = KMStorageWrapper.Convert(MapTestEnum.H);


            KMStorageWrapper.TryRegisterMultiKey(out var id1, MapKeyType.AND, idA, idB);
            KMStorageWrapper.TryRegisterMultiKey(out var id3, MapKeyType.AND, idC, idE);
            KMStorageWrapper.TryRegisterMultiKey(out var id4, MapKeyType.AND, idC, idD);
            KMStorageWrapper.TryRegisterMultiKey(out var id5, MapKeyType.AND, idA, idB, idC, idE, idF);
            KMStorageWrapper.TryRegisterMultiKey(out var id7, MapKeyType.AND, idC, idE, idF);
            KMStorageWrapper.TryRegisterMultiKey(out var id12, MapKeyType.OR, id1, id3);
            KMStorageWrapper.TryRegisterMultiKey(out var id14, MapKeyType.OR, idA, id1, id3, id4);

            Assert.IsTrue(id14.Contains(id1));//(A&B) IN A|(A&B)|(C&E)|(C&D)
            Assert.IsTrue(id14.Contains(id12));
            Assert.IsTrue(id14.Contains(idA));
            Assert.IsFalse(id14.Contains(id7));

            Assert.IsTrue(idA.Contains(idA));
            Assert.IsFalse(idA.Contains(idB));

            Assert.IsTrue(id5.Contains(id1));
            Assert.IsFalse(id5.Contains(id12));
            Assert.IsTrue(id5.Contains(idA));
        }

        [TestMethod]
        public void TestKeyMapAddExtensions()
        {

            var idA = KMStorageWrapper.Convert(MapTestEnum.A);
            var idB = KMStorageWrapper.Convert(MapTestEnum.B);
            var idC = KMStorageWrapper.Convert(MapTestEnum.C);
            var idD = KMStorageWrapper.Convert(MapTestEnum.D);
            var idE = KMStorageWrapper.Convert(MapTestEnum.E);
            var idF = KMStorageWrapper.Convert(MapTestEnum.F);
            var idG = KMStorageWrapper.Convert(MapTestEnum.G);
            var idH = KMStorageWrapper.Convert(MapTestEnum.H);

            Assert.IsTrue(idA.AndWith(idB, out var id1));//a&b
            Assert.IsTrue(idC.AndWith(idD, out var id2));//c&d
            Assert.IsTrue(idE.OrWith(idF, out var id3));//e|f
            Assert.IsTrue(idG.OrWith(idH, out var id4));//g|h

            Assert.IsTrue(id1.AndWith(idH, out var _));//(a&b)&h
            Assert.IsTrue(id1.AndWith(id2, out var _));//(a&b)&(c&d)
            Assert.IsTrue(id1.AndWith(id3, out var _));//(A&B)&(E|F)
            Assert.IsTrue(id3.AndWith(id4, out var _));//(E|F)&(G|H)

            Assert.IsTrue(id3.OrWith(id4, out var _));//(E|F)|(G|H)
            Assert.IsTrue(id3.OrWith(id1, out var _));//(E|f)|(A&B)
            Assert.IsTrue(id1.OrWith(id2, out var _));//(A&B)|(C&D)
        }

        [TestMethod]
        public void TestKeyMapMathExtensions()
        {

            var idA = KMStorageWrapper.Convert(MapTestEnum.A);
            var idB = KMStorageWrapper.Convert(MapTestEnum.B);
            var idC = KMStorageWrapper.Convert(MapTestEnum.C);
            var idD = KMStorageWrapper.Convert(MapTestEnum.D);
            var idE = KMStorageWrapper.Convert(MapTestEnum.E);
            var idF = KMStorageWrapper.Convert(MapTestEnum.F);
            var idG = KMStorageWrapper.Convert(MapTestEnum.G);
            var idH = KMStorageWrapper.Convert(MapTestEnum.H);

            idA.AndWith(idB, out var id1);//a&b
            idC.AndWith(idD, out var id2);//c&d
            idE.OrWith(idF, out var id3);//e|f
            idG.OrWith(idH, out var id4);//g|h

            id1.AndWith(idH, out var id5);//(a&b)&h

            id3.OrWith(id4, out var id6);//(E|F)|(G|H)

            Assert.IsTrue(id5.SymmetricExceptWith(id2, out var id7));
            Assert.IsTrue(id5.SymmetricExceptWith(idB, out var id8));

            Assert.IsTrue(id5.IntersectWith(id1, out var id9));
            Assert.IsTrue(id5.IntersectWith(idB, out var id10));
        }

        [TestMethod]
        public void TestCanTriggerExtension()
        {

            var idA = KMStorageWrapper.Convert(MapTestEnum.A);
            var idB = KMStorageWrapper.Convert(MapTestEnum.B);
            var idC = KMStorageWrapper.Convert(MapTestEnum.C);
            var idD = KMStorageWrapper.Convert(MapTestEnum.D);
            var idE = KMStorageWrapper.Convert(MapTestEnum.E);
            var idF = KMStorageWrapper.Convert(MapTestEnum.F);
            var idG = KMStorageWrapper.Convert(MapTestEnum.G);
            var idH = KMStorageWrapper.Convert(MapTestEnum.H);

            idA.AndWith(idB, out var id1);//a&b
            idC.AndWith(idD, out var id2);//c&d
            idE.OrWith(idF, out var id3);//e|f
            idG.OrWith(idH, out var id4);//g|h

            id1.AndWith(idH, out var id5);//(a&b)&h

            id3.OrWith(id4, out var id6);//(E|F)|(G|H)

            Assert.IsTrue(id5.CanTrigger(id1));//and触发and
            Assert.IsTrue(id5.CanTrigger(idH));//and触发single
            Assert.IsTrue(id5.CanTrigger(id4));//and触发or


            Console.WriteLine(ShowAllNodes());
            Console.WriteLine(ToGraphvizCode());

        }

        [TestMethod]
        public void TestKeyMapQ()
        {
            KMStorageWrapper.TryRegisterRunTimeKey(out var _rid, "new_key");
            var key = Q("new_key") & (Q(MapTestEnum.B) | (Q("testA")) & Q(MapTestEnum.E));

            var key2 = Q(MapTestEnum.B) & Q("new_key");


            KMStorageWrapper.TryRegisterRunTimeKey(out var _tempid, "OB");

            var effect = Q(MapTestEnum.宣言) & Q("OB") | Q(MapTestEnum.选择);

            var eventhappened = Q(MapTestEnum.发动) & Q(MapTestEnum.宣言) & Q("OB");

            Assert.IsTrue(eventhappened * effect);
        }

        [TestMethod]
        public void TestKeyDivide_v1()
        {
            var idA = KMStorageWrapper.Convert(MapTestEnum.A);
            var idB = KMStorageWrapper.Convert(MapTestEnum.B);
            var idC = KMStorageWrapper.Convert(MapTestEnum.C);
            var idD = KMStorageWrapper.Convert(MapTestEnum.D);
            var idE = KMStorageWrapper.Convert(MapTestEnum.E);
            var idF = KMStorageWrapper.Convert(MapTestEnum.F);
            var idG = KMStorageWrapper.Convert(MapTestEnum.G);
            var idH = KMStorageWrapper.Convert(MapTestEnum.H);

            idA.AndWith(idB, out var idAB);
            idC.AndWith(idD, out var idDC);
            idE.AndWith(idF, out var idEF);

            idAB.ConnectWith(idDC, 1, out var id1_2);
            idDC.ConnectWith(idEF, 2, out var id2_3);

            id1_2.DivideWith(id2_3,out var id_divide1);

            Assert.IsTrue(id_divide1 > 0);

            Console.WriteLine(ShowAllNodes());

        }

        [TestMethod]
        public void TestKeyDivide_v2()
        {
            var idA = KMStorageWrapper.Convert(MapTestEnum.A);
            var idB = KMStorageWrapper.Convert(MapTestEnum.B);
            var idC = KMStorageWrapper.Convert(MapTestEnum.C);
            var idD = KMStorageWrapper.Convert(MapTestEnum.D);
            var idE = KMStorageWrapper.Convert(MapTestEnum.E);
            var idF = KMStorageWrapper.Convert(MapTestEnum.F);
            var idG = KMStorageWrapper.Convert(MapTestEnum.G);
            var idH = KMStorageWrapper.Convert(MapTestEnum.H);

            idA.AndWith(idB, out var idAB);
            idC.AndWith(idD, out var idDC);
            idE.AndWith(idF, out var idEF);

            idAB.ConnectWith(idDC, 1, out var id1_2);
            idDC.ConnectWith(idEF, 2, out var id2_3);

            id1_2.DivideWith(id2_3, out var id_divide1);

            id1_2.DivideWith(idE,out var id_divide2);

            idAB.DivideWith(idC, out var id_divide3);

            Assert.IsTrue(id_divide1 > 0);

            Console.WriteLine(ShowAllNodes());

        }
    }
}
