/***************************************************************************************************

Copyright (c) 2018 Intellectual Ventures Property Holdings, LLC (IVPH) All rights reserved.

EMOD is licensed under the Creative Commons Attribution-Noncommercial-ShareAlike 4.0 License.
To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-sa/4.0/legalcode

***************************************************************************************************/

using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using compartments;
using compartments.emod.utils;
using distlib.randomvariates;
using compartments.emodl;

namespace cmsunittests
{
    [TestFixture, Description("Pseudo-random number generator tests")]
    class PrngTests : AssertionHelper
    {
        private MethodInfo _resetMethodinfo;

        [TestFixtureSetUp]
        public void Init()
        {
            _resetMethodinfo = typeof (RNGFactory).GetMethod("Reset", BindingFlags.NonPublic | BindingFlags.Static);
        }

        [Test, Description("RNGFactory test - VANILLA")]
        public void RngFactoryVanilla()
        {
            const string prngText = "vanilla";
            var prng = GetRngFromFactory(prngText);
            Assert.True(prng is DotNetVariateGenerator);
            Console.WriteLine("{0}() PASSED", MethodBase.GetCurrentMethod().Name);
        }

        [Test, Description("RNGFactory test - RANDLIB")]
        public void RngFactoryRandLib()
        {
            const string prngText = "RandLib";
            var prng = GetRngFromFactory(prngText);
            Assert.True(prng is RandLibVariateGenerator);
            Console.WriteLine("{0}() PASSED", MethodBase.GetCurrentMethod().Name);
        }

        private RandomVariateGenerator GetRngFromFactory(string prngText)
        {
            SetCurrentPrng(prngText);
            var prng = RNGFactory.GetRNG();
            return prng;
        }

        private void SetCurrentPrng(string prng)
        {
            const string configTemplate = "{\"RNG\":{\"type\":\"%PRNG%\"}}";
            string configText = configTemplate.Replace("%PRNG%", prng);
            Configuration.CurrentConfiguration = Configuration.ConfigurationFromString(configText);
            _resetMethodinfo.Invoke(null, null);
        }

        [Test, Description("SIR with Vanilla PRNG")]
        public void SirWithVanillaPrng()
        {
            const string prng = "VANILLA";
            var passed = ExecuteSirModel(prng);
            Assert.True(passed);
            Console.WriteLine("{0}() PASSED", MethodBase.GetCurrentMethod().Name);
        }

        [Test, Description("SIR with RandLib PRNG")]
        public void SirWithRandLibPrng()
        {
            const string prng = "RANDLIB";
            var passed = ExecuteSirModel(prng);
            Assert.True(passed);
            Console.WriteLine("{0}() PASSED", MethodBase.GetCurrentMethod().Name);
        }

        private bool ExecuteSirModel(string prng)
        {
            SetCurrentPrng(prng);
            var modelInfo = EmodlLoader.LoadEMODLFile("resources\\sir.emodl");
            const int repeats = 128;
            const int samples = 100;
            var solver = SolverFactory.CreateSolver("SSA", modelInfo, repeats, 548.0, samples);
            solver.Solve();
            var tempFilename = Path.GetTempFileName();
            var trajectoryData = solver.GetTrajectoryData();
            var means = new double[3][];
            var speciesMin = new[] { double.MaxValue, double.MaxValue, double.MaxValue};
            var speciesMax = new double[3];

            for (int species = 0; species < 3; species++)
            {
                means[species] = new double[samples];
                for (int trajectory = 0; trajectory < repeats; trajectory++)
                {
                    for (int sample = 0; sample < samples; sample++)
                    {
                        means[species][sample] += trajectoryData[species*repeats + trajectory][sample];
                    }
                }

                for (int sample = 0; sample < samples; sample++)
                {
                    var mean = means[species][sample]/repeats;
                    speciesMin[species] = Math.Min(speciesMin[species], mean);
                    speciesMax[species] = Math.Max(speciesMax[species], mean);
                    means[species][sample] = mean;
                }
            }

            const double minSmin = 19.0;
            const double maxSmin = 27.0;
            const double maxS    = 990.0;
            const double minI    = 10.0;
            const double minImax = 551.0;
            const double maxImax = 571.0;
            const double minR    = 0.0;
            const double minRmax = 859.0;
            const double maxRmax = 879.0;

            Console.WriteLine("Susceptible: min = {0}, max = {1} (limits- {2} <= min <= {3}, max == {4})",
                speciesMin[0], speciesMax[0], minSmin, maxSmin, maxS);
            Console.WriteLine("Infectious:  min = {0}, max = {1} (limits- min == {2}, {3} <= max <= {4})",
                speciesMin[1], speciesMax[1], minI, minImax, maxImax);
            Console.WriteLine("Recovered:   min = {0}, max = {1} (limits- min == {2}, {3} <= max <= {4})",
                speciesMin[2], speciesMax[2], minR, minRmax, maxRmax);

            // ReSharper disable CompareOfFloatsByEqualityOperator
            var susceptibleOkay = (minSmin <= speciesMin[0]) && (speciesMin[0] <= maxSmin) && (speciesMax[0] == maxS);
            var infectiousOkay  = (speciesMin[1] == minI) && (minImax <= speciesMax[1]) && (speciesMax[1] <= maxImax);
            var recoveredOkay   = (speciesMin[2] == minR) && (minRmax <= speciesMax[2]) && (speciesMax[2] <= maxRmax);
            // ReSharper restore CompareOfFloatsByEqualityOperator

            if (!(susceptibleOkay && infectiousOkay && recoveredOkay))
            {
                solver.OutputData(tempFilename);
                Console.WriteLine("Wrote SIR simulation data (with {0} PRNG) to {1}", prng, tempFilename);
            }
            else
            {
                File.Delete(tempFilename);
            }

            var passed = susceptibleOkay && infectiousOkay && recoveredOkay;
            return passed;
        }

        [Test]
        public void RngFactoryDefaultTest()
        {
            Configuration.CurrentConfiguration = Configuration.ConfigurationFromString("{}");
            _resetMethodinfo.Invoke(null, null);

            RandomVariateGenerator rng = RNGFactory.GetRNG();

            Console.WriteLine("Testing RNGFactory default value...");
            // TODO: when MT is implemented we probably want it to be the default?
            Expect(rng is RandLibVariateGenerator);

            Console.WriteLine("{0}() PASSED", MethodBase.GetCurrentMethod().Name);
        }

        [Test]
        [ExpectedException(typeof(TypeInitializationException))]
        public void RngFactoryExceptionTest()
        {
            Console.WriteLine("Testing RNGFactory exception...");

            const string configString = "{\"RNG\":{\"type\":\"busted\",\"seed\":1968}}";
            Configuration.CurrentConfiguration = Configuration.ConfigurationFromString(configString);
            MethodInfo reset = typeof(RNGFactory).GetMethod("Reset", BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Static);
            try
            {
                if (reset != null)
                {
                    reset.Invoke(null, null);
                }
                else
                {
                    throw new NullReferenceException("Couldn't get 'Reset' method from RNGFactory.");
                }
            }
            catch (Exception ex)
            {
                if (ex.InnerException is ArgumentException)
                {
                    Console.WriteLine("Caught TargetInvocationException with InnerException == ArgumentException");
                    throw new TypeInitializationException("RNGFactory.GeneratorType", null);
                }

                Console.WriteLine("Caught exception: {0}", ex.Message);
                if (ex.InnerException != null)
                {
                    throw ex.InnerException;
                }

                throw;
            }

            Console.WriteLine("{0}() FAILED", MethodBase.GetCurrentMethod().Name);
        }
    }
}
