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
using compartments.emod.utils;
using compartments.emodl;
using compartments.solvers;
using compartments.solvers.solverbase;
using distlib.samplers;

namespace cmsunittests.solvers
{
    [TestFixture, Description("Tau Leaping Solver Tests")]
    class TauLeapingTests : AssertionHelper
    {
        [Test]
        public void TauLeapingConstructorDefaultTest()
        {            
            const string configString = @"{""solver"":""Tau"",""prng_seed"":123, ""prng_index"":1,""RNG"":{""type"":""RANDLIB""}}";

            Configuration.CurrentConfiguration = Configuration.ConfigurationFromString(configString);

            ModelInfo modelInfo = EmodlLoader.LoadEMODLFile("resources//testmodel.emodl");
            const double duration = 6.28318531;
            const int repeats = 42;
            const int samples = 100;
            var solver = new TauLeaping(modelInfo, duration, repeats, samples);

            Assert.AreEqual(0.001, ReflectionUtility.GetHiddenField<double>("epsilon", solver));
            Assert.AreEqual(2, ReflectionUtility.GetHiddenField<int>("nc", solver));
            Assert.AreEqual(10, ReflectionUtility.GetHiddenField<int>("multiple", solver));
            Assert.AreEqual(100, ReflectionUtility.GetHiddenField<int>("SSAruns", solver));
            string regimeName = ReflectionUtility.GetSolverRegimeName(solver);
            Assert.AreEqual("Leaping", regimeName);

            double[] currentRates = ReflectionUtility.GetHiddenField<double[]>("_currentRates", solver);

            Assert.AreEqual(15, currentRates.Length);
        }

        [Test]
        public void CalculateProposedTauTest1()
        {
            Model model;
            MethodInfo calculateProposedTauMethod;
            MethodInfo resetModelStateMethod;
            var solver = DefaultSetupForCalculateProposedTauTests(out model, out calculateProposedTauMethod, out resetModelStateMethod);

            ReflectionUtility.SetHiddenField("_regime", solver, 0);
            double tauIn = 5;
            object[] inputArray1 = new object[1];
            inputArray1[0] = tauIn;

            var tauOut1 = calculateProposedTauMethod.Invoke(solver, inputArray1);
            Assert.AreEqual(0.017758100416769496, tauOut1);

            ReflectionUtility.RunResetRngFactory();
            ReflectionUtility.SetHiddenField("_distributionSampler", solver, RandLibSampler.CreateRandLibSampler(RNGFactory.GetRNG()));

            inputArray1[0] = 0.0001;
            var tauOut1B = calculateProposedTauMethod.Invoke(solver, inputArray1);
            Assert.AreEqual(inputArray1[0], tauOut1B);
        }

        [Test]
        public void CalculateProposedTauTest2()
        {
            Model model;
            MethodInfo calculateProposedTauMethod;
            MethodInfo resetModelStateMethod;
            var solver = DefaultSetupForCalculateProposedTauTests(out model, out calculateProposedTauMethod, out resetModelStateMethod);

            ReflectionUtility.SetHiddenField("_regime", solver, 0);
            model.Parameters[2].Value = 0;
            model.Parameters[3].Value = 0;
            object[] inputArray1 = new object[1];
            double tauIn = 5;
            inputArray1[0] = tauIn;
            inputArray1[0] = tauIn;

            var tauOut2 = calculateProposedTauMethod.Invoke(solver, inputArray1);
            Assert.AreEqual(5, tauOut2);
        }

        private static TauLeaping DefaultSetupForCalculateProposedTauTests(out Model model,
                                                                           out MethodInfo calculateProposedTauMethod, out MethodInfo resetModelStateMethod)
        {
            ////// /Setup the test /////////                      
            const string configString = @"{""solver"":""Tau"",""prng_seed"":123, ""prng_index"":1,""RNG"":{""type"":""RANDLIB""}}";
            Configuration.CurrentConfiguration = Configuration.ConfigurationFromString(configString);

            ModelInfo modelInfo = EmodlLoader.LoadEMODLFile("resources//testmodelTau.emodl");
            const double duration = 6.28318531;
            const int repeats = 42;
            const int samples = 100;
            ReflectionUtility.RunResetRngFactory();
            var solver = new TauLeaping(modelInfo, duration, repeats, samples);

            //Find Hidden Method to in order to initialize the population of each of the species
            resetModelStateMethod = ReflectionUtility.GetHiddenMethod("ResetModelState", solver);
            resetModelStateMethod.Invoke(solver, null);

            //Find the model object
            model = ReflectionUtility.GetHiddenField<Model>("model", solver);

            //Find the method
            calculateProposedTauMethod = ReflectionUtility.GetHiddenMethod("CalculateProposedTau", solver);
            return solver;
        }

        [Test]
        public void CalculateProposedTauTest3()
        {
            Model model;
            MethodInfo calculateProposedTauMethod;
            MethodInfo resetModelStateMethod;
            var solver = DefaultSetupForCalculateProposedTauTests(out model, out calculateProposedTauMethod, out resetModelStateMethod);

            ReflectionUtility.SetHiddenField("_regime", solver, 3);
            model.Parameters[2].Value = 0;
            model.Parameters[3].Value = 0;
            object[] inputArray1 = new object[1];
            inputArray1[0] = 6;

            var tauOut3 = calculateProposedTauMethod.Invoke(solver, inputArray1);
            Assert.AreEqual(6, tauOut3);
        }

        [Test]
        public void CalculateProposedTauTest4()
        {
            Model model;
            MethodInfo calculateProposedTauMethod;
            MethodInfo resetModelStateMethod;
            var solver = DefaultSetupForCalculateProposedTauTests(out model, out calculateProposedTauMethod, out resetModelStateMethod);

            ReflectionUtility.SetHiddenField("_regime", solver, 0);
            object[] inputArray1 = new object[1];

            ReflectionUtility.RunResetRngFactory();
            ReflectionUtility.SetHiddenField("_distributionSampler", solver, RandLibSampler.CreateRandLibSampler(RNGFactory.GetRNG()));
            //resetModelStateMethod.Invoke(solver, null);
            ReflectionUtility.SetHiddenField("_regime", solver, 3);
            model.Species[0].Count = 3;
            model.Species[1].Count = 2;
            inputArray1[0] = 10;
            ReflectionUtility.SetHiddenField("nc", solver, 3);

            var tauOut4 = calculateProposedTauMethod.Invoke(solver, inputArray1);
            Assert.AreEqual(0.026637150625154243, tauOut4);

            ReflectionUtility.RunResetRngFactory();
            ReflectionUtility.SetHiddenField("_distributionSampler", solver, RandLibSampler.CreateRandLibSampler(RNGFactory.GetRNG()));
            ReflectionUtility.SetHiddenField("_regime", solver, 3);
            inputArray1[0] = 0.001;
            var tauOut4B = calculateProposedTauMethod.Invoke(solver, inputArray1);
            Assert.AreEqual(0.001, tauOut4B);

            ReflectionUtility.RunResetRngFactory();
            ReflectionUtility.SetHiddenField("_distributionSampler", solver, RandLibSampler.CreateRandLibSampler(RNGFactory.GetRNG()));
            ReflectionUtility.SetHiddenField("_regime", solver, 3);
            inputArray1[0] = 6.29010763;
            var tauOut4C = calculateProposedTauMethod.Invoke(solver, inputArray1);
            Assert.AreEqual(0.13335417718069428, tauOut4C);
        }

        [Test]
        public void CalculateProposedTauTest5()
        {
            Model model;
            MethodInfo calculateProposedTauMethod;
            MethodInfo resetModelStateMethod;
            var solver = DefaultSetupForCalculateProposedTauTests(out model, out calculateProposedTauMethod, out resetModelStateMethod);

            object[] inputArray1 = new object[1];
            ReflectionUtility.RunResetRngFactory();
            ReflectionUtility.SetHiddenField("_distributionSampler", solver, RandLibSampler.CreateRandLibSampler(RNGFactory.GetRNG()));
            //resetModelStateMethod.Invoke(solver, null);
            ReflectionUtility.SetHiddenField("_regime", solver, 3);
            model.Species[0].Count = 200;
            model.Species[1].Count = 10;
            ReflectionUtility.SetHiddenField("epsilon", solver, 0.1);
            inputArray1[0] = 10;

            var tauOut5 = calculateProposedTauMethod.Invoke(solver, inputArray1);
            Assert.AreEqual(0.052564102564102565, tauOut5);

            ReflectionUtility.RunResetRngFactory();
            ReflectionUtility.SetHiddenField("_distributionSampler", solver, RandLibSampler.CreateRandLibSampler(RNGFactory.GetRNG()));
            ReflectionUtility.SetHiddenField("_regime", solver, 3);
            inputArray1[0] = 0.01;
            var tauOut5B = calculateProposedTauMethod.Invoke(solver, inputArray1);
            Assert.AreEqual(0.01, tauOut5B);
        }

        [Test]
        public void CalculateProposedTauTest6()
        {
            Model model;
            MethodInfo calculateProposedTauMethod;
            MethodInfo resetModelStateMethod;
            var solver = DefaultSetupForCalculateProposedTauTests(out model, out calculateProposedTauMethod, out resetModelStateMethod);

            object[] inputArray1 = new object[1];

            ReflectionUtility.RunResetRngFactory();
            ReflectionUtility.SetHiddenField("_distributionSampler", solver, RandLibSampler.CreateRandLibSampler(RNGFactory.GetRNG()));
            resetModelStateMethod.Invoke(solver, null);
            ReflectionUtility.SetHiddenField("_regime", solver, 3);
            model.Species[0].Count = 200;
            model.Species[1].Count = 2;
            ReflectionUtility.SetHiddenField("epsilon", solver, 0.1);
            inputArray1[0] = 10;

            var tauOut6 = calculateProposedTauMethod.Invoke(solver, inputArray1);
            Assert.AreEqual(0.050502512562814073, tauOut6);

            ReflectionUtility.RunResetRngFactory();
            ReflectionUtility.SetHiddenField("_distributionSampler", solver, RandLibSampler.CreateRandLibSampler(RNGFactory.GetRNG()));
            resetModelStateMethod.Invoke(solver, null);
            ReflectionUtility.SetHiddenField("_regime", solver, 3);
            model.Species[0].Count = 200;
            model.Species[1].Count = 2;
            ReflectionUtility.SetHiddenField("epsilon", solver, 0.1);
            inputArray1[0] = 10;

            var tauOut6Test = calculateProposedTauMethod.Invoke(solver, inputArray1);
            Assert.AreEqual(0.050502512562814073, tauOut6Test);

            ReflectionUtility.RunResetRngFactory();
            ReflectionUtility.SetHiddenField("_distributionSampler", solver, RandLibSampler.CreateRandLibSampler(RNGFactory.GetRNG()));
            ReflectionUtility.SetHiddenField("_regime", solver, 3);
            inputArray1[0] = 0.01;
            var tauOut6B = calculateProposedTauMethod.Invoke(solver, inputArray1);
            Assert.AreEqual(0.01, tauOut6B);
        }

        [Test]
        public void CalculateProposedTauTest7()
        {
            Model model;
            MethodInfo calculateProposedTauMethod;
            MethodInfo resetModelStateMethod;
            var solver = DefaultSetupForCalculateProposedTauTests(out model, out calculateProposedTauMethod, out resetModelStateMethod);

            object[] inputArray1 = new object[1];

            ReflectionUtility.RunResetRngFactory();
            ReflectionUtility.SetHiddenField("_distributionSampler", solver, RandLibSampler.CreateRandLibSampler(RNGFactory.GetRNG()));
            resetModelStateMethod.Invoke(solver, null);
            ReflectionUtility.SetHiddenField("_regime", solver, 3);
            model.Species[0].Count = 3;
            model.Species[1].Count = 3;
            inputArray1[0] = 10;
            ReflectionUtility.SetHiddenField("nc", solver, 10);

            var tauOut7 = calculateProposedTauMethod.Invoke(solver, inputArray1);
            Assert.AreEqual(0.023677467222359326, tauOut7);

            ReflectionUtility.RunResetRngFactory();
            ReflectionUtility.SetHiddenField("_distributionSampler", solver, RandLibSampler.CreateRandLibSampler(RNGFactory.GetRNG()));
            ReflectionUtility.SetHiddenField("_regime", solver, 3);
            inputArray1[0] = 0.001;

            var tauOut7B = calculateProposedTauMethod.Invoke(solver, inputArray1);
            Assert.AreEqual(0.001, tauOut7B);
        }

        [Test]
        public void ExecuteReactionsTest()
        {
            //0.  Setup
            const string configString = @"{""solver"":""Tau"",""prng_seed"":123, ""prng_index"":1,""RNG"":{""type"":""RANDLIB""}}";
            Configuration.CurrentConfiguration = Configuration.ConfigurationFromString(configString);

            ModelInfo modelInfo = EmodlLoader.LoadEMODLFile("resources//testmodelTau.emodl");
            const double duration = 6.28318531;
            const int repeats = 42;
            const int samples = 100;
            var solver = new TauLeaping(modelInfo, duration, repeats, samples);

            //Find Hidden Method to in order to initialize the population of each of the species
            MethodInfo resetModelStateMethod = ReflectionUtility.GetHiddenMethod("ResetModelState", solver);
            resetModelStateMethod.Invoke(solver, null);

            //Find the model object
            var model = ReflectionUtility.GetHiddenField<Model>("model", solver);

            //Find the method
            MethodInfo executeReactionsMethod = ReflectionUtility.GetHiddenMethod("ExecuteReactions", solver);

            //1.  ==Regime.SSA
            ReflectionUtility.RunResetRngFactory();
            ReflectionUtility.SetHiddenField("_distributionSampler", solver, RandLibSampler.CreateRandLibSampler(RNGFactory.GetRNG()));
            ReflectionUtility.SetHiddenField("_regime", solver, 0);
            ReflectionUtility.GetHiddenField<Reaction>("_ssaReaction", solver);
            //a. _ssaReaction == null;

            executeReactionsMethod.Invoke(solver,null);

            var regime = ReflectionUtility.GetHiddenField<int>("_regime", solver);
            Assert.AreEqual(0,regime);

                    //b. _ssaReaction != null;
            ReflectionUtility.SetHiddenField("_remainingSSAsteps", solver, 1);
            ReflectionUtility.SetHiddenField("_ssaReaction", solver, model.Reactions[0]);
            ReflectionUtility.RunResetRngFactory();
            ReflectionUtility.SetHiddenField("_distributionSampler", solver, RandLibSampler.CreateRandLibSampler(RNGFactory.GetRNG()));

            executeReactionsMethod.Invoke(solver, null);
            regime = ReflectionUtility.GetHiddenField<int>("_regime", solver);
            Assert.AreEqual(3, regime);
            Assert.AreEqual(6.0, model.Species[0].Count);
            Assert.AreEqual(1.0, model.Species[1].Count);

            //2.  == Regime.NonCritical

            ReflectionUtility.SetHiddenField("_regime", solver, 1);

            ReflectionUtility.RunResetRngFactory();
            resetModelStateMethod.Invoke(solver, null);
            model.Species[0].Count = 100;
            model.Species[1].Count = 50;
            var nonCriticalReactionsList = new List<Reaction>();
            nonCriticalReactionsList.Add(model.Reactions[0]);
            nonCriticalReactionsList.Add(model.Reactions[1]);
            var nonCriticalRates = new double[2];
            nonCriticalRates[0] = model.Reactions[0].Rate;
            nonCriticalRates[1] = model.Reactions[1].Rate;

            ReflectionUtility.SetHiddenField("_distributionSampler", solver, RandLibSampler.CreateRandLibSampler(RNGFactory.GetRNG()));
            ReflectionUtility.SetHiddenField("_leapTau", solver, 0.01);
            ReflectionUtility.SetHiddenField("_nonCriticalReactions", solver, nonCriticalReactionsList);
            ReflectionUtility.SetHiddenField("_noncriticalRates", solver, nonCriticalRates);

            executeReactionsMethod.Invoke(solver, null);

            regime = ReflectionUtility.GetHiddenField<int>("_regime", solver);
            Assert.AreEqual(3, regime);
            Assert.AreEqual(103, model.Species[0].Count);
            Assert.AreEqual(47, model.Species[1].Count);

            //3.  == Regime.Critical ) no noncriticalreactions
            ReflectionUtility.SetHiddenField("_regime", solver, 2);

            ReflectionUtility.RunResetRngFactory();
            resetModelStateMethod.Invoke(solver, null);
            model.Species[0].Count = 100;
            model.Species[1].Count = 50;
            var criticalReactionsList = new List<Reaction>();
            criticalReactionsList.Add(model.Reactions[0]); 
            criticalReactionsList.Add(model.Reactions[1]);
            var criticalRates = new double[2];
            criticalRates[0] = model.Reactions[0].Rate;
            criticalRates[1] = model.Reactions[1].Rate;

            MethodInfo updateAndSumRatesMethod = ReflectionUtility.GetHiddenMethod("UpdateAndSumRates", solver);
            object[] inputArray1 = new object[2];
            inputArray1[0] = criticalReactionsList; inputArray1[1] = criticalRates; 

            var a0Critical = updateAndSumRatesMethod.Invoke(solver, inputArray1);

            ReflectionUtility.SetHiddenField("_distributionSampler", solver, RandLibSampler.CreateRandLibSampler(RNGFactory.GetRNG()));
            ReflectionUtility.SetHiddenField("_a0Critical", solver, a0Critical);
            ReflectionUtility.SetHiddenField("_criticalReactions", solver, criticalReactionsList);
            ReflectionUtility.SetHiddenField("_criticalRates", solver, criticalRates);
            ReflectionUtility.SetHiddenField("_nonCriticalReactions", solver, null);
            ReflectionUtility.SetHiddenField("_noncriticalRates", solver, null);

            executeReactionsMethod.Invoke(solver, null);

            regime = ReflectionUtility.GetHiddenField<int>("_regime", solver);
            Assert.AreEqual(3, regime);
            Assert.AreEqual(101, model.Species[0].Count);
            Assert.AreEqual(49, model.Species[1].Count);

            //3.  == Regime.Critical b) noncriticalreactions and critical reactions
            ReflectionUtility.SetHiddenField("_regime", solver, 2);

            ReflectionUtility.RunResetRngFactory();
            resetModelStateMethod.Invoke(solver, null);
            model.Species[0].Count = 100;
            model.Species[1].Count = 50;
            criticalReactionsList = new List<Reaction>();
            nonCriticalReactionsList = new List<Reaction>();
            criticalReactionsList.Add(model.Reactions[0]);
            nonCriticalReactionsList.Add(model.Reactions[1]);
            criticalRates = new double[1];
            nonCriticalRates = new double[1];
            criticalRates[0] = model.Reactions[0].Rate;
            nonCriticalRates[0] = model.Reactions[1].Rate;
                    
            inputArray1[0] = criticalReactionsList; inputArray1[1] = criticalRates;

            a0Critical = updateAndSumRatesMethod.Invoke(solver, inputArray1);

            ReflectionUtility.SetHiddenField("_distributionSampler", solver, RandLibSampler.CreateRandLibSampler(RNGFactory.GetRNG()));
            ReflectionUtility.SetHiddenField("_a0Critical", solver, a0Critical);
            ReflectionUtility.SetHiddenField("_leapTau", solver, 0.01);
            ReflectionUtility.SetHiddenField("_criticalReactions", solver, criticalReactionsList);
            ReflectionUtility.SetHiddenField("_criticalRates", solver, criticalRates);
            ReflectionUtility.SetHiddenField("_nonCriticalReactions", solver, nonCriticalReactionsList);
            ReflectionUtility.SetHiddenField("_noncriticalRates", solver, nonCriticalRates);

            executeReactionsMethod.Invoke(solver, null);

            regime = ReflectionUtility.GetHiddenField<int>("_regime", solver);
            Assert.AreEqual(3, regime);
            Assert.AreEqual(100, model.Species[0].Count);
            Assert.AreEqual(50, model.Species[1].Count);

            //4.  == Regime.AnythingElse
            ReflectionUtility.SetHiddenField("_regime", solver, 4);
            try
            {
                executeReactionsMethod.Invoke(solver, null);
                Assert.Fail();
            }
            catch (ApplicationException ex)
            {
                Assert.That(ex.GetBaseException().ToString(), Is.StringContaining("Bad solver mode."));
            }
        }

        [Test]
        public void ComputeJacobianTest()
        {
            //0.  Setup
            const string configString = @"{""solver"":""TAU""}";
            Configuration.CurrentConfiguration = Configuration.ConfigurationFromString(configString);

            ModelInfo modelInfo = EmodlLoader.LoadEMODLFile("resources//testmodelTau.emodl");
            const double duration = 6.28318531;
            const int repeats = 42;
            const int samples = 100;
            var solver = new TauLeaping(modelInfo, duration, repeats, samples);

            //Find Hidden Method to in order to initialize the population of each of the species
            MethodInfo resetModelStateMethod = ReflectionUtility.GetHiddenMethod("ResetModelState", solver);
            resetModelStateMethod.Invoke(solver, null);

            //Find the model object
            var model = ReflectionUtility.GetHiddenField<Model>("model", solver);

            //Find methods
            MethodInfo computeJacobianMethod = ReflectionUtility.GetHiddenMethod("ComputeJacobian", solver);

            //Set currentRates
            double[] currentRates = new double[2];
            currentRates[0] = model.Reactions[0].Rate;
            currentRates[1] = model.Reactions[1].Rate;

            ReflectionUtility.SetHiddenField("_currentRates", solver, currentRates);

            //Test Function
            var jacobian = computeJacobianMethod.Invoke(solver, null);
            double[,] check = new double[2,2];
            check[0, 0] = 2.0;
            check[0, 1] = 0.0;
            check[1, 0] = 0.0;
            check[1, 1] = 1.0;
            Assert.AreEqual(check,jacobian);

            model.Parameters[2].Value = 0.0;
            model.Parameters[3].Value = 0.0;

            currentRates[0] = model.Reactions[0].Rate;
            currentRates[1] = model.Reactions[1].Rate;

            ReflectionUtility.SetHiddenField("_currentRates", solver, currentRates);

            jacobian = computeJacobianMethod.Invoke(solver, null);

            check[0, 0] = 0.0;
            check[0, 1] = 0.0;
            check[1, 0] = 0.0;
            check[1, 1] = 0.0;
            Assert.AreEqual(check, jacobian);

            resetModelStateMethod.Invoke(solver, null);

            model.Parameters[2].Value = 2.0;
            model.Parameters[3].Value = 1.0;

            model.Species[0].Count = 0;
            model.Species[1].Count = 0;

            currentRates[0] = model.Reactions[0].Rate;
            currentRates[1] = model.Reactions[1].Rate;

            ReflectionUtility.SetHiddenField("_currentRates", solver, currentRates);

            jacobian = computeJacobianMethod.Invoke(solver, null);

            check[0, 0] = 2.0;
            check[0, 1] = 0.0;
            check[1, 0] = 0.0;
            check[1, 1] = 1.0;
            Assert.AreEqual(check, jacobian);
        }

        [Test]
        public void ComputeNonlinearModelJacobianTest()
        {
            //0.  Setup
            const string configString = @"{""solver"":""TAU""}";
            Configuration.CurrentConfiguration = Configuration.ConfigurationFromString(configString);

            ModelInfo modelInfo = EmodlLoader.LoadEMODLFile("resources//testmodelTau2.emodl");
            const double duration = 6.28318531;
            const int repeats = 42;
            const int samples = 100;
            var solver = new TauLeaping(modelInfo, duration, repeats, samples);

            //Find Hidden Method to in order to initialize the population of each of the species
            MethodInfo resetModelStateMethod = ReflectionUtility.GetHiddenMethod("ResetModelState", solver);
            resetModelStateMethod.Invoke(solver, null);

            //Find the model object
            var model = ReflectionUtility.GetHiddenField<Model>("model", solver);

            //Find methods
            MethodInfo computeJacobianMethod = ReflectionUtility.GetHiddenMethod("ComputeJacobian", solver);

            //Set currentRates
            double[] currentRates = new double[2];
            currentRates[0] = model.Reactions[0].Rate;
            currentRates[1] = model.Reactions[1].Rate;

            ReflectionUtility.SetHiddenField("_currentRates", solver, currentRates);

            //Test Function
            var jacobian = computeJacobianMethod.Invoke(solver, null);
            double[,] check = new double[2, 2];
            check[0, 0] = 4.0;
            check[0, 1] = 10.0;
            check[1, 0] = 2.0;
            check[1, 1] = 5.0;
            Assert.AreEqual(check, jacobian);
        }

        [Test]
        public void ComputeTauTest()
        {
            //0.  Setup
            const string configString = @"{""solver"":""TAU""}";
            Configuration.CurrentConfiguration = Configuration.ConfigurationFromString(configString);

            ModelInfo modelInfo = EmodlLoader.LoadEMODLFile("resources//testmodelTau.emodl");
            const double duration = 6.28318531;
            const int repeats = 42;
            const int samples = 100;
            var solver = new TauLeaping(modelInfo, duration, repeats, samples);

            //Find Hidden Method to in order to initialize the population of each of the species
            MethodInfo resetModelStateMethod = ReflectionUtility.GetHiddenMethod("ResetModelState", solver);
            resetModelStateMethod.Invoke(solver, null);

            //Find the model object
            var model = ReflectionUtility.GetHiddenField<Model>("model", solver);

            //Find methods
            MethodInfo computeTauMethod = ReflectionUtility.GetHiddenMethod("ComputeTau", solver);
            MethodInfo updateAndSumRatesMethod = ReflectionUtility.GetHiddenMethod("UpdateAndSumRates", solver);

            var jacobian = new double[2,2];
            var subreactions = new List<Reaction>();
            double epsilon;

            epsilon = 0.1;
            subreactions.Add(model.Reactions[0]);
            subreactions.Add(model.Reactions[1]);
            var currentRates = new double[2];
            currentRates[0] = model.Reactions[0].Rate;
            currentRates[1] = model.Reactions[1].Rate;
            object[] inputArray = new object[2];
            inputArray[0] = subreactions; inputArray[1] = currentRates; 
 
            var a0 = updateAndSumRatesMethod.Invoke(solver,inputArray);

            jacobian[0, 0] = 2.0;
            jacobian[0, 1] = 0.0;
            jacobian[1, 0] = 0.0;
            jacobian[1, 1] = 1.0;

            object[] inputArray2 = new object[4];
            inputArray2[0] = jacobian;
            inputArray2[1] = epsilon;
            inputArray2[2] = a0;
            inputArray2[3] = subreactions;

            //Test of one model file with two reactions.

            var tauOut = computeTauMethod.Invoke(solver, inputArray2);
            Assert.AreEqual(0.030000000000000009, tauOut);

            //Test of one model file with zero in some input components.

            a0 = 0.0;
            inputArray2[2] = a0;

            tauOut = computeTauMethod.Invoke(solver, inputArray2);
            Assert.AreEqual(0.0, tauOut);

            updateAndSumRatesMethod.Invoke(solver, inputArray);
            inputArray2[1] = 0.0;
            tauOut = computeTauMethod.Invoke(solver, inputArray2);
            Assert.AreEqual(0.0, tauOut);
        }

        [Test]
        public void FireNonCriticalReactionsTest()
        {
            const string configString = @"{""solver"":""Tau"",""prng_seed"":123, ""prng_index"":1,""RNG"":{""type"":""RANDLIB""}}";
            Configuration.CurrentConfiguration = Configuration.ConfigurationFromString(configString);

            ModelInfo modelInfo = EmodlLoader.LoadEMODLFile("resources\\testmodelMidPoint.emodl");
            const double duration = 6.28318531;
            const int repeats = 42;
            const int samples = 100;
            var solver = new TauLeaping(modelInfo, duration, repeats, samples);

            //Inputs for the method
            // Tau Step
            double tau1 = 0.0;
            var tau2 = (1.0 / 200.0);
            
            //Need a list of reactions
            var currentRates = new double[1];
            var nonCriticalReaction = new List<Reaction>();

            //Find Hidden Method to in order to initialize the population of each of the species
            MethodInfo resetModelStateMethod = ReflectionUtility.GetHiddenMethod("ResetModelState", solver);
            resetModelStateMethod.Invoke(solver, null);

            MethodInfo updateAndSumRatesMethod = ReflectionUtility.GetHiddenMethod("UpdateAndSumRates", solver);

            //Find the model object
            var model = ReflectionUtility.GetHiddenField<Model>("model", solver);
            currentRates[0] = model.Reactions[0].Rate;
            nonCriticalReaction.Add(model.Reactions[0]);

            object[] inputArray1 = new object[3];
            inputArray1[0] = tau1; inputArray1[1] = nonCriticalReaction; inputArray1[2] = currentRates;

            object[] inputArray2 = new object[3];
            inputArray2[0] = tau2; inputArray2[1] = nonCriticalReaction; inputArray2[2] = currentRates;

            //Find Hidden Method.
            MethodInfo fireNonCriticalReactionsMethod = ReflectionUtility.GetHiddenMethod("FireNonCriticalReactions", solver);

            // First Test if Tau = 0;
            fireNonCriticalReactionsMethod.Invoke(solver, inputArray1);

            Assert.AreEqual(400, model.Species[0].Value);
            Assert.AreEqual(200, model.Species[1].Value);
            Assert.AreEqual(900, model.Species[2].Value);

            //Second Test if Tau = = 0.05.  Set the RNG to a known seed number, in order to exactly know what is generated

            ReflectionUtility.RunResetRngFactory();
            ReflectionUtility.SetHiddenField("_distributionSampler", solver, RandLibSampler.CreateRandLibSampler(RNGFactory.GetRNG()));
            fireNonCriticalReactionsMethod.Invoke(solver, inputArray2);

            Assert.AreEqual(384.0, model.Species[0].Value);
            Assert.AreEqual(184.0, model.Species[1].Value);
            Assert.AreEqual(916.0, model.Species[2].Value);

            resetModelStateMethod.Invoke(solver, null);
            object[] inputArray3 = new object[2];
            inputArray3[0] = model.Reactions; inputArray3[1] = currentRates;
            updateAndSumRatesMethod.Invoke(solver, inputArray3);
            ReflectionUtility.RunResetRngFactory();
            ReflectionUtility.SetHiddenField("_distributionSampler", solver, RandLibSampler.CreateRandLibSampler(RNGFactory.GetRNG()));
            fireNonCriticalReactionsMethod.Invoke(solver, inputArray2);

            Assert.AreEqual(384.0, model.Species[0].Value);
            Assert.AreEqual(184.0, model.Species[1].Value);
            Assert.AreEqual(916.0, model.Species[2].Value);
        }

        [Test]
        public void ComputeLTest()
        {
            ////// /Setup the test /////////
            const string configString = @"{""solver"":""TAU""}";
            Configuration.CurrentConfiguration = Configuration.ConfigurationFromString(configString);

            var modelInfo = EmodlLoader.LoadEMODLFile("resources//testmodelTau.emodl");
            const double duration = 6.28318531;
            const int repeats = 42;
            const int samples = 100;
            var solver = new TauLeaping(modelInfo, duration, repeats, samples);

            //Find Hidden Method to in order to initialize the population of each of the species
            var resetModelStateMethod = ReflectionUtility.GetHiddenMethod("ResetModelState", solver);
            resetModelStateMethod.Invoke(solver, null);

            //Find the model object
            var model = ReflectionUtility.GetHiddenField<Model>("model", solver);

            //Find the method
            var computeLMethod = ReflectionUtility.GetHiddenMethod("ComputeL", solver);
            var criticalReactions = new List<Reaction>();
            var nonCriticalReactions = new List<Reaction>();

            //ComputeL(out List<Reaction> criticalReactions, out List<Reaction> nonCriticalReactions, int nc)
            // All noncritical

            var inputArray = new object[3];
            inputArray[0] = criticalReactions;
            inputArray[1] = nonCriticalReactions;
            inputArray[2] = 2;

            computeLMethod.Invoke(solver, inputArray);

            Assert.AreEqual(criticalReactions,inputArray[0]);
            Assert.AreEqual(model.Reactions,inputArray[1]);

            //Please note the above assertions.  Since the method has "out" properties the object[] inputArray is 
            // modified by ComputeLMethod.Invoke.  This is used below as well.

            //3.  Both critical

            resetModelStateMethod.Invoke(solver, null);
            model.Species[0].Count = 3;
            model.Species[1].Count = 2;

            criticalReactions = new List<Reaction>();
            nonCriticalReactions = new List<Reaction>();
           
            inputArray[0] = criticalReactions;
            inputArray[1] = nonCriticalReactions;
            inputArray[2] = 10;

            computeLMethod.Invoke(solver, inputArray);
           
            Assert.AreEqual(nonCriticalReactions, inputArray[1]);
            Assert.AreEqual(model.Reactions, inputArray[0]);

            // One critical, one noncritical.

            resetModelStateMethod.Invoke(solver, null);
            model.Species[0].Count = 200;
            model.Species[1].Count = 2;

            criticalReactions = new List<Reaction>();
            nonCriticalReactions = new List<Reaction>();

            inputArray[0] = criticalReactions;
            inputArray[1] = nonCriticalReactions;
            inputArray[2] = 10;

            computeLMethod.Invoke(solver, inputArray);

            var singleReactionList1 = new List<Reaction>();
            var singleReactionList2 = new List<Reaction>();
            singleReactionList1.Add(model.Reactions[0]); 
            singleReactionList2.Add(model.Reactions[1]);

            Assert.AreEqual(singleReactionList1, inputArray[0]);
            Assert.AreEqual(singleReactionList2, inputArray[1]);
        }

        [Test]
        public void ComputeLMoreThanOneReactantTest()
        {
            // Need to access the RHS of the or boolean (tempmin > tempminproposed))
            // In order to do that I need more than one species in the reactants list
            
            ////// /Setup the test /////////
            const string configString = @"{""solver"":""TAU""}";
            Configuration.CurrentConfiguration = Configuration.ConfigurationFromString(configString);

            var modelInfo = EmodlLoader.LoadEMODLFile("resources//testmodelTau3.emodl");
            const double duration = 6.28318531;
            const int repeats = 42;
            const int samples = 100;
            var solver = new TauLeaping(modelInfo, duration, repeats, samples);

            //Find Hidden Method to in order to initialize the population of each of the species
            var resetModelStateMethod = ReflectionUtility.GetHiddenMethod("ResetModelState", solver);
            resetModelStateMethod.Invoke(solver, null);

            //Find the model object
            var model = ReflectionUtility.GetHiddenField<Model>("model", solver);

            //Find the method
            var computeLMethod = ReflectionUtility.GetHiddenMethod("ComputeL", solver);
            var criticalReactions = new List<Reaction>();
            var nonCriticalReactions = new List<Reaction>();

            //ComputeL(out List<Reaction> criticalReactions, out List<Reaction> nonCriticalReactions, int nc)
            // All noncritical

            var inputArray = new object[3];
            inputArray[0] = criticalReactions;
            inputArray[1] = nonCriticalReactions;
            inputArray[2] = 2;

            computeLMethod.Invoke(solver, inputArray);

            Assert.AreEqual(criticalReactions, inputArray[0]);
            Assert.AreEqual(model.Reactions, inputArray[1]);
        }

        [Test]
        public void GillespieTauTest()
        {
            ////// /Setup the test /////////
            const string configString = @"{""solver"":""Tau"",""prng_seed"":123, ""prng_index"":1,""RNG"":{""type"":""RANDLIB""}}";
            Configuration.CurrentConfiguration = Configuration.ConfigurationFromString(configString);

            ModelInfo modelInfo = EmodlLoader.LoadEMODLFile("resources//testmodelTau.emodl");
            const double duration = 6.28318531;
            const int repeats = 42;
            const int samples = 100;
            var solver = new TauLeaping(modelInfo, duration, repeats, samples);

            //Find the method
            MethodInfo gillespieTauMethod = ReflectionUtility.GetHiddenMethod("GillespieTau", solver);

            //No need to test a0 = 0, since the method is only called when a0 != 0.
            // a0 != 0 

            ReflectionUtility.RunResetRngFactory();
            ReflectionUtility.SetHiddenField("rng", solver, RNGFactory.GetRNG());

            var inputArray = new object[1];
            inputArray[0] = 1;

            var tauOut1 = gillespieTauMethod.Invoke(solver, inputArray);
            Assert.That(tauOut1, Is.EqualTo(0.21309720500123394));

            ReflectionUtility.RunResetRngFactory();
            ReflectionUtility.SetHiddenField("rng", solver, RNGFactory.GetRNG());

            inputArray[0] = 100;

            var tauOut2 = gillespieTauMethod.Invoke(solver, inputArray);
            Assert.That(tauOut2, Is.EqualTo(0.0021309720500123394));

            ReflectionUtility.RunResetRngFactory();
            ReflectionUtility.SetHiddenField("rng", solver, RNGFactory.GetRNG());

            inputArray[0] = 0.001;

            var tauOut3 = gillespieTauMethod.Invoke(solver, inputArray);
            Assert.That(tauOut3, Is.EqualTo(213.09720500123393));
        }

        [Test]
        public void GillespieReactionTest()
        {
            ////// /Setup the test /////////
            const string configString = @"{""solver"":""TAU""}";
            Configuration.CurrentConfiguration = Configuration.ConfigurationFromString(configString);

            var modelInfo = EmodlLoader.LoadEMODLFile("resources//testmodelTau.emodl");
            const double duration = 6.28318531;
            const int repeats = 42;
            const int samples = 100;
            var solver = new TauLeaping(modelInfo, duration, repeats, samples);

             //Find the model object
            var model = ReflectionUtility.GetHiddenField<Model>("model", solver);

            //Find Hidden Method to in order to initialize the population of each of the species
            var resetModelStateMethod = ReflectionUtility.GetHiddenMethod("ResetModelState", solver);
            resetModelStateMethod.Invoke(solver, null);

            var updateAndSumRatesMethod = ReflectionUtility.GetHiddenMethod("UpdateAndSumRates", solver);
            var currentRates = ReflectionUtility.GetHiddenField<double[]>("_currentRates", solver);

            //Find the method
            var gillespieReactionMethod = ReflectionUtility.GetHiddenMethod("GillespieReaction", solver);

            //Test for one value of a0
            ReflectionUtility.RunResetRngFactory();
            ReflectionUtility.SetHiddenField("rng", solver, RNGFactory.GetRNG());

            var inputArray = new object[1];
            inputArray[0] = 0.01;

            var reactionOut = gillespieReactionMethod.Invoke(solver, inputArray);
            Assert.That(reactionOut, Is.EqualTo(model.Reactions[0]));

            //Test for an actual a0 from the model file.
            ReflectionUtility.RunResetRngFactory();
            ReflectionUtility.SetHiddenField("rng", solver, RNGFactory.GetRNG());

            var inputArrayForUpdate = new object[2];
            inputArrayForUpdate[0] = model.Reactions;
            inputArrayForUpdate[1] = currentRates;

            var a0 = updateAndSumRatesMethod.Invoke(solver, inputArrayForUpdate);

            inputArray[0] = a0;

            var reactionOut2 = gillespieReactionMethod.Invoke(solver, inputArray);
            Assert.That(reactionOut2, Is.EqualTo(model.Reactions[0]));

            //Test for a changed a0 and rate parameter

            ReflectionUtility.RunResetRngFactory();
            ReflectionUtility.SetHiddenField("rng", solver, RNGFactory.GetRNG());

            model.Parameters[3].Value = 10.0;
            currentRates = ReflectionUtility.GetHiddenField<double[]>("_currentRates", solver);
            inputArrayForUpdate[1] = currentRates;

            a0 = updateAndSumRatesMethod.Invoke(solver, inputArrayForUpdate);
            inputArray[0] = a0;

            var reactionOut3 = gillespieReactionMethod.Invoke(solver, inputArray);
            Assert.That(reactionOut3, Is.EqualTo(model.Reactions[1]));
        }

        [Test]
        public void FireCriticalReactionTest()
        {
            ////// /Setup the test /////////
            const string configString = @"{""solver"":""TAU""}";
            Configuration.CurrentConfiguration = Configuration.ConfigurationFromString(configString);

            var modelInfo = EmodlLoader.LoadEMODLFile("resources//testmodelTau.emodl");
            const double duration = 6.28318531;
            const int repeats = 42;
            const int samples = 100;
            var solver = new TauLeaping(modelInfo, duration, repeats, samples);

            //Find the model object
            var model = ReflectionUtility.GetHiddenField<Model>("model", solver);

            //Find Hidden Method to in order to initialize the population of each of the species
            var resetModelStateMethod = ReflectionUtility.GetHiddenMethod("ResetModelState", solver);
            resetModelStateMethod.Invoke(solver, null);

            var updateAndSumRatesMethod = ReflectionUtility.GetHiddenMethod("UpdateAndSumRates", solver);
            var currentRates = ReflectionUtility.GetHiddenField<double[]>("_currentRates", solver);

            //Find the method
            var fireCriticalReactionMethod = ReflectionUtility.GetHiddenMethod("FireCriticalReaction", solver);

            //Find a0 update currentRates.
            var inputArrayForUpdate = new object[2];
            inputArrayForUpdate[0] = model.Reactions;
            inputArrayForUpdate[1] = currentRates;

            var a0 = updateAndSumRatesMethod.Invoke(solver, inputArrayForUpdate);
            currentRates = ReflectionUtility.GetHiddenField<double[]>("_currentRates", solver);

            //Define inputs for method  Here there is one reaction

            ReflectionUtility.RunResetRngFactory();
            ReflectionUtility.SetHiddenField("rng", solver, RNGFactory.GetRNG());

            var inputArray = new object[3];
            var criticalReactions = new List<Reaction>();
            criticalReactions.Add(model.Reactions[0]);
            var criticalRates = new double[1];
            criticalRates[0] = currentRates[0];

            inputArray[0] = criticalReactions;
            inputArray[1] = criticalRates;
            inputArray[2] = criticalRates[0];

            fireCriticalReactionMethod.Invoke(solver, inputArray);

            Assert.AreEqual(6,model.Species[0].Count);
            Assert.AreEqual(1,model.Species[1].Count);

            //Define inputs for method  Here there is for two reactions

            ReflectionUtility.RunResetRngFactory();
            ReflectionUtility.SetHiddenField("rng", solver, RNGFactory.GetRNG());
            resetModelStateMethod.Invoke(solver, null);

            inputArray = new object[3];
            criticalReactions = new List<Reaction>();
            criticalReactions.Add(model.Reactions[0]);
            criticalReactions.Add(model.Reactions[1]);
            criticalRates = new double[2];
            criticalRates[0] = currentRates[0];
            criticalRates[1] = currentRates[1];

            inputArray[0] = criticalReactions;
            inputArray[1] = criticalRates;
            inputArray[2] = a0;

            fireCriticalReactionMethod.Invoke(solver, inputArray);

            Assert.AreEqual(6, model.Species[0].Count);
            Assert.AreEqual(1, model.Species[1].Count);

            ReflectionUtility.RunResetRngFactory();
            ReflectionUtility.SetHiddenField("rng", solver, RNGFactory.GetRNG());
            resetModelStateMethod.Invoke(solver, null);

            inputArray = new object[3];
            criticalReactions = new List<Reaction>();
            criticalReactions.Add(model.Reactions[0]);
            criticalReactions.Add(model.Reactions[1]);
            criticalRates = new double[2];
            criticalRates[0] = currentRates[0];
            criticalRates[1] = currentRates[1];

            inputArray[0] = criticalReactions;
            inputArray[1] = criticalRates;
            inputArray[2] = a0;

            fireCriticalReactionMethod.Invoke(solver, inputArray);

            Assert.AreEqual(6, model.Species[0].Count);
            Assert.AreEqual(1, model.Species[1].Count);
        }

        [Test]
        public void CheckParametersTest()
        {
            var configString = @"{""solver"":""Tau"",""tau-leaping"":{""epsilon"": -1}}";
            Configuration.CurrentConfiguration = Configuration.ConfigurationFromString(configString);

            var modelInfo = EmodlLoader.LoadEMODLFile("resources//testmodelTau.emodl");
            const double duration = 6.28318531;
            const int repeats = 42;
            const int samples = 100;

            try
            {
                var solver = new TauLeaping(modelInfo, duration, repeats, samples);
                Assert.Fail();
            }
            catch (ApplicationException ex)
            {
                Assert.That(ex.GetBaseException().ToString(), Is.StringContaining("Epsilon was set to less than or equal to zero."));
            }

            configString = @"{""solver"":""Tau"",""tau-leaping"":{""Nc"": -1}}";
            Configuration.CurrentConfiguration = Configuration.ConfigurationFromString(configString);

            try
            {
                var solver = new TauLeaping(modelInfo, duration, repeats, samples);
                Assert.Fail();
            }
            catch (ApplicationException ex)
            {
                Assert.That(ex.GetBaseException().ToString(), Is.StringContaining("Nc was et to less than zero or equal to zero."));
            }

            configString = @"{""solver"":""Tau"",""tau-leaping"":{""Multiple"": -1}}";
            Configuration.CurrentConfiguration = Configuration.ConfigurationFromString(configString);

            try
            {
                var solver = new TauLeaping(modelInfo, duration, repeats, samples);
                Assert.Fail();
            }
            catch (ApplicationException ex)
            {
                Assert.That(ex.GetBaseException().ToString(), Is.StringContaining("Multiple was set to less than or equal to zero."));
            }

            configString = @"{""solver"":""Tau"",""tau-leaping"":{""SSARuns"": 0}}";
            Configuration.CurrentConfiguration = Configuration.ConfigurationFromString(configString);

            try
            {
                var solver = new TauLeaping(modelInfo, duration, repeats, samples);
                Assert.Fail();
            }
            catch (ApplicationException ex)
            {
                Assert.That(ex.GetBaseException().ToString(), Is.StringContaining("SSAruns was set to less than one."));
            }
        }

        [Test]
        public void TestToString()
        {
            const string configString = @"{""solver"":""Tau""}";
            Configuration.CurrentConfiguration = Configuration.ConfigurationFromString(configString);

            ModelInfo modelInfo = EmodlLoader.LoadEMODLFile("resources\\testmodel.emodl");
            const double duration = 6.28318531;
            const int repeats = 42;
            const int samples = 100;
            var solver = new TauLeaping(modelInfo, duration, repeats, samples);

            Assert.AreEqual("Tau-Leaping", solver.ToString());
        }
    }
}
