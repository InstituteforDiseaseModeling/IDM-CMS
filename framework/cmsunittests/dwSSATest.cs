/***************************************************************************************************

Copyright (c) 2018 Intellectual Ventures Property Holdings, LLC (IVPH) All rights reserved.

EMOD is licensed under the Creative Commons Attribution-Noncommercial-ShareAlike 4.0 License.
To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-sa/4.0/legalcode

***************************************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
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
// ReSharper disable InconsistentNaming

namespace cmsunittests
{
    [TestFixture]
    // ReSharper disable InconsistentNaming
    class dwSSATest : AssertionHelper
    // ReSharper restore InconsistentNaming
    {
        [Test]
        public void TestReactionSet1()
        {
            SpeciesDescription info1;
            Species reactant1;
            CreateSpeciesInfoAndSpecies("reactant1", 10, out info1, out reactant1);
            SpeciesDescription info2;
            Species reactant2;
            CreateSpeciesInfoAndSpecies("reactant2", 40, out info2, out reactant2);
            SpeciesDescription info3;
            Species product1;
            CreateSpeciesInfoAndSpecies("product1", 90, out info3, out product1);

            const double rate1 = 2.0;
            Reaction reaction1 = CreateReaction(info1, reactant1, info2, reactant2, info3, product1, rate1);
            Assert.AreEqual(reaction1.Reactants[0], reactant1);
            Assert.AreEqual(reaction1.Reactants[1], reactant2);
            Assert.AreEqual(reaction1.Products[0], product1);

            var reactions = new List<Reaction> { reaction1 };
            var reactionSet1 = new ReactionSet(reactions);
            Assert.AreEqual(reactionSet1.Reactions[0].Name, reactions[0].Name);

            double[] gamma = { 2.0 };
            Assert.AreEqual(4.0, reactionSet1.UpdateRates(gamma));
            Assert.AreEqual(4.0, reactionSet1.PredilectionRates[0]);
            Assert.AreEqual(2.0, reactionSet1.CurrentRates[0]);
            Assert.AreEqual(1, reactionSet1.NumReactions);

            reactionSet1.FireReaction(0);
            Assert.AreEqual(9, reactant1.Value);
            Assert.AreEqual(39, reactant2.Value);
            Assert.AreEqual(91, product1.Value);

            reactionSet1.FireReaction(0);
            Assert.AreEqual(8, reactant1.Value);
            Assert.AreEqual(38, reactant2.Value);
            Assert.AreEqual(92, product1.Value);
        }

        [Test]
        public void TestReactionSet2()
        {
            SpeciesDescription info1;
            Species reactant1;
            CreateSpeciesInfoAndSpecies("reactant1", 10, out info1, out reactant1);
            SpeciesDescription info2;
            Species reactant2;
            CreateSpeciesInfoAndSpecies("reactant2", 40, out info2, out reactant2);
            SpeciesDescription info3;
            Species product1;
            CreateSpeciesInfoAndSpecies("product1", 90, out info3, out product1);
            SpeciesDescription info4;
            Species reactant3;
            CreateSpeciesInfoAndSpecies("reactant3", 1, out info4, out reactant3);
            SpeciesDescription info5;
            Species reactant4;
            CreateSpeciesInfoAndSpecies("reactant4", 2, out info5, out reactant4);
            SpeciesDescription info6;
            Species product2;
            CreateSpeciesInfoAndSpecies("product2", 3, out info6, out product2);

            const double rate1 = 2.0;
            const double rate2 = 3.0;

            Reaction reaction1 = CreateReaction(info1, reactant1, info2, reactant2, info3, product1, rate1);
            Reaction reaction2 = CreateReaction(info4, reactant3, info5, reactant4, info6, product2, rate2);

            var reactions = new List<Reaction> { reaction1, reaction2 };
            var reactionSet = new ReactionSet(reactions);

            double[] gamma = { 1.5, 2 };
            Assert.AreEqual(9.0, reactionSet.UpdateRates(gamma));
            Assert.AreEqual(3.0, reactionSet.CurrentRates[1]);
            Assert.AreEqual(3.0, reactionSet.PredilectionRates[0]);
            Assert.AreEqual(2, reactionSet.NumReactions);

            reactionSet.FireReaction(0);
            Assert.AreEqual(9, reactant1.Value);
            Assert.AreEqual(39, reactant2.Value);
            Assert.AreEqual(91, product1.Value);

            reactionSet.FireReaction(1);
            Assert.AreEqual(0, reactant3.Value);
            Assert.AreEqual(1, reactant4.Value);
            Assert.AreEqual(4, product2.Value);

            int mu = reactionSet.SelectReaction(reactionSet.CurrentRates.Sum());
            Assert.AreEqual(1, mu);
            mu = reactionSet.SelectReaction(0.0);
            Assert.AreEqual(0, mu);
        }

        [Test]
        public void TestGammaInfo()
        {
            const int numReactions = 3;
            var gammaInfo = new GammaInfo(numReactions);

            int[] n = { 2, 4, 0 };
            double[] lambda = { 4.0, 10.0, 25.0 };
            const double weight = 1.5;

            gammaInfo.UpdateGamma(weight, n, lambda);
            gammaInfo.SetIntermediateGamma();
            gammaInfo.UpdateStructure();
            var intermediateGamma = gammaInfo.IntermediateGamma;
            var gammaNum = GetHiddenField<double[]>("_gammaNum", gammaInfo);

            Assert.AreEqual(intermediateGamma[0], 0.5);
            Assert.AreEqual(intermediateGamma[1], 0.4);
            Assert.AreEqual(intermediateGamma[2], double.Epsilon);
            Assert.AreEqual(gammaInfo.IntermediateRareEvent, -1);
            Assert.AreEqual(new double[] { 0, 0, 0 }, gammaNum);
        }

        [Test]
        public void TestdwSSAConstructorWithDefaults()
        {
            var solver = InstantiateSolver();

            Assert.AreEqual(0, GetHiddenField<int>("_trajectoryCounter", solver));
            Assert.AreEqual(0.0, GetHiddenField<double>("_runningMean", solver));
            Assert.AreEqual(0.0, GetHiddenField<double>("_runningVariance", solver));
            Assert.AreEqual(100000, GetHiddenField<int>("_crossEntropyRuns", solver));
            Assert.AreEqual(0.01, GetHiddenField<double>("_crossEntropyThreshold", solver));
            Assert.AreEqual(200, GetHiddenField<int>("_crossEntropyMinDataSize", solver));
            Assert.AreEqual("reExpression", GetHiddenField<string>("_reExpressionName", solver));
            Assert.AreEqual("reVal", GetHiddenField<string>("_reValName", solver));
            Assert.AreEqual(0.0, GetHiddenField<double>("_rareEventValue", solver));
            Assert.AreEqual("rever_isom_dwSSA_1e6.txt", GetHiddenField<string>("_outputFileName", solver));

            var expression = GetHiddenField<Expression>("_reExpression", solver);
            Assert.AreEqual("reExpression", expression.Name);
            var equalto = GetHiddenField<EqualTo>("_rareEventTest", solver);
            Assert.AreEqual(true, equalto.Value);
            var gamma = GetHiddenField<double[]>("_gamma", solver);
            Assert.That(gamma, Is.All.EqualTo(0.0));
            var reactions = GetHiddenField<ReactionSet>("_reactions", solver);
            Assert.AreEqual(gamma.Length, reactions.NumReactions);
        }

        [Test]
        public void TestdwSSAConstructor()
        {
            const string configString = @"{""solver"":""DWSSA"", ""dwSSA"": { ""crossEntropyThreshold"":0.005, ""crossEntropyRuns"":1000000, ""reExpressionName"":""reExpNameCustom"", ""reValName"":""reValNameCustom"", ""outputFileName"":""rever_isom_dwSSA_1e6_Custom.txt"", ""gamma"":[1.0, 1.0]}}";
            Configuration.CurrentConfiguration = Configuration.ConfigurationFromString(configString);

            var modelInfo = EmodlLoader.LoadEMODLFile("resources\\rever_isom_custom.emodl");
            const double duration = 20.0;
            const int repeats = 222;
            const int samples = 2;
            var solver = new dwSSA(modelInfo, duration, repeats, samples);

            Assert.AreEqual(0, GetHiddenField<int>("_trajectoryCounter", solver));
            Assert.AreEqual(0.0, GetHiddenField<double>("_runningMean", solver));
            Assert.AreEqual(0.0, GetHiddenField<double>("_runningVariance", solver));
            Assert.AreEqual(1000000, GetHiddenField<int>("_crossEntropyRuns", solver));
            Assert.AreEqual(0.005, GetHiddenField<double>("_crossEntropyThreshold", solver));
            Assert.AreEqual(200, GetHiddenField<int>("_crossEntropyMinDataSize", solver));
            Assert.AreEqual("reExpNameCustom", GetHiddenField<string>("_reExpressionName", solver));
            Assert.AreEqual("reValNameCustom", GetHiddenField<string>("_reValName", solver));
            Assert.AreEqual(0.0, GetHiddenField<double>("_rareEventValue", solver));
            Assert.AreEqual("rever_isom_dwSSA_1e6_Custom.txt", GetHiddenField<string>("_outputFileName", solver));

            var expression = GetHiddenField<Expression>("_reExpression", solver);
            Assert.AreEqual("reExpNameCustom", expression.Name);
            var equalto = GetHiddenField<EqualTo>("_rareEventTest", solver);
            Assert.AreEqual(true, equalto.Value);
            var gamma = GetHiddenField<double[]>("_gamma", solver);
            Assert.That(gamma, Is.All.EqualTo(1.0));
            var reactions = GetHiddenField<ReactionSet>("_reactions", solver);
            Assert.AreEqual(gamma.Length, reactions.NumReactions);
        }

        [Test]
        public void TestInitialize()
        {
            const string configString = @"{""solver"":""DWSSA"", ""dwSSA"":{""gamma"":[1.0, 1.0]}}";
            Configuration.CurrentConfiguration = Configuration.ConfigurationFromString(configString);

            ModelInfo modelInfo = EmodlLoader.LoadEMODLFile("resources\\rever_isom.emodl");
            const double duration = 10.0;
            const int repeats = 1000000;
            const int samples = 1;
            
            var solver = new dwSSA(modelInfo, duration, repeats, samples);
            solver.Initialize();

            var rareEventValue = GetHiddenField<double>("_rareEventValue", solver);
            var reExpression = GetHiddenField<Expression>("_reExpression", solver);

            Assert.AreEqual(30.0, rareEventValue);
            Assert.AreEqual(0.0, reExpression.Value);
        }

        [Test]
        public void TestCheckParameters1()
        {
            const string configString = @"{""solver"":""DWSSA"", ""dwSSA"":{""gamma"":[1.0, -1.0]}}";
            Configuration.CurrentConfiguration = Configuration.ConfigurationFromString(configString);

            ModelInfo modelInfo = EmodlLoader.LoadEMODLFile("resources\\rever_isom.emodl");
            const double duration = 10.0;
            const int repeats = 1000000;
            const int samples = 1;

            var solver = new dwSSA(modelInfo, duration, repeats, samples);
            var method = GetHiddenMethod("CheckParameters", solver);

            try
            {
                method.Invoke(solver, null);
                Assert.Fail("CheckParameters should have thrown an Error");
            }
            catch (TargetInvocationException ex)
            {
                Assert.That(ex.GetBaseException().ToString(), Is.StringContaining("Biasing parameter"));
            }
        }

        [Test]
        public void TestCheckParameters2()
        {
            const string configString = @"{""solver"":""DWSSA"",""dwSSA"":{""crossEntropyRuns"":100}}";
            Configuration.CurrentConfiguration = Configuration.ConfigurationFromString(configString);

            var modelInfo = EmodlLoader.LoadEMODLFile("resources\\rever_isom.emodl");
            const double duration = 10.0;
            const int repeats = 1000000;
            const int samples = 1;
            var solver = new dwSSA(modelInfo, duration, repeats, samples);
            var method = GetHiddenMethod("CheckParameters", solver);

            try
            {
                method.Invoke(solver, null);
                Assert.Fail("CheckParameters should have thrown an Error");
            }
            catch (TargetInvocationException ex)
            {
                Assert.That(ex.GetBaseException().ToString(), Is.StringContaining("crossEntropyRuns"));
            }
        }

        [Test]
        public void TestCheckParameters3()
        {
            const string configString = @"{""solver"":""DWSSA"",""dwSSA"":{""crossEntropyThreshold"":1.001}}";
            Configuration.CurrentConfiguration = Configuration.ConfigurationFromString(configString);

            var modelInfo = EmodlLoader.LoadEMODLFile("resources\\rever_isom.emodl");
            const double duration = 10.0;
            const int repeats = 1000000;
            const int samples = 1;

            var solver = new dwSSA(modelInfo, duration, repeats, samples);
            var method = GetHiddenMethod("CheckParameters", solver);

            try
            {
                method.Invoke(solver, null);
                Assert.Fail("CheckParameters should have thrown an Error");
            }
            catch (TargetInvocationException ex)
            {
                Assert.That(ex.GetBaseException().ToString(), Is.StringContaining("crossEntropyThreshold"));
            }
        }

        [Test]
        public void TestCheckParameters4()
        {
            const string configString = @"{""solver"":""DWSSA"",""dwSSA"":{""crossEntropyThreshold"":-0.001}}";
            Configuration.CurrentConfiguration = Configuration.ConfigurationFromString(configString);

            var modelInfo = EmodlLoader.LoadEMODLFile("resources\\rever_isom.emodl");
            const double duration = 10.0;
            const int repeats = 1000000;
            const int samples = 1;

            var solver = new dwSSA(modelInfo, duration, repeats, samples);
            var method = GetHiddenMethod("CheckParameters", solver);

            try
            {
                method.Invoke(solver, null);
                Assert.Fail("CheckParameters should have thrown an Error");
            }
            catch (TargetInvocationException ex)
            {
                Assert.That(ex.GetBaseException().ToString(), Is.StringContaining("crossEntropyThreshold"));
            }
        }

        [Test]
        public void TestCheckParameters5()
        {
            const string configString = @"{""solver"":""DWSSA"", ""dwSSA"":{""crossEntropyMinDataSize"":5}}";
            Configuration.CurrentConfiguration = Configuration.ConfigurationFromString(configString);

            var modelInfo = EmodlLoader.LoadEMODLFile("resources\\rever_isom.emodl");
            const double duration = 10.0;
            const int repeats = 1000000;
            const int samples = 1;
            
            var solver = new dwSSA(modelInfo, duration, repeats, samples);
            var method = GetHiddenMethod("CheckParameters", solver);

            try
            {
                method.Invoke(solver, null);
                Assert.Fail("CheckParameters should have thrown an Error");
            }
            catch (TargetInvocationException ex)
            {
                Assert.That(ex.GetBaseException().ToString(), Is.StringContaining("crossEntropyMinDataSize"));
            }
        }

        [Test]
        public void TestCheckParameters6()
        {
            const string configString = @"{""solver"":""DWSSA"",""dwSSA"":{""reExpressionName"":""reExpNameCustom""}}";
            Configuration.CurrentConfiguration = Configuration.ConfigurationFromString(configString);

            var modelInfo = EmodlLoader.LoadEMODLFile("resources\\rever_isom.emodl");
            const double duration = 10.0;
            const int repeats = 1000000;
            const int samples = 1;
            var solver = new dwSSA(modelInfo, duration, repeats, samples);
            var method = GetHiddenMethod("CheckParameters", solver);

            try
            {
                method.Invoke(solver, null);
                Assert.Fail("CheckParameters should have thrown an Error");
            }
            catch (TargetInvocationException ex)
            {
                Assert.That(ex.GetBaseException().ToString(), Is.StringContaining("rare event expression field"));
            }
        }


        [Test]
        public void TestCheckParameters7()
        {
            const string configString = @"{""solver"":""DWSSA"",""dwSSA"":{""reExpressionName"":""reExpNameCustom"", ""reValName"":""reValNameCustom""}}";
            Configuration.CurrentConfiguration = Configuration.ConfigurationFromString(configString);
            RunResetRNGFactory();

            var modelInfo = EmodlLoader.LoadEMODLFile("resources\\rever_isom_custom.emodl");
            const double duration = 10.0;
            const int repeats = 1000000;
            const int samples = 1;
            var solver = new dwSSA(modelInfo, duration, repeats, samples);
            var method = GetHiddenMethod("CheckParameters", solver);

            try
            {
                method.Invoke(solver, null);
                Assert.Fail("CheckParameters should have thrown an Error");
            }
            catch (TargetInvocationException ex)
            {
                Assert.That(ex.GetBaseException().ToString(), Is.StringContaining("rare event value name"));
            }
        }

        [Test]
        public void TestSetRareEventType()
        {
            const string configString = @"{""solver"":""DWSSA"",""dwSSA"":{""gamma"": [1, 1]}}";
            Configuration.CurrentConfiguration = Configuration.ConfigurationFromString(configString);

            var modelInfo = EmodlLoader.LoadEMODLFile("resources\\rever_isom.emodl");
            const double duration = 10.0;
            const int repeats = 100000;
            const int samples = 1;
            var solver = new dwSSA(modelInfo, duration, repeats, samples);
            solver.Initialize();

            var method = FindMethod("SetRareEventType", typeof(dwSSA));
            method.Invoke(solver, null);

            var reExpression = GetHiddenField<Expression>("_reExpression", solver);
            var reEventValue = GetHiddenField<double>("_rareEventValue", solver);

            Assert.AreEqual(0, reExpression.Value);
            Assert.AreEqual(30.0, reEventValue);
            Assert.AreEqual(1, GetHiddenField<int>("_rareEventType", solver));
        }

        [Test]
        public void TestSelectAndFireReaction()
        {
            const string configString = @"{""solver"":""DWSSA"", ""prng_seed"":123, ""prng_index"":1, ""RNG"":{""type"":""RANDLIB""}, ""dwSSA"":{""crossEntropyThreshold"":0.005, ""crossEntropyRuns"":100000, ""reExpressionName"":""reExpression"", ""reValName"":""reVal"", ""gamma"":[2.0, 0.5]}}";
            Configuration.CurrentConfiguration = Configuration.ConfigurationFromString(configString);
            RunResetRNGFactory();

            var modelInfo = EmodlLoader.LoadEMODLFile("resources\\rever_isom.emodl");
            const double duration = 10.0;
            const int repeats = 1;
            const int samples = 1;
            var solver = new dwSSA(modelInfo, duration, repeats, samples);
            solver.Initialize();

            var startRealization = GetHiddenMethod("StartRealization", solver);
            startRealization.Invoke(solver, null);

            var method = solver.GetType().GetMethod("SelectAndFireReaction", BindingFlags.NonPublic | BindingFlags.Instance, Type.DefaultBinder, new[] { typeof(double) }, null);
            var inputArray1 = new object[] { 1.5 };
            method.Invoke(solver, inputArray1);

            var reactions = GetHiddenField<ReactionSet>("_reactions", solver);
            Assert.AreEqual(reactions.Reactions[0].Reactants[0].Value, 99);
        }

        [Test]
        public void TestCalculateProposedTau()
        {
            var solver = InstantiateSolver();
            var method = GetHiddenMethod("CalculateProposedTau", solver);
            Assert.AreEqual("CalculateProposedTau", method.Name);

            try
            {
                var inputArray1 = new object[] { 1.0 };
                method.Invoke(solver, inputArray1);
                Assert.Fail("CalculateProposedTau should have thrown an Error");
            }
            catch (TargetInvocationException ex)
            {
                Assert.That(ex.GetBaseException().ToString(), Is.StringContaining("dwSSA doesn't use CalculateProposedTau()"));
            }
        }

        [Test]
        public void TestExecuteReactions()
        {
            var solver = InstantiateSolver();
            var method = GetHiddenMethod("ExecuteReactions", solver);
            Assert.AreEqual("ExecuteReactions", method.Name);

            try
            {
                method.Invoke(solver, null);
                Assert.Fail("ExecuteReactions should have thrown an Error");
            }
            catch (TargetInvocationException ex)
            {
                Assert.That(ex.GetBaseException().ToString(), Is.StringContaining("dwSSA doesn't use ExecuteReactions()"));
            }
        }

        [Test]
        public void TestStepOnce()
        {
            var solver = InstantiateSolver();
            try
            {
                var method = solver.GetType().GetMethod("StepOnce", BindingFlags.NonPublic | BindingFlags.Instance, Type.DefaultBinder, new Type[0], null);
                method.Invoke(solver, null);
                Assert.Fail("StepOnce should have thrown an Error");
            }
            catch (TargetInvocationException ex)
            {
                Assert.That(ex.GetBaseException().ToString(), Is.StringContaining("dwSSA doesn't use StepOnce()"));
            }
        }

        [Test]
        public void TestStepOnceWithWeight()
        {
            var solver = StartStepOnce();

            var stepOnce = solver.GetType().GetMethod("StepOnce", BindingFlags.NonPublic | BindingFlags.Instance, Type.DefaultBinder, new[] { typeof(double).MakeByRefType() }, null);
            var inputArray1 = new object[] { 1.5 };
            stepOnce.Invoke(solver, inputArray1);

            var currentTime = GetHiddenField<double>("_currentTime", solver);
            Assert.AreEqual(0.0088790502083847481d, currentTime);
        }

        private static dwSSA StartStepOnce()
        {
            const string configString =
                @"{""solver"":""DWSSA"",""prng_seed"":123, ""prng_index"":1,""RNG"":{""type"":""RANDLIB""},""dwSSA"":{""crossEntropyThreshold"":0.005, ""crossEntropyRuns"":100000, ""reExpressionName"":""reExpression"", ""reValName"":""reVal"", ""gamma"":[2.0, 0.5]}}";
            Configuration.CurrentConfiguration = Configuration.ConfigurationFromString(configString);
            RunResetRNGFactory();

            var modelInfo = EmodlLoader.LoadEMODLFile("resources\\rever_isom.emodl");
            const double duration = 10.0;
            const int repeats = 1;
            const int samples = 1;
            var solver = new dwSSA(modelInfo, duration, repeats, samples);
            solver.Initialize();

            var startRealization = GetHiddenMethod("StartRealization", solver);
            startRealization.Invoke(solver, null);
            return solver;
        }

        [Test]
        public void TestStepOnceWithParameters()
        {
            var solver = StartStepOnce();

            var stepOnce = solver.GetType().GetMethod("StepOnce", BindingFlags.NonPublic | BindingFlags.Instance, Type.DefaultBinder, new[] { typeof(double).MakeByRefType(), typeof(double[]), typeof(int[]).MakeByRefType(), typeof(double[]).MakeByRefType() }, null);
            const double weight = 1.5;
            var tempGamma = new[] { 2.0, 0.5 };
            var tempN = new[] { 10, 20 };
            var tempLambda = new[] { 20.0, 40.0 };
            var inputArray1 = new object[] { weight, tempGamma, tempN, tempLambda };
            stepOnce.Invoke(solver, inputArray1);

            var currentTime = GetHiddenField<double>("_currentTime", solver);
            Assert.AreEqual(0.0088790502083847481d, currentTime);
        }

        [Test]
        public void TestRunCrossEntropy()
        {
            var solver = SetupCrossEntropy();

            var method = solver.GetType().GetMethod("RunCrossEntropy", BindingFlags.NonPublic | BindingFlags.Instance, Type.DefaultBinder, new Type[0], null);
            method.Invoke(solver, null);

            var gammas = GetHiddenField<double[]>("_gamma", solver);
            // Assert.That(new[] { 1.3024005829071461d, 0.7871633385546174d }, Is.EquivalentTo(gammas));
            var save = GlobalSettings.DefaultFloatingPointTolerance;
            // C# Double.Epsilon is much too small - the smallest number distinguishable from 0 rather than the smallest detectable delta.
            // C++ DBL_EPSILON, 2.2204460492503131e-16, is still a bit too small.
            // Let's use C++ FLT_EPSILON,
            GlobalSettings.DefaultFloatingPointTolerance = 1.19209290E-07;
            Assert.AreEqual(1.355768664932917d, gammas[0]);
            Assert.AreEqual(0.70925271045434857d, gammas[1]);
            GlobalSettings.DefaultFloatingPointTolerance = save;
        }

        private static dwSSA SetupCrossEntropy()
        {
            const string configString =
                @"{""solver"":""DWSSA"", ""prng_seed"":123, ""prng_index"":1, ""RNG"":{""type"":""RANDLIB""}, ""dwSSA"":{""gamma"":[1, 1]}}";
            Configuration.CurrentConfiguration = Configuration.ConfigurationFromString(configString);
            RunResetRNGFactory();

            var modelInfo = EmodlLoader.LoadEMODLFile("resources\\rever_isom.emodl");
            const double duration = 10.0;
            const int repeats = 1;
            const int samples = 1;
            var solver = new dwSSA(modelInfo, duration, repeats, samples);
            solver.Initialize();

            var startRealization = GetHiddenMethod("StartRealization", solver);
            startRealization.Invoke(solver, null);
            return solver;
        }

        [Test]
        public void TestCrossEntropy1And2()
        {
            var solver = SetupCrossEntropy();

            var gammaInfo = new GammaInfo(2);
            var inputArray1 = new object[] { gammaInfo };

            var method1 = solver.GetType().GetMethod("CrossEntropy1", BindingFlags.NonPublic | BindingFlags.Instance, Type.DefaultBinder, new[] { typeof(GammaInfo) }, null);
            var gammaInfoOut1 = (GammaInfo)method1.Invoke(solver, inputArray1);
            Assert.AreEqual(23.0, gammaInfoOut1.IntermediateRareEvent);


            var inputArray2 = new object[] { gammaInfoOut1 };
            var method2 = solver.GetType().GetMethod("CrossEntropy2", BindingFlags.NonPublic | BindingFlags.Instance, Type.DefaultBinder, new[] { typeof(GammaInfo) }, null);
            var gammaInfoOut2 = (GammaInfo)method2.Invoke(solver, inputArray2);
            Assert.That(gammaInfoOut2.IntermediateGamma, Is.EquivalentTo(new[] { 1.1685264634846571d, 0.8430033009062794d }));
        }

        [Test]
        public void TestOutput()
        {
            var solver = InstantiateSolver();
            string outputPrefix = Configuration.CurrentConfiguration.GetParameterWithDefault("output.prefix", "trajectories");
            var method = solver.GetType().GetMethod("OutputData", BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, new[] { typeof(string) }, null);
            var inputArray = new object[] { outputPrefix };
            method.Invoke(solver, inputArray);
            
        }

        [Test]
        public void TestSolveOnce()
        {
            const string configString = @"{""solver"":""DWSSA"",""prng_seed"":123, ""prng_index"":1,""RNG"":{""type"":""RANDLIB""},""dwSSA"":{""reExpressionName"":""reExpression"", ""reValName"":""reVal"", ""gamma"":[1, 1]}}";
            Configuration.CurrentConfiguration = Configuration.ConfigurationFromString(configString);
            RunResetRNGFactory();

            var modelInfo = EmodlLoader.LoadEMODLFile("resources\\rever_isom.emodl");
            const double duration = 10.0;
            const int repeats = 1000000;
            const int samples = 1;
            var solver = new dwSSA(modelInfo, duration, repeats, samples);
            solver.Initialize();
            var method = solver.GetType().GetMethod("SolveOnce", BindingFlags.NonPublic | BindingFlags.Instance, Type.DefaultBinder, new Type[] { }, null);

            method.Invoke(solver, null);
            var trajectoryCounter = GetHiddenField<int>("_trajectoryCounter", solver);
            var runningMean = GetHiddenField<double>("_runningMean", solver);
            var currentTime = GetHiddenField<double>("_currentTime", solver);

            Assert.AreEqual(1, trajectoryCounter);
            Assert.AreEqual(0.0, runningMean);
            Assert.GreaterOrEqual(currentTime, duration);
        }

        [Test]
        public void TestSolve()
        {
            const string configString = @"{""solver"":""DWSSA"",""prng_seed"":123, ""prng_index"":1,""RNG"":{""type"":""RANDLIB""},""dwSSA"":{ ""reExpressionName"":""reExpression"", ""reValName"":""reVal"", ""gamma"":[1.3243964798992824, 0.73112035859010072 ]}}";
            Configuration.CurrentConfiguration = Configuration.ConfigurationFromString(configString);
            RunResetRNGFactory();

            var modelInfo = EmodlLoader.LoadEMODLFile("resources\\rever_isom.emodl");
            const double duration = 10.0;
            const int repeats = 100000;
            const int samples = 1;
            var solver = new dwSSA(modelInfo, duration, repeats, samples);
            solver.Solve();

            var trajectoryCounter = GetHiddenField<int>("_trajectoryCounter", solver);
            var runningMean = GetHiddenField<double>("_runningMean", solver);
            var runningVariance = GetHiddenField<double>("_runningVariance", solver);
            Assert.AreEqual(100000, trajectoryCounter);
            Assert.AreEqual(1.0419565264447506E-05d, runningMean);
            Assert.AreEqual(0.022171169128369424d, runningVariance);
        }

        [Test]
        public void TestToString()
        {
            var solver = InstantiateSolver();
            Assert.AreEqual("dwSSA", solver.ToString());
        }

        public static FieldInfo FindField(string fieldName, Type source)
        {
            FieldInfo fieldInfo = source.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance) ?? FindField(fieldName, source.BaseType);
            return fieldInfo;
        }

        public static T GetHiddenField<T>(string fieldName, object source)
        {
            FieldInfo fieldInfo = FindField(fieldName, source.GetType());
            return (T)fieldInfo.GetValue(source);
        }

        public static MethodInfo FindMethod(string methodName, Type source)
        {
            MethodInfo methodInfo = source.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance) ?? FindMethod(methodName, source.BaseType);
            return methodInfo;
        }

        public static MethodInfo GetHiddenMethod(string methodName, object source)
        {
            MethodInfo methodInfo = FindMethod(methodName, source.GetType());
            return methodInfo;
        }

        private static void CreateSpeciesInfoAndSpecies(string speciesName, int initialPopulation, out SpeciesDescription description, out Species species)
        {
            description = new SpeciesDescription(speciesName, initialPopulation);
            //species = new Species(description) { Count = description.InitialValue };
            species = new Species(description, new Dictionary<string, IValue>());
            species.Reset();
        }

        private static Reaction CreateReaction(SpeciesDescription info1, Species reactant1, SpeciesDescription info2, Species reactant2, SpeciesDescription info3, Species product1, double rate)
        {
            var builder = new ReactionInfo.ReactionBuilder("theReaction");
            builder.AddReactant(info1);
            builder.AddReactant(info2);
            builder.AddProduct(info3);
            builder.SetRate(new NumericExpressionTree(new Constant(rate)));

            var reactionInfo = builder.Reaction;
            var speciesMap = new Dictionary<SpeciesDescription, Species>(3) { { info1, reactant1 }, { info2, reactant2 }, { info3, product1 } };
            var symbolMap = new Dictionary<string, IValue>();
            var reaction = new Reaction(reactionInfo, speciesMap, symbolMap);

            return reaction;
        }

        private static void RunResetRNGFactory()
        {
            var methodInfo = typeof(RNGFactory).GetMethod("Reset", BindingFlags.NonPublic | BindingFlags.Static);
            methodInfo.Invoke(null, null);
        }

        private static dwSSA InstantiateSolver()
        {
            const string configString = @"{""solver"":""DWSSA"",""prng_seed"":123, ""prng_index"":1,""RNG"":{""type"":""RANDLIB""}}";
            Configuration.CurrentConfiguration = Configuration.ConfigurationFromString(configString);
            RunResetRNGFactory();

            var modelInfo = EmodlLoader.LoadEMODLFile("resources\\rever_isom.emodl");
            var solver = new dwSSA(modelInfo, 10.0, 1000000, 1);

            return solver;
        }
    }
}