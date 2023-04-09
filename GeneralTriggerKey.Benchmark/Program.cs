using BenchmarkDotNet.Running;
using GeneralTriggerKey.Benchmark;
using GeneralTriggerKey.Utils;
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
        //InjectEnumsFromAssembly(typeof(Program).Assembly);
        //var key=Q(MapTestEnum.A) & Q(MapTestEnum.B);
        //Console.WriteLine(ShowAllNodes());
        //Console.WriteLine(ToGraphvizCode());
        var summary = BenchmarkRunner.Run<CodeBenchmark>();
    }
}