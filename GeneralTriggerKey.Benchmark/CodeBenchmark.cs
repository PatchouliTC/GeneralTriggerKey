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
        STKey1,
        STKey2,
        STKey3,
        STKey4
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
        public void TestCodebyStr()
        {
            var key1 = G("((STKey4&STKey3)|(STKey1))/(STKey3|STKey1)");

            var key2 = G("(STKey4&STKey3&STKey4)/STKey1");

            var key3 = G("(STKey4&STKey3&STKey1&STKey2)/STKey1");

            var res=key3 * key1;
        }

    }
}
