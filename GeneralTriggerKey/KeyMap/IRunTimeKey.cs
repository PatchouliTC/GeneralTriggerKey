using System;
using System.Collections.Generic;
using System.Text;

namespace GeneralTriggerKey.KeyMap
{
    internal interface IRunTimeKey : ISimpleNode
    {
        public string Range { get; }
    }
}
