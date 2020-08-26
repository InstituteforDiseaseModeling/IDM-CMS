using MathNet.Numerics.Random;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace distlib.randomvariates
{
    public class MersenneTwisterVariateGenerator : RandomVariateGenerator
    {
        private readonly MersenneTwister _random;

        private MersenneTwisterVariateGenerator(int seed = 1968)
        {
            _random = new MersenneTwister(seed);
        }

        public static MersenneTwisterVariateGenerator CreateMersenneTwisterVariateGenerator(uint[] seedData = null)
        {
            int seed = 1968;

            if ((seedData != null) && (seedData.Length > 0))
            {
                seed = (int)seedData[0];
            }
            
            var generator = new MersenneTwisterVariateGenerator(seed);

            return generator;
        }

        public double GenerateUniformOO()
        {
            double next;

            do
            {
                next = _random.NextDouble();
            } while (next == 0.0);

            return next;
        }

        public double GenerateUniformOC()
        {
            return (_random.NextInt64() + 1) / long.MaxValue;
        }

        public double GenerateUniformCO()
        {
            return _random.NextDouble();
        }

        public double GenerateUniformCC()
        {
            long random32 = (long) _random.NextFullRangeInt32();
            long intMinValue = (long)int.MinValue;
            long intMaxValue = (long)int.MaxValue;
            long range = intMaxValue - intMinValue + 1;
            random32 = random32 + (-1 * intMinValue);
            return (double)random32 / range;
        }

    }
}
