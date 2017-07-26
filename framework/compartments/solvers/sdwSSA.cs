using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using compartments.emod;
using compartments.emod.expressions;
using compartments.emod.interfaces;
using compartments.emod.utils;
using compartments.solvers.solverbase;
// ReSharper disable InconsistentNaming

namespace compartments.solvers
{
    /// <summary>
    /// Data structure used to determine biasing parameters during state-dependent multilevel cross entropy method
    /// </summary>
    public struct StateDependentGammaInfo
    {
        private List<double[]> _intermediateGamma;
        private List<double[]> _intermediatePropensityCutoff;
        private readonly List<double[]> _previousIntermediatePropensityCutoff;
        private readonly List<double[]> _gammaNum;
        private readonly List<double[]> _gammaDenom;
        private readonly List<int[]> _nSum;
        private readonly int _numReactions;
        private readonly int _gammaSize;
        private readonly int _binCountThreshold;

        public StateDependentGammaInfo(int numReactions, int gammaSize, int binCountThreshold)
        {
            _numReactions = numReactions;
            _gammaSize = gammaSize;
            _binCountThreshold = binCountThreshold;

            _intermediateGamma = new List<double[]>();
            _intermediatePropensityCutoff = new List<double[]>();
            _previousIntermediatePropensityCutoff = new List<double[]>();
            _gammaDenom = new List<double[]>();
            _gammaNum = new List<double[]>();
            _nSum = new List<int[]>();

            for (int i = 0; i < _numReactions; i++)
            {
                _intermediateGamma.Add(Enumerable.Repeat(1.0, _gammaSize).ToArray());
                _intermediatePropensityCutoff.Add(new double[_gammaSize - 1]);
                _previousIntermediatePropensityCutoff.Add(new double[_gammaSize - 1]);
                _gammaNum.Add(new double[_gammaSize]);
                _gammaDenom.Add(new double[_gammaSize]);
                _nSum.Add(new int[_gammaSize]);

                for (int j = 0; j < _gammaSize - 1; j++)
                {
                    _intermediatePropensityCutoff[i][j] = 1.0 / (_gammaSize) * (j + 1);
                }
            }
        }

        public List<double[]> IntermediateGamma => _intermediateGamma;

        public List<double[]> IntermediatePropensityCutoff => _intermediatePropensityCutoff;

        public List<double[]> PreviousIntermediatePropensityCutoff => _previousIntermediatePropensityCutoff;

        public void SetIntermediateGamma()
        {
            for (int i = 0; i < _numReactions; i++)
            {
                int newGammaLength = _gammaNum[i].Length;
                _intermediateGamma[i] = new double[newGammaLength];

                for (int j = 0; j < newGammaLength; j++)
                    _intermediateGamma[i][j] = _gammaNum[i][j] / _gammaDenom[i][j] > double.Epsilon ? _gammaNum[i][j] / _gammaDenom[i][j] : double.Epsilon;
            }
        }

        public void UpdateGamma(double weight, List<int[]> n, List<double[]> lambda)
        {
            for (int i = 0; i < _numReactions; i++)
            {
                for (int j = 0; j < _gammaNum[i].Length; j++)
                {
                    _gammaNum[i][j] += weight * n[i][j];
                    _gammaDenom[i][j] += weight * lambda[i][j];
                    _nSum[i][j] += n[i][j];
                }
            }
        }

        public void UpdatePropensityCutoff(double[] startPC, double[] endPC)
        {
            double[] tempPropensityCutoff = { 1.0 };

            for (int i = 0; i < _numReactions; i++)
            {
                _previousIntermediatePropensityCutoff[i] = _intermediatePropensityCutoff[i];
                _intermediatePropensityCutoff[i] = new double[_gammaSize - 1];
                double binWidth = (endPC[i] - startPC[i]) / _gammaSize;

                if (binWidth > double.Epsilon)
                {
                    for (int j = 0; j < _gammaSize - 1; j++)
                    {
                        _intermediatePropensityCutoff[i][j] = startPC[i] + binWidth * (j + 1);
                    }
                }
                else
                {
                    _intermediatePropensityCutoff[i] = tempPropensityCutoff;
                }
            }
        }

        public void UpdateStructure()
        {
            for (int i = 0; i < _numReactions; i++)
            {
                var pcLength = (Math.Abs(1.0 - _intermediatePropensityCutoff[i][0]) > double.Epsilon) ? _intermediatePropensityCutoff[i].Length : 0;
                _gammaNum[i] = new double[pcLength + 1];
                _gammaDenom[i] = new double[pcLength + 1];
                _nSum[i] = new int[pcLength + 1];
            }
        }

        public void MergeBins()
        {
            for (int i = 0; i < _numReactions; i++)
            {
                _previousIntermediatePropensityCutoff[i] = _intermediatePropensityCutoff[i];
            }

            _intermediatePropensityCutoff = new List<double[]>();
            _intermediateGamma = new List<double[]>();

            for (int i = 0; i < _numReactions; i++)
            {
                if (_nSum[i].Sum() < _binCountThreshold)
                {
                    double[] tempGamma = { _gammaNum[i].Sum() / _gammaDenom[i].Sum() };
                    tempGamma = tempGamma[0] > double.Epsilon ? tempGamma : new[] { double.Epsilon };
                    double[] tempPropensityCutoff = { 1.0 };
                    _intermediateGamma.Add(tempGamma);
                    _intermediatePropensityCutoff.Add(tempPropensityCutoff);
                    continue;
                }

                var mergePair = new List<int[]>();
                int currentIndex = 0;
                int mergePairIndex = 0;
                int startPairIndex = 0;
                int niLength = _nSum[i].Length;
                int leftTotal = _nSum[i].Max() + 1;

                while (currentIndex < niLength)
                {
                    int currentTotal = _nSum[i][currentIndex];

                    if (currentTotal >= _binCountThreshold)
                    {
                        mergePair.Add(new[] { currentIndex, currentIndex });
                        mergePairIndex++;
                        currentIndex++;
                        startPairIndex++;
                        leftTotal = currentTotal;
                    }
                    else
                    {
                        while (currentTotal < _binCountThreshold)
                        {
                            if (currentIndex == (niLength) - 1)
                            {
                                mergePair[mergePairIndex - 1][1] = currentIndex;
                                currentIndex++;
                                break;
                            }

                            if (leftTotal < _nSum[i][currentIndex + 1])
                            {
                                leftTotal += currentTotal;
                                mergePair[mergePairIndex - 1][1] = currentIndex;
                                currentIndex++;
                                startPairIndex = currentIndex;
                                break;
                            }

                            currentIndex++;
                            currentTotal += _nSum[i][currentIndex];

                            if (currentTotal < _binCountThreshold) continue;
                            mergePair.Add(new[] { startPairIndex, currentIndex });
                            currentIndex++;
                            mergePairIndex++;
                            startPairIndex = currentIndex;
                            leftTotal = currentTotal;
                            break;
                        }
                    }
                }

                int mpCount = mergePair.Count;
                var tempGammaj = new double[mpCount];

                for (int j = 0; j < mpCount; j++)
                {
                    int startInd = mergePair[j][0];
                    int endInd = mergePair[j][1];
                    double tempNumj = _gammaNum[i][startInd];
                    double tempDenomj = _gammaDenom[i][startInd];

                    for (int k = startInd + 1; k <= endInd; k++)
                    {
                        tempNumj += _gammaNum[i][k];
                        tempDenomj += _gammaDenom[i][k];
                    }

                    tempGammaj[j] = tempNumj / tempDenomj;
                }

                _intermediateGamma.Add(tempGammaj);
                double[] tempPropensityCutoffj;

                if (mpCount == 1)
                    tempPropensityCutoffj = new[] { 1.0 };
                else
                {
                    tempPropensityCutoffj = new double[mpCount - 1];

                    for (int j = 0; j < (mpCount - 1); j++)
                    {
                        tempPropensityCutoffj[j] = _previousIntermediatePropensityCutoff[i][mergePair[j][1]];
                    }
                }

                _intermediatePropensityCutoff.Add(tempPropensityCutoffj);
            }
        }
    }

    /// <summary>
    /// sdwSSA reaction class
    /// sorting of reactions (in order they are read from a model file) is to be implemented 
    /// </summary>
    class StateDependentReactionSet
    {
        private readonly double[] _currentRates;
        private readonly double[] _predilectionRates;
        private readonly IList<Reaction> _reactions;
        private readonly int _numReactions;

        public StateDependentReactionSet(IList<Reaction> reactions, int gammaSize)
        {
            _reactions = reactions;
            GammaSize = gammaSize;
            _numReactions = reactions.Count;
            _currentRates = new double[_numReactions];
            _predilectionRates = new double[_numReactions];
        }

        public int GammaSize { get; }

        public int NumReactions => _numReactions;

        public double[] CurrentRates => _currentRates;

        public double[] PredilectionRates => _predilectionRates;

        public IList<Reaction> Reactions => _reactions;

        public void FireReaction(int mu)
        {
            foreach (Species s in _reactions[mu].Reactants)
                s.Decrement();

            foreach (Species s in _reactions[mu].Products)
                s.Increment();
        }

        public double UpdateRatesIteration1(List<double[]> gamma, List<double[]> propensityCutoff, List<double[]> previousPropensityCutoff, ref int[] gammaIndex, ref int[] binIndex)
        {
            int index = 0;
            double a0 = 0.0;
            double b0 = 0.0;

            foreach (Reaction r in _reactions)
            {
                double av = r.Rate;
                _currentRates[index++] = av;
                a0 += av;
            }

            for (int i = 0; i < _numReactions; i++)
            {
                if (gamma[i].Length == 1)
                    index = 0;
                else
                {
                    int pcjLength = propensityCutoff[i].Length;
                    index = -1;

                    for (int j = 0; j < pcjLength; j++)
                    {
                        if ((_currentRates[i] / a0) < propensityCutoff[i][j] && _currentRates[i] > 0)
                        {
                            index = j;
                            break;
                        }
                    }
                    if (index == -1)
                    {
                        index = pcjLength;
                    }
                }

                gammaIndex[i] = index;
                double bv = _currentRates[i] * gamma[i][index];
                PredilectionRates[i] = bv;
                b0 += bv;
            }

            return b0;
        }

        public double UpdateRatesIteration2(List<double[]> gamma, List<double[]> propensityCutoff, List<double[]> previousPropensityCutoff, ref int[] gammaIndex, ref int[] binIndex)
        {
            int bIndex = 0;
            double a0 = 0.0;
            double b0 = 0.0;

            foreach (Reaction r in _reactions)
            {
                double av = r.Rate;
                _currentRates[bIndex++] = av;
                a0 += av;
            }

            for (int i = 0; i < _numReactions; i++)
            {
                int pcjLength = propensityCutoff[i].Length;
                bIndex = -1;

                for (int j = 0; j < pcjLength; j++)
                {
                    if ((_currentRates[i] / a0) >= propensityCutoff[i][j] && _currentRates[i] > 0) continue;
                    bIndex = j;
                    break;
                }

                if (bIndex == -1)
                    bIndex = pcjLength;

                binIndex[i] = bIndex;
                int gIndex;

                if (gamma[i].Length == 1)
                {
                    gIndex = 0;
                }
                else
                {
                    int ppcjLength = previousPropensityCutoff[i].Length;
                    gIndex = -1;

                    for (int j = 0; j < ppcjLength; j++)
                    {
                        if ((_currentRates[i] / a0) >= previousPropensityCutoff[i][j]) continue;
                        gIndex = j;
                        break;
                    }

                    if (gIndex == -1)
                        gIndex = ppcjLength;
                }

                gammaIndex[i] = gIndex;
                double bv = _currentRates[i] * gamma[i][gIndex];
                PredilectionRates[i] = bv;
                b0 += bv;
            }

            return b0;
        }

        public int SelectReaction(double threshold)
        {
            int mu = 0;
            double cummulativeSum = 0.0;

            for (int i = 0; i < _numReactions; i++)
            {
                cummulativeSum += _predilectionRates[i];
                if ((threshold <= cummulativeSum) && (_predilectionRates[i] > 0.0f))
                {
                    mu = i;
                    break;
                }
            }

            return mu;
        }
    }

// ReSharper disable InconsistentNaming
    public class sdwSSA : SolverBase
// ReSharper restore InconsistentNaming
    {
        private int _crossEntropyRuns;
        private float _crossEntropyThreshold;
        private readonly int _binCountThreshold;
        private readonly int _crossEntropyMinDataSize;

        private readonly List<double[]> _gamma;
        private readonly int _gammaSize;
        private int[] _gammaIndex;
        private int[] _binIndex;
        private readonly List<double[]> _propensityCutoff;

        private readonly string _reExpressionName;
        private readonly string _reValName;
        private float _rareEventValue;
        private IBoolean _rareEventTest;
        private readonly Expression _reExpression;
        private int _rareEventType;
        private readonly StateDependentReactionSet _reactions;

        private int _trajectoryCounter;
        private double _runningMean;
        private double _runningVariance;

        private readonly BiasingParameters _biasingParameters;
        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly string _biasingParametersFileName; // ReSharper wants to make this local, but we'll leave it at this scope so it can be debugged.
        private readonly Boolean _biasingParametersFlag;

        public sdwSSA(ModelInfo modelinfo, float duration, int repeats, int samples)
            : base(modelinfo, duration, repeats, samples)
        {
            Configuration config = Configuration.CurrentConfiguration;
            _reactions = new StateDependentReactionSet(model.Reactions, _gammaSize);

            _trajectoryCounter = 0;
            _runningMean = 0.0;
            _runningVariance = 0.0;

            _crossEntropyRuns = config.GetParameterWithDefault("sdwSSA.crossEntropyRuns", 100000);
            _crossEntropyThreshold = config.GetParameterWithDefault("sdwSSA.crossEntropyThreshold", 0.01f);
            _crossEntropyMinDataSize = config.GetParameterWithDefault("sdwSSA.crossEntropyMinDataSize", 200);
            _reExpressionName = config.GetParameterWithDefault("sdwSSA.reExpressionName", "reExpression");
            _reValName = config.GetParameterWithDefault("sdwSSA.reValName", "reVal");
            _gammaSize = config.GetParameterWithDefault("sdwSSA.gammaSize", 15);
            _binCountThreshold = config.GetParameterWithDefault("sdwSSA.binCount", 20);

            _reExpression = model.Expressions.FirstOrDefault(e => e.Name == _reExpressionName);
            _rareEventValue = 0.0f;
            _rareEventTest = new EqualTo(_reExpression, new ConstantValue(_rareEventValue));

            _gamma = new List<double[]>();
            _propensityCutoff = new List<double[]>();
            _gammaIndex = new int[_reactions.NumReactions];
            _binIndex = new int[_reactions.NumReactions];

            _biasingParametersFileName = config.GetParameterWithDefault("sdwSSA.biasingParametersFileName", modelinfo.Name + "_biasingParameters.json");
            _biasingParametersFlag = (String.CompareOrdinal(_biasingParametersFileName, modelinfo.Name + "_biasingParameters.json") == 0);
            _biasingParameters = _biasingParametersFlag ? new BiasingParameters() : BiasingParametersDeserializer.ReadParametersFromJsonFile(_biasingParametersFileName);
        }

        public void Initialize()
        {
            CheckParameters();
            _rareEventValue = model.Parameters.First(p => p.Name == _reValName).Value;
            _rareEventTest = new EqualTo(_reExpression, new ConstantValue(_rareEventValue));
            SetRareEventType();

            if (_biasingParametersFlag)
            {
                for (int i = 0; i < _reactions.NumReactions; i++)
                {
                    _gamma.Add(new double[_gammaSize]);
                    _propensityCutoff.Add(new double[_gammaSize - 1]);
                }

                _biasingParameters.RareEvent.ExpressionLocale = "global";
                _biasingParameters.RareEvent.ExpressionName = _reExpressionName;

                foreach (var locale in modelInfo.Locales)
                {
                    var newLocaleInfo = new BiasingParameters.LocaleInfo { Name = locale.Name };
                    var newReactionInfo = newLocaleInfo.Reactions;
                    LocaleInfo testLocale = locale;
                    var reactions = modelInfo.Reactions.Where(r => r.Locale == testLocale);
                    newReactionInfo.AddRange(reactions.Select(reaction => new BiasingParameters.ReactionInfo { Name = reaction.Name }));
                    _biasingParameters.Locales.Add(newLocaleInfo);
                }
            }
            else
            {
                for (int i = 0; i < _reactions.NumReactions; i++)
                {
                    var localeIndex = LocaleIndex(_reactions.Reactions[i]);
                    var reactionIndex = ReactionIndex(_reactions.Reactions[i], localeIndex);
                    var tempRareEvent = _biasingParameters.Locales[localeIndex].Reactions[reactionIndex].RareEvents[_biasingParameters.RareEvent.IntermediateRareEventCount - 1];
                    _gamma.Add(tempRareEvent.Gammas);
                    _propensityCutoff.Add(tempRareEvent.Thresholds);
                }
            }
        }

        public override void Solve()
        {
            Initialize();

            if (_biasingParametersFlag)
            {
                Console.WriteLine("Biasing parameters not found. Starting multilevel cross entropy simulation with " + _crossEntropyRuns + " realizations...");
                RunCrossEntropy();
            }
            Console.WriteLine("\n\nStarting sdwSSA simulations with the following biasing parameters:");

            // how can index better format these messages?
            for (int i = 0; i < _reactions.NumReactions; i++)
            {
                Console.WriteLine("   Gamma {0}: ", (i + 1));
                for (int j = 0; j < _gamma[i].Length; j++)
                    Console.Write(" {0}", _gamma[i][j]);
                Console.WriteLine("\n");

                Console.WriteLine("   Propensity Cutoff {0}: ", (i + 1));
                for (int j = 0; j < _propensityCutoff[i].Length; j++)
                    Console.Write(" {0}", _propensityCutoff[i][j]);
                Console.WriteLine("\n");
            }

            base.Solve();
        }

        protected delegate double ReactionRatesUpdateMethod(List<double[]> gamma, List<double[]> propensityCutoff, List<double[]> previousPropensityCutoff, ref int[] gammaIndex, ref int[] binIndex);

        protected delegate void BinEdgesUpdateMethod(ref double[] startPC, ref double[] endPC, double a0, int mu);

        private void CheckParameters()
        {
            if (_crossEntropyRuns < 5000)
                throw new ApplicationException("crossEntropyRuns must be greater than 5000 (default value = 100,000).");

            if (_crossEntropyThreshold > 1.0f || _crossEntropyThreshold < 0.0f)
                throw new ApplicationException("crossEntropyThreshold must be between 0 and 1 (default is 0.01).");

            if (_crossEntropyMinDataSize < 100)
                throw new ApplicationException("crossEntropyMinDataSize must be an integer >= 100.");

            if (_gammaSize < 1)
                throw new ApplicationException("gammaSize must be a positive integer greater than 1. Use dwSSA for gammaSize = 1.");

            if (_binCountThreshold < 10)
                throw new ApplicationException("binCount must be an integer >= 10.");

            if (_reExpression == null)
                throw new ApplicationException("rare event expression field is missing.");

            if (model.Parameters.FirstOrDefault(p => p.Name == _reValName) == null)
                throw new ApplicationException("rare event value name field is missing.");
        }

        private void SetRareEventType()
        {
            ResetModelState();
            _rareEventType = _reExpression.Value < _rareEventValue ? 1 : -1;
        }

        protected override float CalculateProposedTau(float tauLimit)
        {
            throw new ApplicationException("sdwSSA doesn't use CalculateProposedTau().");
        }

        protected override void ExecuteReactions()
        {
            throw new ApplicationException("sdwSSA doesn't use ExecuteReactions().");
        }

        protected override void SolveOnce()
        {
            double weight = 1.0;
            double delta;
            bool weightFlag = true;

            while (CurrentTime < duration)
            {
                if (_rareEventTest.Value)
                {
                    _trajectoryCounter++;
                    delta = weight - _runningMean;
                    _runningMean += delta / _trajectoryCounter;
                    _runningVariance += delta * (weight - _runningMean);
                    weightFlag = false;
                    break;
                }

                StepOnce(ref weight);
            }

            if (weightFlag)
            {
                _trajectoryCounter++;
                delta = -_runningMean;
                _runningMean += delta / _trajectoryCounter;
                _runningVariance += delta * -_runningMean;
            }
        }

        private int SelectAndFireReaction(double b0)
        {
            float r2 = rng.GenerateUniformOO();
            double threshhold = r2 * b0;
            int mu = _reactions.SelectReaction(threshhold);
            _reactions.FireReaction(mu);

            return mu;
        }

        private void UpdateBinEdges(ref double[] startPC, ref double[] endPC, double a0, int mu)
        {
            double relPropensity = _reactions.CurrentRates[mu] / a0;

            if (relPropensity < startPC[mu])
                startPC[mu] = relPropensity;
            if (relPropensity > endPC[mu])
                endPC[mu] = relPropensity;
        }

        private void AccumulateBiasingParameters(ref List<int[]> tempN, ref List<double[]> tempLambda, double tau, int mu, int[] indexArray)
        {
            tempN[mu][indexArray[mu]]++;

            for (int i = 0; i < _reactions.NumReactions; i++)
            {
                tempLambda[i][indexArray[i]] += _reactions.CurrentRates[i] * tau;
            }
        }

        protected override void StepOnce()
        {
            throw new ApplicationException("sdwSSA doesn't use StepOnce().");
        }

        protected virtual void StepOnce(ref double weight)
        {
            double b0 = _reactions.UpdateRatesIteration1(_gamma, _propensityCutoff, null, ref _gammaIndex, ref _binIndex);
            double a0 = _reactions.CurrentRates.Sum();

            if (b0 > 0.0)
            {
                float r = rng.GenerateUniformOO();
                double tau = Math.Log(1.0 / r) / b0;
                CurrentTime += (float)tau;

                if (CurrentTime < duration)
                {
                    int mu = SelectAndFireReaction(b0);
                    weight *= Math.Exp((b0 - a0) * tau) / _gamma[mu][_gammaIndex[mu]];
                }
            }
            else
            {
                CurrentTime = duration;
            }
        }

        protected virtual void GenericStepOnce(ref double weight, StateDependentGammaInfo tempGammaInfo, ref double[] startPC, ref double[] endPC, ref List<int[]> tempN, ref List<double[]> tempLambda, ReactionRatesUpdateMethod reactionRateUpdateMethod, ref int[] indexArray, BinEdgesUpdateMethod binEdgesUpdateMethod)
        {
            double b0 = reactionRateUpdateMethod(tempGammaInfo.IntermediateGamma, tempGammaInfo.IntermediatePropensityCutoff, tempGammaInfo.PreviousIntermediatePropensityCutoff, ref _gammaIndex, ref _binIndex);
            double a0 = _reactions.CurrentRates.Sum();

            if (b0 > 0.0)
            {
                float r = rng.GenerateUniformOO();
                double tau = Math.Log(1.0 / r) / b0;
                CurrentTime += (float)tau;

                if (CurrentTime < duration)
                {
                    int mu = SelectAndFireReaction(b0);
                    AccumulateBiasingParameters(ref tempN, ref tempLambda, tau, mu, indexArray);

                    binEdgesUpdateMethod?.Invoke(ref startPC, ref endPC, a0, mu);

                    weight *= Math.Exp((b0 - a0) * tau) / tempGammaInfo.IntermediateGamma[mu][_gammaIndex[mu]];
                }
            }
            else
            {
                CurrentTime = duration;
            }
        }

        private void UpdateBiasingParameters(StateDependentGammaInfo gammaInfo, double intermediateRareEvent)
        {
            _biasingParameters.RareEvent.Thresholds.Add(intermediateRareEvent);

            for (int i = 0; i < _reactions.Reactions.Count; i++)
            {
                var tempRareEventInfo = new BiasingParameters.RareEventInfo { BinCount = gammaInfo.IntermediateGamma[i].Count() };
                gammaInfo.IntermediateGamma[i].CopyTo(tempRareEventInfo.Gammas, 0);
                gammaInfo.IntermediatePropensityCutoff[i].CopyTo(tempRareEventInfo.Thresholds, 0);
                UpdateReactionInfo(tempRareEventInfo, i);
            }
        }

        private void UpdateReactionInfo(BiasingParameters.RareEventInfo tempRareEventInfo, int index)
        {
            var tempReaction = _reactions.Reactions[index];
            var localeIndex = LocaleIndex(tempReaction);
            var reactionIndex = ReactionIndex(tempReaction, localeIndex);

            _biasingParameters.Locales[localeIndex].Reactions[reactionIndex].RareEvents.Add(tempRareEventInfo);
        }

        private int ReactionIndex(Reaction tempReaction, int localeIndex)
        {
            var reactionIndex = _biasingParameters.Locales[localeIndex].Reactions.FindIndex(r => r.Name == tempReaction.Name);

            return reactionIndex;
        }

        private int LocaleIndex(Reaction tempReaction)
        {
            var tempLocaleInfo = _biasingParameters.Locales.First(l => l.Name == tempReaction.Info.Locale.Name);
            var localeIndex = _biasingParameters.Locales.FindIndex(l => l == tempLocaleInfo);

            return localeIndex;
        }

        private void RunCrossEntropy()
        {
            int iter = 1;
            var gammaInfo = new StateDependentGammaInfo(_reactions.NumReactions, _gammaSize, _binCountThreshold);
            float intermediateRareEvent = _rareEventType == 1 ? 0 : _reExpression.Value;

            while (Math.Abs(intermediateRareEvent - _rareEventValue) > double.Epsilon && iter < 15)
            {
                Console.WriteLine("\nCross Entropy iteration " + iter + ":");

                var startPC = Enumerable.Repeat(1.0, _reactions.NumReactions).ToArray();
                var endPC = new double[_reactions.NumReactions];
                bool rareEventFlag = false;
                gammaInfo.UpdateStructure();

                CrossEntropy1(ref gammaInfo, ref intermediateRareEvent, ref startPC, ref endPC, ref rareEventFlag);
                Console.WriteLine("   Intermediate rare event: " + intermediateRareEvent);

                if (rareEventFlag)
                {
                    Console.WriteLine("\nReached the rare event. Exiting multilevel cross entropy simulation...");
                    UpdateBiasingParameters(gammaInfo, intermediateRareEvent);
                    break;
                }

                gammaInfo.UpdatePropensityCutoff(startPC, endPC);
                gammaInfo.UpdateStructure();

                CrossEntropy2(ref gammaInfo, intermediateRareEvent);
                gammaInfo.MergeBins();
                UpdateBiasingParameters(gammaInfo, intermediateRareEvent);
                iter++;
            }

            _biasingParameters.WriteParametersToJsonFile(modelInfo.Name + "_sdwSSA_CEinfo.json");

            if (iter == 15)
            {
                throw new ApplicationException("multilevel cross entropy did not converged in 15 iterations.");
            }

            for (int i = 0; i < _reactions.NumReactions; i++)
            {
                _gamma[i] = gammaInfo.IntermediateGamma[i];
                _propensityCutoff[i] = gammaInfo.IntermediatePropensityCutoff[i];
            }
        }

        private void CrossEntropy1(ref StateDependentGammaInfo gammaInfo, ref float intermediateRareEvent, ref double[] startPC, ref double[] endPC, ref bool rareEventFlag)
        {
            var maxRareEventValue = new float[_crossEntropyRuns];
            int counter = 0;

            var rateUpdateMethod = new ReactionRatesUpdateMethod(_reactions.UpdateRatesIteration1);
            var binEdgesUpdateMethod = new BinEdgesUpdateMethod(UpdateBinEdges);

            for (int i = 0; i < _crossEntropyRuns; i++)
            {
                StartRealization();
                float currentMin = _rareEventType * (_rareEventValue - _reExpression.Value);
                var n = new List<int[]>();
                var lambda = new List<double[]>();
                double weight = 1.0;

                for (int j = 0; j < _reactions.NumReactions; j++)
                {
                    n.Add(new int[(gammaInfo.IntermediatePropensityCutoff[j]).Length + 1]);
                    lambda.Add(new double[(gammaInfo.IntermediatePropensityCutoff[j]).Length + 1]);
                }

                while (CurrentTime < duration)
                {
                    if (_rareEventTest.Value)
                    {
                        counter++;
                        gammaInfo.UpdateGamma(weight, n, lambda);
                        break;
                    }

                    GenericStepOnce(ref weight, gammaInfo, ref startPC, ref endPC, ref n, ref lambda, rateUpdateMethod, ref _gammaIndex, binEdgesUpdateMethod);
                    float tempMin = _rareEventType * (_rareEventValue - _reExpression.Value);
                    currentMin = Math.Min(currentMin, tempMin);
                }

                maxRareEventValue[i] = currentMin;
            }

            Array.Sort(maxRareEventValue);
            float ireComp = maxRareEventValue[(int)Math.Ceiling(_crossEntropyRuns * _crossEntropyThreshold)];
            float pastIntermediateRareEvent = intermediateRareEvent;
            intermediateRareEvent = ireComp < 0 ? _rareEventValue : _rareEventType * (_rareEventValue - ireComp);

            if ((pastIntermediateRareEvent - intermediateRareEvent) * _rareEventType >= 0)
            {
                _crossEntropyThreshold *= 0.8f;
                Console.WriteLine("Cross entropy threshold changed to : " + _crossEntropyThreshold);

                if (_crossEntropyThreshold * _crossEntropyRuns * 0.8 < _crossEntropyMinDataSize)
                {
                    _crossEntropyRuns = (int)Math.Ceiling(_crossEntropyMinDataSize / _crossEntropyThreshold);
                    Console.WriteLine("Number of cross entropy simulations changed to : " + _crossEntropyRuns);
                }
            }

            if (intermediateRareEvent >= _rareEventValue && counter >= (int)(_crossEntropyRuns * _crossEntropyThreshold))
            {
                rareEventFlag = true;
                gammaInfo.SetIntermediateGamma();
            }
        }

        private void CrossEntropy2(ref StateDependentGammaInfo gammaInfo, float tempRareEvent)
        {
            IBoolean tempRareEventExpression = new EqualTo(_reExpression, new ConstantValue(tempRareEvent));

            var startPC = Enumerable.Repeat(1.0, _reactions.NumReactions).ToArray();
            var endPC = new double[_reactions.NumReactions];
            var rateUpdateMethod = new ReactionRatesUpdateMethod(_reactions.UpdateRatesIteration2);

            for (int i = 0; i < _crossEntropyRuns; i++)
            {
                StartRealization();
                var n = new List<int[]>();
                var lambda = new List<double[]>();
                double weight = 1.0;

                for (int j = 0; j < _reactions.NumReactions; j++)
                {
                    n.Add(new int[(gammaInfo.IntermediatePropensityCutoff[j]).Length + 1]);
                    lambda.Add(new double[(gammaInfo.IntermediatePropensityCutoff[j]).Length + 1]);
                }

                while (CurrentTime < duration)
                {
                    if (tempRareEventExpression.Value)
                    {
                        gammaInfo.UpdateGamma(weight, n, lambda);
                        break;
                    }

                    GenericStepOnce(ref weight, gammaInfo, ref startPC, ref endPC, ref n, ref lambda, rateUpdateMethod, ref _binIndex, null);
                }
            }

            gammaInfo.SetIntermediateGamma();
        }

        public override void OutputData(string prefix)
        {
            double reUnc = Math.Sqrt(_runningVariance / _trajectoryCounter) / Math.Sqrt(_trajectoryCounter);
            double reVar = _runningVariance / _trajectoryCounter;

            string filename = modelInfo.Name + "_sdwSSA.txt";
            using (var output = new StreamWriter(filename))
            {
                output.Write("FrameworkVersion,\"");
                output.Write(VersionInfo.Version);
                output.Write("\",\"");
                output.Write(VersionInfo.Description);
                output.WriteLine("\"");

                output.WriteLine("No. of realizations: {0}", SamplingParams.RealizationCount);
                output.WriteLine("Rare event probability estimate: {0} ", _runningMean);
                output.WriteLine("68% uncertainty: {0}", reUnc);
                output.WriteLine("Sample variance: {0}", reVar);
            }
        }

        public override string ToString()
        {
            return "sdwSSA";
        }
    }
}