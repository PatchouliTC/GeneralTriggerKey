using IdGen;
using System;

namespace GeneralTriggerKey.Utils
{
    /// <summary>
    /// 内部Id生成器
    /// </summary>
    public class IdCreator
    {
        private IdGenerator _generator;
        private long require_times = 0;
        private static IdCreator s_instance = default!;
        /// <summary>
        /// 
        /// </summary>
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
        /// <summary>
        /// 当前时间种子
        /// </summary>
        public DateTimeOffset IdTimeSeed
        {
            get => _generator.Options.TimeSource.Epoch;
        }
        /// <summary>
        /// 当前适用ID
        /// </summary>
        public int GeneratorId
        {
            get => _generator.Id;
        }
        /// <summary>
        /// 是否生成过了Id
        /// </summary>
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
