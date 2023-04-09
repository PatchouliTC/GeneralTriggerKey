using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;

namespace GeneralTriggerKey.Utils
{
    public class GLogger
    {
        private ILoggerFactory _factory = NullLoggerFactory.Instance;

        private static GLogger s_instance = default!;

        public static GLogger Instance
        {
            get
            {
                return s_instance ??= new GLogger();
            }
        }
        private GLogger() { }


        public ILoggerFactory GlobalLoggerFactory
        {
            get { return _factory; }
        }

        public bool IsNullLogger
        {
            get
            {
                return _factory is NullLoggerFactory;
            }
        }

        public void SetFactory(ILoggerFactory _factory)
        {
            this._factory = _factory;
        }
        public ILogger GetLogger<T>()
        {
            return _factory.CreateLogger<T>();
        }
        public ILogger GetLogger(string name)
        {
            return _factory.CreateLogger(name);
        }
        public ILogger GetLogger(Type type)
        {
            return _factory.CreateLogger(type);
        }
    }
}
