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
        D = 4,
        E = 5,
        F = 6,
        G = 7,
        H = 8,
        宣言,
        选择,
        发动,
    }
    [MemoryDiagnoser]
    public class CodeBenchmark
    {
        public CodeBenchmark()
        {
            InjectEnumsFromAssembly(typeof(CodeBenchmark).Assembly);
        }
        [Benchmark]
        public void TestSameCodeAdd()
        {
            var key = Q(MapTestEnum.F)&Q(MapTestEnum.H);

            _ = Q("testA") * key;
        }
    }
}
