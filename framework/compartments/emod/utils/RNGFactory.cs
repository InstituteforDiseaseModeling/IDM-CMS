/***************************************************************************************************

Copyright (c) 2018 Intellectual Ventures Property Holdings, LLC (IVPH) All rights reserved.

EMOD is licensed under the Creative Commons Attribution-Noncommercial-ShareAlike 4.0 License.
To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-sa/4.0/legalcode

***************************************************************************************************/

using System;
using distlib.randomvariates;

namespace compartments.emod.utils
{
    // ReSharper disable once InconsistentNaming
    public class RNGFactory
    {
        // ReSharper disable InconsistentNaming
        public enum GeneratorType
        {
            VANILLA,
            RANDLIB
        }
        // ReSharper restore InconsistentNaming

        private static GeneratorType _generatorType;
        private static uint _seedIndex;
        private static uint _generatorSeed;
        private static RandomVariateGenerator _generator;

        static RNGFactory()
        {
            Reset();
        }

        // Extract this from the static constructor to facilitate testing.
        protected internal static void Reset()
        {
            // Look at the config, if there is one, to determine which generator
            // to use and what seed value.

            // TODO: make MT be the default?
            string defaultPrngType = "RANDLIB";
            string configPrngType  = Configuration.CurrentConfiguration.GetParameterWithDefault("RNG.type", defaultPrngType);
            _generatorType         = (GeneratorType)Enum.Parse(typeof(GeneratorType), configPrngType.ToUpper());
            _generatorSeed         = (uint)Configuration.CurrentConfiguration.GetParameterWithDefault("prng_seed", 0);
            _seedIndex             = (uint)Configuration.CurrentConfiguration.GetParameterWithDefault("prng_index", 0);

            _generator = null;
            try
            {
                _generator = CreateNewPrng(_generatorType, _generatorSeed, _seedIndex);
            }
            catch (Exception)
            {
                Console.Error.WriteLine("Unknown/unsupported RNG type: {0}", configPrngType);
                throw;
            }

            Console.WriteLine("Using {0} PRNG ({1}, {2}).", configPrngType, _generatorSeed, _seedIndex);
        }

        public static RandomVariateGenerator CreateNewPrng(GeneratorType generatorType, uint generatorSeed, uint seedIndex)
        {
            RandomVariateGenerator prng;

            switch (generatorType)
            {
                case GeneratorType.VANILLA:
                    prng = DotNetVariateGenerator.CreateDotNetVariateGenerator(new[] {generatorSeed});
                    break;

                case GeneratorType.RANDLIB:
                    prng = RandLibVariateGenerator.CreateRandLibVariateGenerator(new [] { generatorSeed });
                    break;

                default:
                    throw new ArgumentException($"Unknown/unsupported RNG type: {generatorType}");
            }

            return prng;
        }

        // ReSharper disable once InconsistentNaming
        public static RandomVariateGenerator GetRNG() { return _generator; }
    }
}
