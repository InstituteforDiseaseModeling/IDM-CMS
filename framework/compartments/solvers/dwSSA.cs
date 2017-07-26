using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using compartments.emod;
using compartments.emod.expressions;
using compartments.emod.interfaces;
using compartments.emod.utils;
using compartments.solvers.solverbase;

namespace compartments.solvers
{
    /// <summary>
    /// Data structure used to determine biasing parameters during multilevel cross entropy method
    /// </summary>
    public struct GammaInfo
    {
        private readonly double[] _intermediateGamma;
        private double[] _gammaNum;
        private double[] _gammaDenom;
        private readonly int _numReactions;

        public GammaInfo(int numberOfReactions)
            : this()
        {
            _numReactions = numberOfReactions;
            _intermediateGamma = Enumerable.Repeat(1.0, _numReactions).ToArray();
            _gammaNum = new double[_numReactions];
            _gammaDenom = new double[_numReactions];
            IntermediateRareEvent = -1.0f;
        }

        public double[] IntermediateGamma
        {
            get { return _intermediateGamma; }
            set
            {
                for (int i = 0; i < _numReactions; i++)
                {
                    if (value[i] > double.Epsilon)
                        _intermediateGamma[i] = value[0];
                    else
                        _intermediateGamma[i] = double.Epsilon;
                }
            }
        }

        public float IntermediateRareEvent { get; set; }

        public void SetIntermediateGamma()
        {
            for (int i = 0; i < _numReactions; i++)
                _intermediateGamma[i] = (_gammaNum[i] / _gammaDenom[i] > double.Epsilon) ? _gammaNum[i] / _gammaDenom[i] : double.Epsilon;
        }

        public void UpdateGamma(double weight, int[] n, double[] lambda)
        {
            for (int i = 0; i < _numReactions; i++)
            {
                _gammaNum[i] += weight * n[i];
                _gammaDenom[i] += weight * lambda[i];
            }
        }

        public void UpdateStructure()
        {
            _gammaDenom = new double[_numReactions];
            _gammaNum = new double[_numReactions];
        }
    }

    /// <summary>
    /// dwSSA reaction class
    /// sorting of reactions (in order they are read from a model file) is to be implemented
    /// </summary>
    internal class ReactionSet
    {
        private readonly double[] _currentRates;
        private readonly double[] _predilectionRates;
        private readonly IList<Reaction> _reactions;
        private readonly int _numReactions;

        public ReactionSet(IList<Reaction> reactions)
        {
            _reactions = reactions;
            _numReactions = reactions.Count;
            _currentRates = new double[_numReactions];
            _predilectionRates = new double[_numReactions];
        }

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

        public double UpdateRates(double[] gamma)
        {
            double b0 = 0.0;
            int index = 0;

            foreach (Reaction r in _reactions)
            {
                double av = r.Rate;
                double bv = av * gamma[index];
                _currentRates[index] = av;
                _predilectionRates[index++] = bv;
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

    // ReSharper disable once InconsistentNaming
    public class dwSSA : SolverBase
    {
        private int _crossEntropyRuns;
        private float _crossEntropyThreshold;
        private readonly int _crossEntropyMinDataSize;

        private readonly double[] _gamma;
        private readonly string _reExpressionName;
        private readonly string _reValName;
        private float _rareEventValue;
        private IBoolean _rareEventTest;
        private readonly Expression _reExpression;
        private int _rareEventType;
        private readonly ReactionSet _reactions;

        private int _trajectoryCounter;
        private double _runningMean;
        private double _runningVariance;

        // file for writing dwSSA results
        private readonly string _outputFileName;

        public dwSSA(ModelInfo modelInfo, float duration, int repeats, int samples)
            : base(modelInfo, duration, repeats, samples)
        {
            Configuration config = Configuration.CurrentConfiguration;
            _reactions = new ReactionSet(model.Reactions);

            _trajectoryCounter = 0;
            _runningMean = 0.0;
            _runningVariance = 0.0;

            _crossEntropyRuns = config.GetParameterWithDefault("dwSSA.crossEntropyRuns", 100000);
            _crossEntropyThreshold = config.GetParameterWithDefault("dwSSA.crossEntropyThreshold", 0.01f);
            _crossEntropyMinDataSize = config.GetParameterWithDefault("dwSSA.crossEntropyMinDataSize", 200);
            _reExpressionName = config.GetParameterWithDefault("dwSSA.reExpressionName", "reExpression");
            _reValName = config.GetParameterWithDefault("dwSSA.reValName", "reVal");

            _reExpression = model.Expressions.FirstOrDefault(e => e.Name == _reExpressionName);
            _rareEventValue = 0.0f;
            _rareEventTest = new EqualTo(_reExpression, new ConstantValue(_rareEventValue));

            _gamma = config.GetParameterWithDefault("dwSSA.gamma", new double[_reactions.NumReactions]);
            _outputFileName = config.GetParameterWithDefault("dwSSA.outputFileName", modelInfo.Name + "_dwSSA_1e" + Math.Log(SamplingParams.RealizationCount, 10) + ".txt");
        }

        public void Initialize()
        {
            CheckParameters();
            _rareEventValue = model.Parameters.First(p => p.Name == _reValName).Value;
            _rareEventTest = new EqualTo(_reExpression, new ConstantValue(_rareEventValue));
            SetRareEventType();

            if (!Configuration.CurrentConfiguration.HasParameter("dwSSA.gamma"))
            {
                Console.WriteLine("Biasing parameters not found. Starting multilevel cross entropy simulation with " + _crossEntropyRuns + " realizations...");
                RunCrossEntropy();
            }
        }

        public override void Solve()
        {
            Initialize();

            Console.WriteLine("\n\nStarting dwSSA simulations with the following biasing parameters:\n");

            for (int i = 0; i < _reactions.NumReactions; i++)
                Console.WriteLine("   Gamma {0}: {1}", (i + 1), _gamma[i]);

            base.Solve();
        }

        private void CheckParameters()
        {
            if (Configuration.CurrentConfiguration.HasParameter("dwSSA.gamma") && _gamma.Any(bias => bias <= 0))
                throw new ApplicationException("Biasing parameter must be a positive real.");

            if (_crossEntropyRuns < 5000)
                throw new ApplicationException("crossEntropyRuns must be greater than 5000 (default value = 100,000).");

            if (_crossEntropyThreshold > 1.0f || _crossEntropyThreshold < 0.0f)
                throw new ApplicationException("crossEntropyThreshold must be between 0 and 1 (default is 0.01).");

            if (_crossEntropyMinDataSize < 100)
                throw new ApplicationException("crossEntropyMinDataSize must be greater than 100.");

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
            throw new ApplicationException("dwSSA doesn't use CalculateProposedTau().");
        }

        protected override void ExecuteReactions()
        {
            throw new ApplicationException("dwSSA doesn't use ExecuteReactions().");
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

        protected override void StepOnce()
        {
            throw new ApplicationException("dwSSA doesn't use StepOnce().");
        }

        protected virtual void StepOnce(ref double weight)
        {
            double b0 = _reactions.UpdateRates(_gamma);
            double a0 = _reactions.CurrentRates.Sum();

            if (b0 > 0.0)
            {
                float r = rng.GenerateUniformOO();
                double tau = Math.Log(1.0 / r) / b0;
                CurrentTime += (float)tau;

                if (CurrentTime < duration)
                {
                    int mu = SelectAndFireReaction(b0);
                    weight *= Math.Exp((b0 - a0) * tau) / _gamma[mu];
                }
            }
            else
                CurrentTime = duration;
        }

        protected virtual void StepOnce(ref double weight, double[] tempGamma, ref int[] tempN, ref double[] tempLambda)
        {
            double b0 = _reactions.UpdateRates(tempGamma);
            double a0 = _reactions.CurrentRates.Sum();

            if (b0 > 0.0)
            {
                float r = rng.GenerateUniformOO();
                double tau = Math.Log(1.0 / r) / b0;
                CurrentTime += (float)tau;

                if (CurrentTime < duration)
                {
                    int mu = SelectAndFireReaction(b0);
                    tempN[mu]++;
                    for (int i = 0; i < _reactions.Reactions.Count; i++)
                        tempLambda[i] += _reactions.CurrentRates[i] * tau;

                    weight *= Math.Exp((b0 - a0) * tau) / tempGamma[mu];
                }
            }
            else
            {
                CurrentTime = duration;
            }
        }

        private void RunCrossEntropy()
        {
            int iter = 1;
            float intermediateRareEvent = _rareEventType == 1 ? 0.0f : _reExpression.Value;
            var gammaInfo = new GammaInfo(_reactions.NumReactions) {IntermediateRareEvent = intermediateRareEvent};

            using (StreamWriter output = File.AppendText(modelInfo.Name + "_dwSSA_CEinfo.txt"))
            {
                while (Math.Abs(intermediateRareEvent - _rareEventValue) > float.Epsilon)
                {
                    Console.WriteLine("\nCross Entropy iteration " + iter + ":");
                    output.WriteLine("\nCross Entropy iteration " + iter + ":");
                    output.Flush();

                    gammaInfo = CrossEntropy1(gammaInfo);
                    bool rareEventFlag = (Math.Abs(gammaInfo.IntermediateRareEvent - _rareEventValue) <= float.Epsilon);
                    intermediateRareEvent = gammaInfo.IntermediateRareEvent;

                    output.WriteLine("   Intermediate rare event: " + intermediateRareEvent);
                    Console.WriteLine("   Intermediate rare event: " + intermediateRareEvent);
                    output.Flush();

                    if (rareEventFlag)
                    {
                        output.WriteLine("\nReached the rare event. Exiting multilevel cross entropy simulation...");
                        Console.WriteLine("\nReached the rare event. Exiting multilevel cross entropy simulation...");
                        output.Flush();
                        break;
                    }

                    gammaInfo.UpdateStructure();
                    gammaInfo = CrossEntropy2(gammaInfo);

                    for (int i = 0; i < _reactions.NumReactions; i++)
                    {
                        output.WriteLine("   Intermediate gamma " + (i + 1) + ": " + gammaInfo.IntermediateGamma[i]);
                        Console.WriteLine("   Intermediate gamma " + (i + 1) + ": " + gammaInfo.IntermediateGamma[i]);
                    }

                    output.Flush();
                    gammaInfo.UpdateStructure();
                    iter++;

                    if (iter == 15)
                    {
                        throw new ApplicationException("multilevel cross entropy did not converged in 15 iterations.");
                    }
                }
            }

            for (int i = 0; i < _reactions.NumReactions; i++)
                _gamma[i] = gammaInfo.IntermediateGamma[i];
        }

        private GammaInfo CrossEntropy1(GammaInfo gammaInfo)
        {
            var maxRareEventValue = new float[_crossEntropyRuns];
            int counter = 0;

            for (int i = 0; i < _crossEntropyRuns; i++)
            {
                StartRealization();
                float currentMin = _rareEventType * (_rareEventValue - _reExpression.Value);
                var n = new int[_reactions.NumReactions];
                var lambda = new double[_reactions.NumReactions];
                double weight = 1.0;

                while (CurrentTime < duration)
                {
                    if (_rareEventTest.Value)
                    {
                        counter++;
                        gammaInfo.UpdateGamma(weight, n, lambda);
                        break;
                    }

                    StepOnce(ref weight, gammaInfo.IntermediateGamma, ref n, ref lambda);
                    float tempMin = _rareEventType * (_rareEventValue - _reExpression.Value);
                    currentMin = Math.Min(currentMin, tempMin);
                }

                maxRareEventValue[i] = currentMin;
            }

            Array.Sort(maxRareEventValue);
            float ireComp = maxRareEventValue[(int)Math.Ceiling(_crossEntropyRuns * _crossEntropyThreshold)];
            float pastIntermediateRareEvent = gammaInfo.IntermediateRareEvent;
            gammaInfo.IntermediateRareEvent = ireComp < 0 ? _rareEventValue : _rareEventType * (_rareEventValue - ireComp);

            if (gammaInfo.IntermediateRareEvent >= _rareEventValue && counter >= (int)(_crossEntropyRuns * _crossEntropyThreshold))
            {
                gammaInfo.SetIntermediateGamma();
                var gammaInfoOut = new GammaInfo(_reactions.NumReactions)
                {
                    IntermediateRareEvent = _rareEventValue
                };

                for (int i = 0; i < _reactions.NumReactions; i++)
                    gammaInfoOut.IntermediateGamma[i] = gammaInfo.IntermediateGamma[i];

                return gammaInfoOut;
            }

            var cetCriteria = (pastIntermediateRareEvent - gammaInfo.IntermediateRareEvent) * _rareEventType;
            if (cetCriteria > 0)
            {
                _crossEntropyThreshold *= 0.8f;
                Console.WriteLine("Cross entropy threshold changed to : " + _crossEntropyThreshold);
                Console.WriteLine("CETC: {0}   pire: {1}   ire:{2} ", cetCriteria, pastIntermediateRareEvent, gammaInfo.IntermediateRareEvent);

                if (_crossEntropyThreshold * _crossEntropyRuns * 0.8 < _crossEntropyMinDataSize)
                {
                    _crossEntropyRuns = (int)Math.Ceiling(_crossEntropyMinDataSize / _crossEntropyThreshold);
                    Console.WriteLine("Number of cross entropy simulations changed to : " + _crossEntropyRuns);
                }
            }

            return gammaInfo;
        }

        private GammaInfo CrossEntropy2(GammaInfo gammaInfo)
        {
            IBoolean tempRareEventExpression = new EqualTo(_reExpression, new ConstantValue(gammaInfo.IntermediateRareEvent));
            var gammaInfoOut = new GammaInfo(_reactions.NumReactions)
                                   {
                                       IntermediateRareEvent = gammaInfo.IntermediateRareEvent,
                                       IntermediateGamma = gammaInfo.IntermediateGamma
                                   };

            for (int i = 0; i < _crossEntropyRuns; i++)
            {
                StartRealization();
                var n = new int[_reactions.NumReactions];
                var lambda = new double[_reactions.NumReactions];
                double weight = 1.0;

                while (CurrentTime < duration)
                {
                    if (tempRareEventExpression.Value)
                    {
                        gammaInfoOut.UpdateGamma(weight, n, lambda);
                        break;
                    }

                    StepOnce(ref weight, gammaInfoOut.IntermediateGamma, ref n, ref lambda);
                }
            }

            gammaInfoOut.SetIntermediateGamma();

            return gammaInfoOut;
        }

        public override void OutputData(string prefix)
        {
            double reUnc = Math.Sqrt(_runningVariance / _trajectoryCounter) / Math.Sqrt(_trajectoryCounter);
            double reVar = _runningVariance / _trajectoryCounter;
            using (var output = new StreamWriter(_outputFileName))
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
            return "dwSSA";
        }
    }
}