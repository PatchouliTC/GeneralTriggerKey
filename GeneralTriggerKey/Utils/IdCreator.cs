using IdGen;
using System;

namespace GeneralTriggerKey.Utils
{
    public class IdCreator
    {
        private IdGenerator _generator;
        private long require_times = 0;
        private static IdCreator s_instance = default!;
        public static IdCreator Instance
        {
            get
            {
                return s_instance ??= new IdCreator();
            }
        }

        private IdCreator()
        {
            _generator = new IdGenerator(0, new IdGeneratorOptions(timeSource: new DefaultTimeSource(DateTime.UtcNow)));
        }

        public DateTimeOffset IdTimeSeed
        {
            get => _generator.Options.TimeSource.Epoch;
        }
        public int GeneratorId
        {
            get => _generator.Id;
        }
        public bool IsGenedIds
        {
            get => require_times > 0;
        }

        /// <summary>
        /// 创建自定义配置的ID生成器
        /// </summary>
        /// <param name="init_seed"></param>
        /// <param name="time_seed"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public void SetCustomGenerator(int init_seed, in DateTime time_seed)
        {
            if (IsGenedIds)
                throw new InvalidOperationException("Id generator already be used,reset config will be cause duplicate id happened");

            _generator = new IdGenerator(init_seed, new IdGeneratorOptions(timeSource: new DefaultTimeSource(time_seed)));
        }

        /// <summary>
        /// 创建ID
        /// </summary>
        /// <returns></returns>
        public long GetId()
        {
            require_times += 1;
            return _generator.CreateId();
        }

        public override string ToString()
        {
            return $"IdGenerator Details:\nSeed:{_generator.Id}\nTimeSeed:{_generator.Options.TimeSource.Epoch}\nGenerate Times:{require_times}\nGenSpeed/ms:{_generator.Options.IdStructure.MaxGenerators * _generator.Options.IdStructure.MaxSequenceIds}";
        }
    }
}
