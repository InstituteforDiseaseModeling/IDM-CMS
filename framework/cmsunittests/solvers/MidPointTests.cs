/***************************************************************************************************

Copyright (c) 2018 Intellectual Ventures Property Holdings, LLC (IVPH) All rights reserved.

EMOD is licensed under the Creative Commons Attribution-Noncommercial-ShareAlike 4.0 License.
To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-sa/4.0/legalcode

***************************************************************************************************/

using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using compartments;
using compartments.emod;
using compartments.emod.expressions;
using compartments.emod.interfaces;
using compartments.emod.utils;
using compartments.emodl;
using compartments.solvers;
using compartments.solvers.solverbase;
using distlib.samplers;

namespace cmsunittests.solvers
{
    [TestFixture, Description("MidPoint Solver Tests")]
    class MidPointTests : AssertionHelper
    {
        [Test]
        public void TestMidPointConstructorWithDefaults()
        {
            const string configString = @"{""solver"":""MID""}";
            Configuration.CurrentConfiguration = Configuration.ConfigurationFromString(configString);

            ModelInfo modelInfo = EmodlLoader.LoadEMODLFile("resources\\testmodel.emodl");
            const double duration = 6.28318531;
            const int repeats = 42;
            const int samples = 100;
            var solver = new MidPoint(modelInfo, duration, repeats, samples);

            Assert.AreEqual(0.01, ReflectionUtility.GetHiddenField<double>("epsilon", solver));
            Assert.AreEqual(2, ReflectionUtility.GetHiddenField<int>("nc", solver));
            Assert.AreEqual(10, ReflectionUtility.GetHiddenField<int>("multiple", solver));
            Assert.AreEqual(100, ReflectionUtility.GetHiddenField<int>("SSAruns", solver));
            string regimeName = ReflectionUtility.GetSolverRegimeName(solver);
            Assert.AreEqual("Leaping", regimeName);
        }

        [Test]
        public void TestMidPointConstructor()
        {
            const string configString = @"{""solver"":""MID"",""midpoint"":{""epsilon"":0.02,""Nc"":3,""Multiple"":8,""SSARuns"":50}}";
            
            Configuration.CurrentConfiguration = Configuration.ConfigurationFromString(configString);

            ModelInfo modelInfo  = EmodlLoader.LoadEMODLFile("resources\\testmodel.emodl");
            const double duration = 6.28318531;
            const int repeats    = 42;
            const int samples    = 100;
            var solver           = new MidPoint(modelInfo, duration, repeats, samples);

            Assert.AreEqual(0.02, ReflectionUtility.GetHiddenField<double>("epsilon", solver));
            Assert.AreEqual(3, ReflectionUtility.GetHiddenField<int>("nc", solver));
            Assert.AreEqual(8, ReflectionUtility.GetHiddenField<int>("multiple", solver));
            Assert.AreEqual(50, ReflectionUtility.GetHiddenField<int>("SSAruns", solver));
            string regimeName = ReflectionUtility.GetSolverRegimeName(solver);
            Assert.AreEqual("Leaping", regimeName);
        }

        [Test]
        public void CheckParametersTest()
        {
            var configString = @"{""solver"":""MID"",""midpoint"":{""epsilon"": -1}}";
            Configuration.CurrentConfiguration = Configuration.ConfigurationFromString(configString);

            var modelInfo = EmodlLoader.LoadEMODLFile("resources//testmodelTau.emodl");
            const double duration = 6.28318531;
            const int repeats = 42;
            const int samples = 100;

            try
            {
                var unused = new MidPoint(modelInfo, duration, repeats, samples);
                Assert.Fail();
            }
            catch (ApplicationException ex)
            {
                Assert.That(ex.GetBaseException().ToString(), Is.StringContaining("Epsilon was set to less than or equal to zero."));
            }

            configString = @"{""solver"":""MID"",""midpoint"":{""Nc"": -1}}";
            Configuration.CurrentConfiguration = Configuration.ConfigurationFromString(configString);

            try
            {
                var unused = new MidPoint(modelInfo, duration, repeats, samples);
                Assert.Fail();
            }
            catch (ApplicationException ex)
            {
                Assert.That(ex.GetBaseException().ToString(), Is.StringContaining("Nc was et to less than zero or equal to zero."));
            }

            configString = @"{""solver"":""MID"",""midpoint"":{""Multiple"": -1}}";
            Configuration.CurrentConfiguration = Configuration.ConfigurationFromString(configString);

            try
            {
                var unused = new MidPoint(modelInfo, duration, repeats, samples);
                Assert.Fail();
            }
            catch (ApplicationException ex)
            {
                Assert.That(ex.GetBaseException().ToString(), Is.StringContaining("Multiple was set to less than or equal to zero."));
            }

            configString = @"{""solver"":""MID"",""midpoint"":{""SSARuns"": 0}}";
            Configuration.CurrentConfiguration = Configuration.ConfigurationFromString(configString);

            try
            {
                var unused = new MidPoint(modelInfo, duration, repeats, samples);
                Assert.Fail();
            }
            catch (ApplicationException ex)
            {
                Assert.That(ex.GetBaseException().ToString(), Is.StringContaining("SSAruns was set to less than one."));
            }
        }

        [Test]
        public void TestFireReaction()
        {
            SpeciesDescription info1;
            SpeciesMP reactant1;
            CreateSpeciesInfoAndSpecies("reactant1", 10, out info1, out reactant1);
            SpeciesDescription info2;
            SpeciesMP reactant2;
            CreateSpeciesInfoAndSpecies("reactant2", 40, out info2, out reactant2);
            SpeciesDescription info3;
            SpeciesMP product1;
            CreateSpeciesInfoAndSpecies("product1", 90, out info3, out product1);
            double rate = 1.0;

            Reaction reaction = CreateReaction(info1, reactant1, info2, reactant2, info3, product1,rate);

            MidPoint solver = InstantiateSolver();

            // Partial, exploratory leap
            solver.FireReaction(reaction, 0.25);
            Assert.AreEqual(9.75, reactant1.Value);
            Assert.AreEqual(39.75, reactant2.Value);
            Assert.AreEqual(90.25, product1.Value);

            // Finish leap
            solver.FireReaction(reaction, 0.75);
            Assert.AreEqual(9, reactant1.Count);
            Assert.AreEqual(39, reactant2.Count);
            Assert.AreEqual(91, product1.Count);

            // Partial, exploratory leap
            solver.FireReaction(reaction, 4.5);
            Assert.AreEqual(4.5, reactant1.Value);
            Assert.AreEqual(34.5, reactant2.Value);
            Assert.AreEqual(95.5, product1.Value);

            // Undo part of the leap
            solver.FireReaction(reaction, -1.5);
            Assert.AreEqual(6, reactant1.Count);
            Assert.AreEqual(36, reactant2.Count);
            Assert.AreEqual(94, product1.Count);

            // Test using a fractional number that can not be exactly represented by a binary/floating point

            var change = (1.0 / 3.0);

            solver.FireReaction(reaction, change);
            Assert.AreEqual(5.666666666666667, reactant1.Value);
            Assert.AreEqual(35.666666666666664, reactant2.Value);
            Assert.AreEqual(94.333333333333329, product1.Value);

        }

        [Test]
        public void TestToString()
        {
            const string configString = @"{""solver"":""MID""}";
            Configuration.CurrentConfiguration = Configuration.ConfigurationFromString(configString);

            ModelInfo modelInfo = EmodlLoader.LoadEMODLFile("resources\\testmodel.emodl");
            const double duration = 6.28318531;
            const int repeats = 42;
            const int samples = 100;
            var solver = new MidPoint(modelInfo, duration, repeats, samples);

            Assert.AreEqual("Mid-Point", solver.ToString());
        }

        [Test]
        public void TestFireNonCriticalReactions()
        {
            const string configString = @"{""solver"":""MID"",""prng_seed"":123, ""prng_index"":1,""RNG"":{""type"":""RANDLIB""}}";
            Configuration.CurrentConfiguration = Configuration.ConfigurationFromString(configString);

            ModelInfo modelInfo = EmodlLoader.LoadEMODLFile("resources\\testmodelMidPoint.emodl");
            const double duration = 6.28318531;
            const int repeats = 42;
            const int samples = 100;
            var solver = new MidPoint(modelInfo, duration, repeats, samples);

            //Inputs for the method
            // Tau Step
            double tau1 = 0.0;
            var tau2 = (1.0 / 200.0);
            
            //Need a list of reactions
            var currentRates = ReflectionUtility.GetHiddenField<double[]>("_currentRates", solver);
            var nonCriticalReaction = new List<Reaction>();

            //Find Hidden Method to in order to initialize the population of each of the species
            MethodInfo resetModelStateMethod = ReflectionUtility.GetHiddenMethod("ResetModelState", solver);
            resetModelStateMethod.Invoke(solver, null);

            MethodInfo updateAndSumRatesMethod = ReflectionUtility.GetHiddenMethod("UpdateAndSumRates", solver);

            var model = ReflectionUtility.GetHiddenField<Model>("model", solver);
            
            nonCriticalReaction.Add(model.Reactions[0]);

            object[] inputArray1 = new object[3];
            inputArray1[0] = tau1; inputArray1[1] = nonCriticalReaction; inputArray1[2] = currentRates;

            object[] inputArray2 = new object[3];
            inputArray2[0] = tau2; inputArray2[1] = nonCriticalReaction; inputArray2[2] = currentRates;

            MethodInfo fireNonCriticalReactionsMethod = ReflectionUtility.GetHiddenMethod("FireNonCriticalReactions", solver);

            // First Test if Tau = 0;
            fireNonCriticalReactionsMethod.Invoke(solver, inputArray1);

            Assert.AreEqual(400, model.Species[0].Value);
            Assert.AreEqual(200, model.Species[1].Value);
            Assert.AreEqual(900, model.Species[2].Value);

            //Second Test if Tau = 0.05 (Not Zero).       

            ReflectionUtility.RunResetRngFactory();
            ReflectionUtility.SetHiddenField("_distributionSampler", solver, RandLibSampler.CreateRandLibSampler(RNGFactory.GetRNG()));
            fireNonCriticalReactionsMethod.Invoke(solver, inputArray2);

            Assert.AreEqual(378.0, model.Species[0].Value);
            Assert.AreEqual(178.0, model.Species[1].Value);
            Assert.AreEqual(922.0, model.Species[2].Value);
           
            resetModelStateMethod.Invoke(solver, null);
            object[] inputArray3 = new object[2];
            inputArray3[0] = model.Reactions; inputArray3[1] = currentRates;
            updateAndSumRatesMethod.Invoke(solver, inputArray3);
            ReflectionUtility.RunResetRngFactory();
            ReflectionUtility.SetHiddenField("_distributionSampler", solver, RandLibSampler.CreateRandLibSampler(RNGFactory.GetRNG()));
            fireNonCriticalReactionsMethod.Invoke(solver, inputArray2);
            
            Assert.AreEqual(378.0, model.Species[0].Value);
            Assert.AreEqual(178.0, model.Species[1].Value);
            Assert.AreEqual(922.0, model.Species[2].Value);
        }

        private static void CreateSpeciesInfoAndSpecies(string speciesName, int initialPopulation, out SpeciesDescription info, out SpeciesMP species)
        {
            info = new SpeciesDescription(speciesName, initialPopulation);
            species = new SpeciesMP(info, new Dictionary<string, IValue>());
            species.Reset();
        }

        private static Reaction CreateReaction(SpeciesDescription info1, SpeciesMP reactant1,
                                               SpeciesDescription info2, SpeciesMP reactant2,
                                               SpeciesDescription info3, SpeciesMP product1, double rate)
        {
            var builder = new ReactionInfo.ReactionBuilder("theReaction");
            builder.AddReactant(info1);
            builder.AddReactant(info2);
            builder.AddProduct(info3);
            builder.SetRate(new NumericExpressionTree(new Constant(rate)));
            var reactionInfo = builder.Reaction;
            var speciesMap = new Dictionary<SpeciesDescription, Species>(3)
                                 {{info1, reactant1}, {info2, reactant2}, {info3, product1}};
            var symbolMap = new Dictionary<string, IValue>();
            var reaction = new Reaction(reactionInfo, speciesMap, symbolMap);
            return reaction;
        }

        private static MidPoint InstantiateSolver()
        {
            const string configString = @"{""solver"":""MID""}";
            Configuration.CurrentConfiguration = Configuration.ConfigurationFromString(configString);

            ModelInfo modelInfo = EmodlLoader.LoadEMODLFile("resources//testmodel.emodl");
            var solver = new MidPoint(modelInfo, 6.28318531, 42, 100);
            return solver;
        }
    }
}