# GeneralTriggerKey
let project enum\[runtime custom key] into one map and create unique key for them 
which also support and\or relate and check trigger support with these uniquekey

# How To Use
## Add Enums
Mark enums that you want to auto registered
```cs
using GeneralTriggerKey.Attributes;
[MapEnum]
public enum MapEnum
{
    [EnumAlia("tA")]
    A = 1,
    [EnumAlia("tB", "TB")]
    B = 2,
    C = 3,
    D = 4,
}
```
Note: each enum value can use ```EnumAlia```for it's short alia

Register target assembly when your project init
```cs
using static GeneralTriggerKey.Operator;
InjectEnumsFromAssembly(typeof(<AnyClassType>).Assembly);
```

## Get/Compare keys
All need functions are in Operator,you can use it like these
```cs
using static GeneralTriggerKey.Operator;
var k1=G("((A&B)|(C&D))/(A|C)");//get key from string expression
var k2=Q(MapTestEnum.C);//get key from known enum key
var k3=Q("tB");//get key by name or alia--if enums,you can use it's key name
var k4=R("runtime_custom_new_key");//try register a new runtime added key
var k5=Q(MapTestEnum.C)&Q("tB");//Each key can use & or | mark to get new relate key
bool cantrigger=k5 * k1;//you can use * mark to quick compare <left> can trigger <right> (left * right) ,for this compare means [k5--C&B can trigger k1--A&B|A&C]
```

## Note
This lib has a default logger,you can set logger factory to receive logger info
```cs
using Microsoft.Extensions.Logging;
using GeneralTriggerKey.Utils;
GLogger.Instance.SetFactory(LoggerFactory.Create(builder => builder.AddConsole()));//Set default console output
```
Support set custom id generator seed and time_seed
```cs
IdCreator.Instance.SetCustomGenerator(114514, DateTime.UtcNow);
```
Warning:If start inject or any get/create nodes operator,this creator won't able to change seed [To avoid id collision]

## Output for check
ShowAllNodes() can let you know every node that been registered
```
************Node List************
[EnumKey](184549376)<A>-OId:1=>Belong:[EnumGroup](171966464)<GeneralTriggerKey.Benchmark.MapTestEnum>
[EnumKey](184549377)<B>-OId:2=>Belong:[EnumGroup](171966464)<GeneralTriggerKey.Benchmark.MapTestEnum>
[EnumKey](184549378)<C>-OId:3=>Belong:[EnumGroup](171966464)<GeneralTriggerKey.Benchmark.MapTestEnum>
[Multikey](301989888)<A&B>{M=AND-3gJVypahpelDGp}
  [EnumKey](184549376)<A>-OId:1=>Belong:[EnumGroup](171966464)<GeneralTriggerKey.Benchmark.MapTestEnum>
  [EnumKey](184549377)<B>-OId:2=>Belong:[EnumGroup](171966464)<GeneralTriggerKey.Benchmark.MapTestEnum>
```

ToGraphvizCode() can generate graphviz code,you can copy them into [GraphvizOnline](https://dreampuf.github.io/GraphvizOnline/) or other generate to know relationship
```
************Copy Code************
digraph G {
184549376 [label="[E]A"];
184549377 [label="[E]B"];
184549378 [label="[E]C"];
301989888 [label="[M]A&B"];
301989888 -> 184549376;
301989888 -> 184549377;
}
```
