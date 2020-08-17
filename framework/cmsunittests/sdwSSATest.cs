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
    internal class sdwSSATest : AssertionHelper
    // ReSharper restore InconsistentNaming
    {
        [Test]
        public void TestStateDependentReactionSet1()
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
            var reaction1 = CreateReaction(info1, reactant1, info2, reactant2, info3, product1, rate1);
            Assert.AreEqual(reaction1.Reactants[0], reactant1);
            Assert.AreEqual(reaction1.Reactants[1], reactant2);
            Assert.AreEqual(reaction1.Products[0], product1);

            const int gammaSize = 10;
            var reactions = new List<Reaction> { reaction1 };
            var reactionSet1 = new StateDependentReactionSet(reactions, gammaSize);
            Assert.AreEqual(gammaSize, reactionSet1.GammaSize);
            Assert.AreEqual(1, reactionSet1.NumReactions);
            Assert.AreEqual(reactions.Count, reactionSet1.Reactions.Count);

            List<double[]> gamma, pc;
            var rates = new[] { 2.0 };
            CreateGammaPC(gammaSize, rates, out gamma, out pc);
            var gammaIndex = new[] { 0 };
            var binIndex = new[] { 0 };

            double b0 = reactionSet1.UpdateRatesIteration1(gamma, pc, pc, ref gammaIndex, ref binIndex);
            Assert.AreEqual(2.0, reactionSet1.CurrentRates[0]);
            Assert.AreEqual(4.0, reactionSet1.PredilectionRates[0]);
            Assert.AreEqual(b0, reactionSet1.PredilectionRates[0]);
            Assert.AreEqual(gammaSize - 1, gammaIndex[0]);
            Assert.AreEqual(0, binIndex[0]);

            b0 = reactionSet1.UpdateRatesIteration2(gamma, pc, pc, ref gammaIndex, ref binIndex);
            Assert.AreEqual(2.0, reactionSet1.CurrentRates[0]);
            Assert.AreEqual(4.0, reactionSet1.PredilectionRates[0]);
            Assert.AreEqual(b0, reactionSet1.PredilectionRates[0]);
            Assert.AreEqual(gammaSize - 1, gammaIndex[0]);
            Assert.AreEqual(gammaSize - 1, binIndex[0]);

            reactionSet1.FireReaction(0);
            Assert.AreEqual(9, reactant1.Value);
            Assert.AreEqual(39, reactant2.Value);
            Assert.AreEqual(91, product1.Value);

            reactionSet1.FireReaction(0);
            Assert.AreEqual(8, reactant1.Value);
            Assert.AreEqual(38, reactant2.Value);
            Assert.AreEqual(92, product1.Value);

            int mu = reactionSet1.SelectReaction(reactionSet1.CurrentRates.Sum());
            Assert.AreEqual(0, mu);
            mu = reactionSet1.SelectReaction(0.0);
            Assert.AreEqual(0, mu);
        }

        [Test]
        public void TestStateDependentReactionSet2()
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
            const int gammaSize = 10;
            var reactions = new List<Reaction> { reaction1, reaction2 };
            var reactionSet2 = new StateDependentReactionSet(reactions, gammaSize);

            List<double[]> gamma, pc;
            var rates = new[] { 1.5, 2.0 };
            CreateGammaPC(gammaSize, rates, out gamma, out pc);
            var gammaIndex = new[] { 0, 0 };
            var binIndex = new[] { 0, 0 };

            double b0 = reactionSet2.UpdateRatesIteration1(gamma, pc, pc, ref gammaIndex, ref binIndex);
            Assert.AreEqual(9.0, b0);
            Assert.AreEqual(3.0, reactionSet2.CurrentRates[1]);
            Assert.AreEqual(3.0, reactionSet2.PredilectionRates[0]);
            Assert.AreEqual(2, reactionSet2.NumReactions);

            b0 = reactionSet2.UpdateRatesIteration2(gamma, pc, pc, ref gammaIndex, ref binIndex);
            Assert.AreEqual(5.0, reactionSet2.CurrentRates.Sum());
            Assert.AreEqual(3.0, reactionSet2.PredilectionRates[0]);
            Assert.AreEqual(9.0, b0);
            Assert.AreEqual(b0, reactionSet2.PredilectionRates.Sum());
            Assert.AreEqual(4, gammaIndex[0]);
            Assert.AreEqual(4, binIndex[0]);

            reactionSet2.FireReaction(0);
            Assert.AreEqual(9, reactant1.Value);
            Assert.AreEqual(39, reactant2.Value);
            Assert.AreEqual(91, product1.Value);

            reactionSet2.FireReaction(1);
            Assert.AreEqual(0, reactant3.Value);
            Assert.AreEqual(1, reactant4.Value);
            Assert.AreEqual(4, product2.Value);

            int mu = reactionSet2.SelectReaction(reactionSet2.CurrentRates.Sum());
            Assert.AreEqual(1, mu);
            mu = reactionSet2.SelectReaction(0.0);
            Assert.AreEqual(0, mu);
        }

        [Test]
        public void TestStateDependentGammaInfo()
        {
            const int numReactions = 3;
            const int gammaSize = 5;
            const int bcThreshold = 20;
            var gammaInfo = new StateDependentGammaInfo(numReactions, gammaSize, bcThreshold);
            Assert.That(Enumerable.Repeat(1.0, gammaSize).ToArray(), Is.EquivalentTo(gammaInfo.IntermediateGamma[0]));
            Assert.That(Enumerable.Repeat(1.0, gammaSize).ToArray(), Is.EquivalentTo(gammaInfo.IntermediateGamma[1]));
            Assert.That(Enumerable.Repeat(1.0, gammaSize).ToArray(), Is.EquivalentTo(gammaInfo.IntermediateGamma[2]));

            List<double[]> gamma, pc;
            var rates = new[] { 1.0, 1.0, 1.0 };
            CreateGammaPC(gammaSize, rates, out gamma, out pc);
            Assert.That(pc, Is.EquivalentTo(gammaInfo.IntermediatePropensityCutoff));
        }

        [Test]
        public void TestStateDependentGammaInfoFunctions()
        {
            const int numReactions = 3;
            const int gammaSize = 5;
            const int bcThreshold = 20;
            var gammaInfo = new StateDependentGammaInfo(numReactions, gammaSize, bcThreshold);

            const double weight = 1.0;
            var n = new List<int[]> { new[] { 20, 20, 20, 20, 20 }, new[] { 1, 2, 3, 4, 5 }, new[] { 10, 20, 30, 15, 5 } };
            var lambda = new List<double[]> { new[] { 10.0, 20.0, 40.0, 100.0, 1000.0 }, new[] { 2.0, 4.0, 6.0, 8.0, 10.0 }, new[] { 20.0, 10.0, 60.0, 30.0, 10.0 } };

            gammaInfo.UpdateGamma(weight, n, lambda);
            gammaInfo.SetIntermediateGamma();
            Assert.That(new[] { 2.0, 1.0, 0.5, 0.2, 0.02 }, Is.EquivalentTo(gammaInfo.IntermediateGamma[0]));
            Assert.That(new[] { 0.5, 0.5, 0.5, 0.5, 0.5 }, Is.EquivalentTo(gammaInfo.IntermediateGamma[1]));
            Assert.That(new[] { 0.5, 2.0, 0.5, 0.5, 0.5 }, Is.EquivalentTo(gammaInfo.IntermediateGamma[2]));

            var startPC = new[] { 0.0, 0.2, 0.5 };
            var endPC = new[] { 0.5, 0.8, 1.0 };
            var ppc = new List<double[]>();
            ppc.AddRange(gammaInfo.IntermediatePropensityCutoff);
            gammaInfo.UpdatePropensityCutoff(startPC, endPC);
            Assert.That(ppc, Is.EqualTo(gammaInfo.PreviousIntermediatePropensityCutoff));
            Assert.That(new[] { 0.1, 0.2, 0.1 * 3, 0.4 }, Is.EqualTo(gammaInfo.IntermediatePropensityCutoff[0]));
            Assert.That(new[] { 0.2 + (0.8 - 0.2) / 5, 0.2 + (0.8 - 0.2) / 5 * 2, 0.2 + (0.8 - 0.2) / 5 * 3, 0.2 + (0.8 - 0.2) / 5 * 4 }, Is.EqualTo(gammaInfo.IntermediatePropensityCutoff[1]));
            Assert.That(new[] { 0.6, 0.7, 0.8, 0.9 }, Is.EqualTo(gammaInfo.IntermediatePropensityCutoff[2]));


            var ppc2 = new List<double[]>();
            ppc2.AddRange(gammaInfo.IntermediatePropensityCutoff);
            gammaInfo.MergeBins();
            Assert.That(new[] { 0.1, 0.2, 0.1 * 3, 0.4 }, Is.EqualTo(gammaInfo.PreviousIntermediatePropensityCutoff[0]));
            Assert.That(new[] { 0.2 + (0.8 - 0.2) / 5, 0.2 + (0.8 - 0.2) / 5 * 2, 0.2 + (0.8 - 0.2) / 5 * 3, 0.2 + (0.8 - 0.2) / 5 * 4 }, Is.EqualTo(gammaInfo.PreviousIntermediatePropensityCutoff[1]));
            Assert.That(new[] { 0.6, 0.7, 0.8, 0.9 }, Is.EqualTo(gammaInfo.PreviousIntermediatePropensityCutoff[2]));
            Assert.That(ppc2[0], Is.EqualTo(gammaInfo.IntermediatePropensityCutoff[0]));
            Assert.That(new[] { 1.0 }, Is.EqualTo(gammaInfo.IntermediatePropensityCutoff[1]));
            Assert.That(new[] { 0.7, 0.8 }, Is.EqualTo(gammaInfo.IntermediatePropensityCutoff[2]));

            gammaInfo.UpdateStructure();
            var gammaNum = GetHiddenField<List<double[]>>("_gammaNum", gammaInfo);
            Assert.AreEqual(new double[gammaSize], gammaNum[0]);
            Assert.AreEqual(new double[1], gammaNum[1]);
            Assert.AreEqual(new double[3], gammaNum[2]);
        }


        [Test]
        public void TestsdwSSAConstructorWithDefaults()
        {
            var solver = InstantiateSolver();

            //test default values
            Assert.AreEqual(0, GetHiddenField<int>("_trajectoryCounter", solver));
            Assert.AreEqual(0.0, GetHiddenField<double>("_runningMean", solver));
            Assert.AreEqual(0.0, GetHiddenField<double>("_runningVariance", solver));

            Assert.AreEqual(100000, GetHiddenField<int>("_crossEntropyRuns", solver));
            Assert.AreEqual(0.01, GetHiddenField<double>("_crossEntropyThreshold", solver));
            Assert.AreEqual(20, GetHiddenField<int>("_binCountThreshold", solver));
            Assert.AreEqual(200, GetHiddenField<int>("_crossEntropyMinDataSize", solver));

            Assert.AreEqual(15, GetHiddenField<int>("_gammaSize", solver));
            Assert.AreEqual(new int[2], GetHiddenField<int[]>("_gammaIndex", solver));
            Assert.AreEqual(new int[2], GetHiddenField<int[]>("_binIndex", solver));

            Assert.AreEqual("rever_isom_biasingParameters.json", GetHiddenField<string>("_biasingParametersFileName", solver));
            Assert.AreEqual(true, GetHiddenField<Boolean>("_biasingParametersFlag", solver));
            Assert.That(GetHiddenField<BiasingParameters>("_biasingParameters", solver).RareEvent.ExpressionName, Is.EqualTo(string.Empty));

            Assert.AreEqual("reExpression", GetHiddenField<string>("_reExpressionName", solver));
            Assert.AreEqual("reVal", GetHiddenField<string>("_reValName", solver));
            Assert.AreEqual(0.0, GetHiddenField<double>("_rareEventValue", solver));

            Assert.AreEqual("reExpression", GetHiddenField<Expression>("_reExpression", solver).Name);
            var equalto = GetHiddenField<EqualTo>("_rareEventTest", solver);
            Assert.AreEqual(true, equalto.Value);

            solver.Initialize();
            Assert.AreEqual(30.0, GetHiddenField<double>("_rareEventValue", solver));
            Assert.AreEqual(1, GetHiddenField<int>("_rareEventType", solver));
            Assert.AreEqual("global", GetHiddenField<BiasingParameters>("_biasingParameters", solver).RareEvent.ExpressionLocale);

            var gamma = GetHiddenField<List<double[]>>("_gamma", solver);
            var pc = GetHiddenField<List<double[]>>("_propensityCutoff", solver);
            Assert.AreEqual(new double[15], gamma[0]);
            Assert.AreEqual(new double[15], gamma[1]);
            Assert.AreEqual(new double[14], pc[0]);
            Assert.AreEqual(new double[14], pc[1]);
        }

        [Test]
        public void TestsdwSSAConstructorWithBiasingParameters()
        {
            const string configString = @"{""solver"":""SDWSSA"",""prng_seed"":123, ""prng_index"":1,""RNG"":{""type"":""RANDLIB""}, ""sdwSSA"":{""crossEntropyThreshold"":0.005, ""crossEntropyRuns"":1000000, ""biasingParametersFileName"":""resources\\rever_isom_sdwSSA_CEinfo.json""}}";
            Configuration.CurrentConfiguration = Configuration.ConfigurationFromString(configString);
            RunResetRNGFactory();

            var modelInfo = EmodlLoader.LoadEMODLFile("resources\\rever_isom.emodl");
            const double duration = 10.0;
            const int repeats = 1000000;
            const int samples = 1;
            var solver = new sdwSSA(modelInfo, duration, repeats, samples);

            Assert.AreEqual(1000000, GetHiddenField<int>("_crossEntropyRuns", solver));
            Assert.AreEqual(0.005, GetHiddenField<double>("_crossEntropyThreshold", solver));
            Assert.AreEqual("resources\\rever_isom_sdwSSA_CEinfo.json", GetHiddenField<string>("_biasingParametersFileName", solver));
            Assert.AreEqual(false, GetHiddenField<Boolean>("_biasingParametersFlag", solver));

            var biasingParameters = GetHiddenField<BiasingParameters>("_biasingParameters", solver);
            Assert.AreEqual("global", biasingParameters.Locales[0].Name);
            Assert.AreEqual(2, biasingParameters.Locales[0].ReactionCount);
            Assert.AreEqual("reExpression", biasingParameters.RareEvent.ExpressionName);
            Assert.AreEqual("global", biasingParameters.RareEvent.ExpressionLocale);
            Assert.AreEqual(2, biasingParameters.RareEvent.IntermediateRareEventCount);
            Assert.That(new float[] { 23, 30 }, Is.EquivalentTo(biasingParameters.RareEvent.Thresholds));

            var reaction1 = biasingParameters.Locales[0].Reactions[0];
            Assert.AreEqual("interconvert1", reaction1.Name);
            Assert.AreEqual(13, reaction1.RareEvents[0].BinCount);
            Assert.AreEqual(new[] { 2.06313742606519, 1.62752608139573, 1.28524659899656, 1.1551778002545701, 1.08968524294438, 1.05978612851480, 1.05213056082493, 1.04916675907724, 1.00586093386706, 1.01144452928748, 1.02083317777346, 1.01513053565442, 1.02988441360663 }, reaction1.RareEvents[0].Gammas);
            Assert.AreEqual(new[] { 0.330135030236500, 0.381663104833693, 0.433191179430885, 0.484719254028077, 0.536247328625270, 0.587775403222462, 0.639303477819654, 0.690831552416846, 0.742359627014039, 0.845415776208423, 0.896943850805615, 0.948471925402808 }, reaction1.RareEvents[0].Thresholds);
            Assert.AreEqual(13, reaction1.RareEvents[1].BinCount);
            Assert.AreEqual(new[] { 2.476968001615, 1.64117464971143, 1.30252163933866, 1.16127755928718, 1.10320326699327, 1.06106835822293, 1.04512721554582, 1.03240567633747, 1.01985869635349, 1.01823044314009, 1.02103023510684, 1.01935203218744, 1.01472718645925 }, reaction1.RareEvents[1].Gammas);
            Assert.AreEqual(new[] { 0.3301350302365, 0.381663104833693, 0.433191179430885, 0.484719254028077, 0.53624732862527, 0.587775403222462, 0.639303477819654, 0.690831552416846, 0.742359627014039, 0.845415776208423, 0.896943850805615, 0.948471925402808 }, reaction1.RareEvents[1].Thresholds);
            
            var reaction2 = biasingParameters.Locales[0].Reactions[1];
            Assert.AreEqual("interconvert2", reaction2.Name);
            Assert.AreEqual(14, reaction2.RareEvents[0].BinCount);
            Assert.AreEqual(new[] { 0.98850417047106398, 0.917309756657961, 1.03395184011107, 0.975731835263174, 1.00611856107535, 0.984970326735658, 0.94726738124926602, 0.96614763699818795, 0.93530750400835505, 0.910799215582815, 0.86803893648544395, 0.77448007324552304, 0.613574832946266, 0.499713830449480 }, reaction2.RareEvents[0].Gammas);
            Assert.AreEqual(new[] { 0.123991842069282, 0.170343933275611, 0.216696024481939, 0.263048115688268, 0.309400206894597, 0.355752298100925, 0.402104389307254, 0.448456480513583, 0.494808571719912, 0.541160662926240, 0.587512754132569, 0.633864845338898, 0.680216936545227 }, reaction2.RareEvents[0].Thresholds);
            Assert.AreEqual(14, reaction2.RareEvents[1].BinCount);
            Assert.AreEqual(new[] { 0.98423158970711, 1.01498329340952, 0.990253088665499, 0.975106365160516, 0.986861478077715, 0.978625997732093, 0.962392697806862, 0.949116411528197, 0.936334221304811, 0.909940261969898, 0.85882305727793, 0.76550824189768, 0.611330117387254, 0.413838468692494 }, reaction2.RareEvents[1].Gammas);
            Assert.AreEqual(new[] { 0.123991842069282, 0.170343933275611, 0.216696024481939, 0.263048115688268, 0.309400206894597, 0.355752298100925, 0.402104389307254, 0.448456480513583, 0.494808571719912, 0.54116066292624, 0.587512754132569, 0.633864845338898, 0.680216936545227 }, reaction2.RareEvents[1].Thresholds);
        }

        [Test]
        public void TestInitialize()
        {
            const string configString = @"{""solver"":""SDWSSA"",""sdwSSA"":{""biasingParametersFileName"":""resources\\rever_isom_CEinfo.json""}}";
            Configuration.CurrentConfiguration = Configuration.ConfigurationFromString(configString);

            ModelInfo modelInfo = EmodlLoader.LoadEMODLFile("resources\\rever_isom.emodl");
            const double duration = 10.0;
            const int repeats = 1000000;
            const int samples = 1;

            var solver = new sdwSSA(modelInfo, duration, repeats, samples);
            solver.Initialize();

            Assert.AreEqual(30.0, GetHiddenField<double>("_rareEventValue", solver));
            Assert.AreEqual(0.0, GetHiddenField<Expression>("_reExpression", solver).Value);
            Assert.AreEqual(1, GetHiddenField<int>("_rareEventType", solver));

            var biasingParameters = GetHiddenField<BiasingParameters>("_biasingParameters", solver);
            var reaction1 = biasingParameters.Locales[0].Reactions[0];
            var reaction2 = biasingParameters.Locales[0].Reactions[1];
            var gammas = GetHiddenField<List<double[]>>("_gamma", solver);
            var pcs = GetHiddenField<List<double[]>>("_propensityCutoff", solver);
            Assert.AreEqual(reaction1.RareEvents[1].Gammas, gammas[0]);
            Assert.AreEqual(reaction2.RareEvents[1].Gammas, gammas[1]);
            Assert.AreEqual(reaction1.RareEvents[1].Thresholds, pcs[0]);
            Assert.AreEqual(reaction2.RareEvents[1].Thresholds, pcs[1]);
        }

        [Test]
        public void TestCheckParameters1()
        {
            const string configString = @"{""solver"":""SDWSSA"",""sdwSSA"":{""crossEntropyRuns"":100}}";
            Configuration.CurrentConfiguration = Configuration.ConfigurationFromString(configString);

            var modelInfo = EmodlLoader.LoadEMODLFile("resources\\rever_isom.emodl");
            const double duration = 10.0;
            const int repeats = 1000000;
            const int samples = 1;
            var solver = new sdwSSA(modelInfo, duration, repeats, samples);
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
        public void TestCheckParameters2()
        {
            const string configString = @"{""solver"":""SDWSSA"",""sdwSSA"":{""crossEntropyThreshold"":1.001}}";
            Configuration.CurrentConfiguration = Configuration.ConfigurationFromString(configString);

            var modelInfo = EmodlLoader.LoadEMODLFile("resources\\rever_isom.emodl");
            const double duration = 10.0;
            const int repeats = 1000000;
            const int samples = 1;

            var solver = new sdwSSA(modelInfo, duration, repeats, samples);
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
        public void TestCheckParameters3()
        {
            const string configString = @"{""solver"":""SDWSSA"",""sdwSSA"":{""crossEntropyThreshold"":-0.001}}";
            Configuration.CurrentConfiguration = Configuration.ConfigurationFromString(configString);

            var modelInfo = EmodlLoader.LoadEMODLFile("resources\\rever_isom.emodl");
            const double duration = 10.0;
            const int repeats = 1000000;
            const int samples = 1;

            var solver = new sdwSSA(modelInfo, duration, repeats, samples);
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
            const string configString = @"{""solver"":""SDWSSA"", ""sdwSSA"":{""crossEntropyMinDataSize"":5}}";
            Configuration.CurrentConfiguration = Configuration.ConfigurationFromString(configString);

            var modelInfo = EmodlLoader.LoadEMODLFile("resources\\rever_isom.emodl");
            const double duration = 10.0;
            const int repeats = 1000000;
            const int samples = 1;

            var solver = new sdwSSA(modelInfo, duration, repeats, samples);
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
        public void TestCheckParameters5()
        {
            const string configString = @"{""solver"":""SDWSSA"", ""sdwSSA"":{""gammaSize"":-2}}";
            Configuration.CurrentConfiguration = Configuration.ConfigurationFromString(configString);

            var modelInfo = EmodlLoader.LoadEMODLFile("resources\\rever_isom.emodl");
            const double duration = 10.0;
            const int repeats = 1000000;
            const int samples = 1;

            var solver = new sdwSSA(modelInfo, duration, repeats, samples);
            var method = GetHiddenMethod("CheckParameters", solver);

            try
            {
                method.Invoke(solver, null);
                Assert.Fail("CheckParameters should have thrown an Error");
            }
            catch (TargetInvocationException ex)
            {
                Assert.That(ex.GetBaseException().ToString(), Is.StringContaining("gammaSize"));
            }
        }

        [Test]
        public void TestCheckParameters6()
        {
            const string configString = @"{""solver"":""SDWSSA"", ""sdwSSA"":{""binCount"":1}}";
            Configuration.CurrentConfiguration = Configuration.ConfigurationFromString(configString);

            var modelInfo = EmodlLoader.LoadEMODLFile("resources\\rever_isom.emodl");
            const double duration = 10.0;
            const int repeats = 1000000;
            const int samples = 1;

            var solver = new sdwSSA(modelInfo, duration, repeats, samples);
            var method = GetHiddenMethod("CheckParameters", solver);

            try
            {
                method.Invoke(solver, null);
                Assert.Fail("CheckParameters should have thrown an Error");
            }
            catch (TargetInvocationException ex)
            {
                Assert.That(ex.GetBaseException().ToString(), Is.StringContaining("binCount must be an integer >= 10"));
            }
        }

        [Test]
        public void TestCheckParameters7()
        {
            const string configString = @"{""solver"":""SDWSSA"",""sdwSSA"":{""reExpressionName"":""reExpNameCustom""}}";
            Configuration.CurrentConfiguration = Configuration.ConfigurationFromString(configString);

            var modelInfo = EmodlLoader.LoadEMODLFile("resources\\rever_isom.emodl");
            const double duration = 10.0;
            const int repeats = 1000000;
            const int samples = 1;
            var solver = new sdwSSA(modelInfo, duration, repeats, samples);
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
        public void TestCheckParameters8()
        {
            const string configString = @"{""solver"":""SDWSSA"",""sdwSSA"":{""reExpressionName"":""reExpNameCustom"", ""reValName"":""reValNameCustom""}}";
            Configuration.CurrentConfiguration = Configuration.ConfigurationFromString(configString);
            RunResetRNGFactory();

            var modelInfo = EmodlLoader.LoadEMODLFile("resources\\rever_isom_custom.emodl");
            const double duration = 10.0;
            const int repeats = 1000000;
            const int samples = 1;
            var solver = new sdwSSA(modelInfo, duration, repeats, samples);
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
        public void TestUpdateBinEdges()
        {
            const string configString = @"{""solver"":""SDWSSA""}";
            Configuration.CurrentConfiguration = Configuration.ConfigurationFromString(configString);

            ModelInfo modelInfo = EmodlLoader.LoadEMODLFile("resources\\rever_isom.emodl");
            const double duration = 10.0;
            const int repeats = 1000000;
            const int samples = 1;
            var solver = new sdwSSA(modelInfo, duration, repeats, samples);

            var startPC = new[] { 0.012, 0.432 };
            var endPC = new[] { 0.899, 0.2134 };
            const double a0 = 2.0;
            const int mu = 0;
            var inputObjectArray = new object[4];
            inputObjectArray[0] = startPC;
            inputObjectArray[1] = endPC;
            inputObjectArray[2] = a0;
            inputObjectArray[3] = mu;

            GetHiddenMethod("UpdateBinEdges", solver).Invoke(solver, inputObjectArray);
            Assert.AreEqual(startPC[mu], 0.0);
        }

        [Test]
        public void TestUpdateBiasingParameters()
        {
            const string configString = @"{""solver"":""SDWSSA"", ""sdwSSA"":{""crossEntropyThreshold"":0.005, ""crossEntropyRuns"":1000000, ""biasingParametersFileName"":""resources\\rever_isom_CEinfo.json""}}";
            Configuration.CurrentConfiguration = Configuration.ConfigurationFromString(configString);

            var modelInfo = EmodlLoader.LoadEMODLFile("resources\\rever_isom.emodl");
            const double duration = 10.0;
            const int repeats = 1000000;
            const int samples = 1;
            var solver = new sdwSSA(modelInfo, duration, repeats, samples);

            var gammaInfo = new StateDependentGammaInfo(2, 10, 20);
            var method = GetHiddenMethod("UpdateBiasingParameters", solver);
            var inputObjectArray = new object[2];
            inputObjectArray[0] = gammaInfo;
            inputObjectArray[1] = 35;
            method.Invoke(solver, inputObjectArray);

            var biasingParameters = GetHiddenField<BiasingParameters>("_biasingParameters", solver);
            Assert.AreEqual(35, biasingParameters.RareEvent.Thresholds[2]);
        }

        [Test]
        public void TestUpdateReactionInfo()
        {
            const string configString = @"{""solver"":""SDWSSA"", ""sdwSSA"":{""crossEntropyThreshold"":0.005, ""crossEntropyRuns"":1000000, ""biasingParametersFileName"":""resources\\rever_isom_CEinfo.json""}}";
            Configuration.CurrentConfiguration = Configuration.ConfigurationFromString(configString);

            var modelInfo = EmodlLoader.LoadEMODLFile("resources\\rever_isom.emodl");
            const double duration = 10.0;
            const int repeats = 1000000;
            const int samples = 1;
            var solver = new sdwSSA(modelInfo, duration, repeats, samples);
           
            var biasingParameters = GetHiddenField<BiasingParameters>("_biasingParameters", solver);
            var tempREInfo = biasingParameters.Locales[0].Reactions[0].RareEvents[0];
            var inputArray = new object[] {tempREInfo, 0};
            GetHiddenMethod("UpdateReactionInfo", solver).Invoke(solver, inputArray);

            Assert.AreEqual(tempREInfo.Thresholds, biasingParameters.Locales[0].Reactions[0].RareEvents[2].Thresholds);
        }

        [Test]
        public void TestLocaleAndReactionIndex()
        {
            const string configString = @"{""solver"":""SDWSSA"", ""sdwSSA"":{""crossEntropyThreshold"":0.005, ""crossEntropyRuns"":1000000, ""biasingParametersFileName"":""resources\\rever_isom_CEinfo.json""}}";
            Configuration.CurrentConfiguration = Configuration.ConfigurationFromString(configString);
            RunResetRNGFactory();

            var modelInfo = EmodlLoader.LoadEMODLFile("resources\\rever_isom.emodl");
            const double duration = 10.0;
            const int repeats = 1000000;
            const int samples = 1;
            var solver = new sdwSSA(modelInfo, duration, repeats, samples);

            var reactionSet = GetHiddenField<StateDependentReactionSet>("_reactions", solver);
            var reaction1 = reactionSet.Reactions[0];
            var inputObject = new object[2];
            inputObject[0] = reaction1;
            inputObject[1] = 0;
            Assert.AreEqual(0, GetHiddenMethod("ReactionIndex", solver).Invoke(solver, inputObject));
            var inputObject2 = new object[1];
            inputObject2[0] = reaction1;
            Assert.AreEqual(0, GetHiddenMethod("LocaleIndex", solver).Invoke(solver, inputObject2));
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
                Assert.That(ex.GetBaseException().ToString(), Is.StringContaining("sdwSSA doesn't use CalculateProposedTau()"));
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
                Assert.That(ex.GetBaseException().ToString(), Is.StringContaining("sdwSSA doesn't use ExecuteReactions()"));
            }
        }

        [Test]
        public void TestSetRareEventType()
        {
            const string configString = @"{""solver"":""SDWSSA"",""sdwSSA"":{""biasingParametersFileName"":""resources\\rever_isom_CEinfo.json""}}";
            Configuration.CurrentConfiguration = Configuration.ConfigurationFromString(configString);

            var modelInfo = EmodlLoader.LoadEMODLFile("resources\\rever_isom.emodl");
            const double duration = 10.0;
            const int repeats = 100000;
            const int samples = 1;
            var solver = new sdwSSA(modelInfo, duration, repeats, samples);
            solver.Initialize();

            var method = FindMethod("SetRareEventType", typeof(sdwSSA));
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
            const string configString = @"{""solver"":""SDWSSA"",""sdwSSA"":{""biasingParametersFileName"":""resources\\rever_isom_CEinfo.json""}}";
            Configuration.CurrentConfiguration = Configuration.ConfigurationFromString(configString);

            var modelInfo = EmodlLoader.LoadEMODLFile("resources\\rever_isom.emodl");
            const double duration = 10.0;
            const int repeats = 100000;
            const int samples = 1;
            var solver = new sdwSSA(modelInfo, duration, repeats, samples);
            solver.Initialize();

            var method = FindMethod("SelectAndFireReaction", typeof(sdwSSA));
            var inputArray = new object[] { 0.1 };
            var mu = (int)method.Invoke(solver, inputArray);
            Assert.AreEqual(0, mu);
        }


        [Test]
        public void TestRunCrossEntropy()
        {
            const string configString = @"{""solver"":""SDWSSA"",""prng_seed"":123, ""prng_index"":1,""RNG"":{""type"":""RANDLIB""}}";
            Configuration.CurrentConfiguration = Configuration.ConfigurationFromString(configString);
            RunResetRNGFactory();

            var modelInfo = EmodlLoader.LoadEMODLFile("resources\\rever_isom.emodl");
            const double duration = 10.0;
            const int repeats = 100000;
            const int samples = 1;
            var solver = new sdwSSA(modelInfo, duration, repeats, samples);
            solver.Initialize();

            var startRealization = GetHiddenMethod("StartRealization", solver);
            startRealization.Invoke(solver, null);

            GetHiddenMethod("RunCrossEntropy", solver).Invoke(solver, null);
            var biasingParameters = GetHiddenField<BiasingParameters>("_biasingParameters", solver);

            Console.Error.WriteLine("** TestRunCrossEntropy gammas **");
            foreach (double value in biasingParameters.Locales[0].Reactions[0].RareEvents[0].Gammas)
            {
                Console.Error.WriteLine(value);
            }

            Assert.AreEqual(new[] { 2.0273575170009774, 1.6011280886463515, 1.2909305036960836, 1.1580792074159503, 1.1014233907817004, 1.0495114005076731, 1.0308762479433711, 1.01949884022247545, 1.0123811789981323, 1.013972793156378, 1.0147858262235918, 0.97835339270352839, 1.025712153478078 },
                            biasingParameters.Locales[0].Reactions[0].RareEvents[0].Gammas);
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
                Assert.That(ex.GetBaseException().ToString(), Is.StringContaining("sdwSSA doesn't use StepOnce()"));
            }
        }

        [Test]
        public void TestStepOnceWithWeight()
        {
            const string configString = @"{""solver"":""SDWSSA"",""prng_seed"":123, ""prng_index"":1,""RNG"":{""type"":""RANDLIB""},""sdwSSA"":{""biasingParametersFileName"":""resources\\rever_isom_CEinfo.json""}}";
            Configuration.CurrentConfiguration = Configuration.ConfigurationFromString(configString);
            RunResetRNGFactory();

            var modelInfo = EmodlLoader.LoadEMODLFile("resources\\rever_isom.emodl");
            const double duration = 10.0;
            const int repeats = 100000;
            const int samples = 1;
            var solver = new sdwSSA(modelInfo, duration, repeats, samples);
            solver.Initialize();
            
            var startRealization = GetHiddenMethod("StartRealization", solver);
            startRealization.Invoke(solver, null);

            var stepOnce = solver.GetType().GetMethod("StepOnce", BindingFlags.NonPublic | BindingFlags.Instance, Type.DefaultBinder, new[] { typeof(double).MakeByRefType() }, null);
            var inputArray1 = new object[] { 1.5 };
            stepOnce.Invoke(solver, inputArray1);

            var currentTime = GetHiddenField<double>("_currentTime", solver);
            Assert.AreEqual(0.0075434142087234968, currentTime);
        }

        [Test]
        public void TestSolveOnce()
        {
            var solver = InstantiateSolver();
            solver.Initialize();

            var method = solver.GetType().GetMethod("SolveOnce", BindingFlags.NonPublic | BindingFlags.Instance, Type.DefaultBinder, new Type[0], null);
            method.Invoke(solver, null);

            var trajectoryCounter = GetHiddenField<int>("_trajectoryCounter", solver);
            var runningMean = GetHiddenField<double>("_runningMean", solver);
            var currentTime = GetHiddenField<double>("_currentTime", solver);

            Assert.AreEqual(1, trajectoryCounter);
            Assert.AreEqual(0.0, runningMean);
            Assert.GreaterOrEqual(currentTime, 10.0);
        }

        [Test]
        public void TestSolve()
        {
            const string configString = @"{""solver"":""SDWSSA"",""prng_seed"":123, ""prng_index"":1,""RNG"":{""type"":""RANDLIB""},""sdwSSA"":{""biasingParametersFileName"":""resources\\rever_isom_CEinfo.json""}}";
            Configuration.CurrentConfiguration = Configuration.ConfigurationFromString(configString);
            RunResetRNGFactory();

            var modelInfo = EmodlLoader.LoadEMODLFile("resources\\rever_isom.emodl");
            const double duration = 10.0;
            const int repeats = 100000;
            const int samples = 1;
            var solver = new sdwSSA(modelInfo, duration, repeats, samples);
            solver.Solve();

            var trajectoryCounter = GetHiddenField<int>("_trajectoryCounter", solver);
            var runningMean = GetHiddenField<double>("_runningMean", solver);
            var runningVariance = GetHiddenField<double>("_runningVariance", solver);
           Assert.AreEqual(100000, trajectoryCounter);
           Assert.AreEqual(0.000011777283310510327, runningMean);
           Assert.AreEqual(0.00017388263350241653, runningVariance);
        }

        [Test]
        public void TestToString()
        {
            var solver = InstantiateSolver();
            Assert.AreEqual("sdwSSA", solver.ToString());
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

        private static void CreateGammaPC(int gammaSize, double[] rates, out List<double[]> gamma, out List<double[]> pc)
        {
            gamma = new List<double[]>();
            pc = new List<double[]>();

            for (int i = 0; i < rates.Count(); i++)
            {
                gamma.Add(Enumerable.Repeat(rates[i], gammaSize).ToArray());
                pc.Add(new double[gammaSize - 1]);
                for (int j = 0; j < gammaSize - 1; j++)
                    pc[i][j] = 1.0 / (gammaSize) * (j + 1);
            }
        }

        private static void RunResetRNGFactory()
        {
            var methodInfo = typeof(RNGFactory).GetMethod("Reset", BindingFlags.NonPublic | BindingFlags.Static);
            methodInfo.Invoke(null, null);
        }

        private static sdwSSA InstantiateSolver()
        {
            const string configString = @"{""solver"":""SDWSSA"",""prng_seed"":123, ""prng_index"":1,""RNG"":{""type"":""RANDLIB""}}";
            Configuration.CurrentConfiguration = Configuration.ConfigurationFromString(configString);
            RunResetRNGFactory();

            var modelInfo = EmodlLoader.LoadEMODLFile("resources\\rever_isom.emodl");
            var solver = new sdwSSA(modelInfo, 10.0, 1000000, 1);

            return solver;
        }
    }
}