using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using GeneralTriggerKey.Attributes;
using static GeneralTriggerKey.Operator;

namespace GeneralTriggerKey.Benchmark
{
    [MapEnum]
    public enum MapTestEnum
    {
        [EnumAlia("testA")]
        A = 1,
        [EnumAlia("testB", "TESTB")]
        B = 2,
        C = 3,
    }
    [MemoryDiagnoser]
    public class CodeBenchmark
    { 

        public CodeBenchmark()
        {
            
        }

        [GlobalSetup]
        public void GlobalSetup()
        {
            InjectEnumsFromAssembly(typeof(CodeBenchmark).Assembly);
        }

        [Benchmark]
        public void TestCodeAdd()
        {
            var key = Q(MapTestEnum.B)&Q(MapTestEnum.A);

            _ = Q("testA") * key;
        }
        [Benchmark]
        public void TestCodebyStr()
        {
            var key = G("A|(B&C)");

            _ = Q("testA") * key;
        }

        [Benchmark]
        public void TestCodeCompareBoth()
        {
            var key1 = Q(MapTestEnum.B) & Q(MapTestEnum.A);

            var key2 = Q(MapTestEnum.C) & Q(MapTestEnum.A);

            _ = key1 * key2;
        }
    }
}
