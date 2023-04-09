using BenchmarkDotNet.Running;
using GeneralTriggerKey.Attributes;
using GeneralTriggerKey.Benchmark;
using GeneralTriggerKey.KeyMap;
using GeneralTriggerKey.Utils;
using GeneralTriggerKey.Utils.Extensions;
using Microsoft.Extensions.Logging;
using static GeneralTriggerKey.Operator;

public class Program
{
    public static void Main(string[] args)
    {
        //GLogger.Instance.SetFactory(new NLogLoggerFactory());
        GLogger.Instance.SetFactory(LoggerFactory.Create(builder => builder.AddConsole()));
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            GLogger.Instance.GetLogger<Program>().LogCritical(e.ExceptionObject as Exception, "CriticalHappened");
        };

        InjectEnumsFromAssembly(typeof(Program).Assembly);

        var summary = BenchmarkRunner.Run<CodeBenchmark>();
    }
}