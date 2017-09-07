using System.Collections.Generic;
using compartments.emod.interfaces;

namespace compartments.emod.expressions
{
    public abstract class BinaryOp : INumericOperator
    {
        private readonly INumericOperator _operatorA;
        private readonly INumericOperator _operatorB;
        protected IValue ArgumentA;
        protected IValue ArgumentB;

        protected BinaryOp(INumericOperator firstOperator, INumericOperator secondOperator)
        {
            _operatorA = firstOperator;
            _operatorB = secondOperator;
            ArgumentA = null;
            ArgumentB = null;
        }

        public IValue ResolveReferences(IDictionary<string, IValue> map)
        {
            if (ArgumentA == null)
                ArgumentA = _operatorA.ResolveReferences(map);

            if (ArgumentB == null)
                ArgumentB = _operatorB.ResolveReferences(map);

            return GetValue();
        }

        public abstract IValue GetValue();
        public abstract string OperatorName { get; }

        public override string ToString()
        {
            return "(" + OperatorName + " " + _operatorA + " " + _operatorB + ")";
        }
    }

    // Add,
    // SubtractOperator,
    // MultiplyOperator,
    // DivideOperator,
    // ModuloOperator,
    // PowerOperator,
    // Minimum,
    // MaximumOperator

    public class AddOperator : BinaryOp
    {
        public AddOperator(INumericOperator a, INumericOperator b) : base(a, b) { }

        public override IValue GetValue()
        {
            return new Adder(ArgumentA, ArgumentB);
        }

        public override string OperatorName { get { return "+"; } }
    }

    public class SubtractOperator : BinaryOp
    {
        public SubtractOperator(INumericOperator a, INumericOperator b) : base(a, b) { }

        public override IValue GetValue() { return new Subtract(ArgumentA, ArgumentB); }

        public override string OperatorName { get { return "-"; } }
    }

    public class MultiplyOperator : BinaryOp
    {
        public MultiplyOperator(INumericOperator a, INumericOperator b) : base(a, b) { }

        public override IValue GetValue() { return new Multiply(ArgumentA, ArgumentB); }

        public override string OperatorName { get { return "*"; } }
    }

    public class DivideOperator : BinaryOp
    {
        public DivideOperator(INumericOperator a, INumericOperator b) : base(a, b) { }

        public override IValue GetValue() { return new Divide(ArgumentA, ArgumentB); }

        public override string OperatorName { get { return "/"; } }
    }
    public class ModuloOperator : BinaryOp
    {
        public ModuloOperator(INumericOperator a, INumericOperator b) : base(a, b) { }

        public override IValue GetValue() { return new Modulo(ArgumentA, ArgumentB); }

        public override string OperatorName { get { return "%"; } }
    }

    public class PowerOperator : BinaryOp
    {
        public PowerOperator(INumericOperator a, INumericOperator b) : base(a, b) { }

        public override IValue GetValue() { return new Power(ArgumentA, ArgumentB); }

        public override string OperatorName { get { return "pow"; } }
    }


    public class MaximumOperator : BinaryOp
    {
        public MaximumOperator(INumericOperator a, INumericOperator b) : base(a, b) { }

        public override IValue GetValue() { return new Maximum(ArgumentA, ArgumentB); }

        public override string OperatorName { get { return "max"; } }
    }

    public class MinimumOperator : BinaryOp
    {
        public MinimumOperator(INumericOperator a, INumericOperator b) : base(a, b) { }

        public override IValue GetValue() { return new Minimum(ArgumentA, ArgumentB); }

        public override string OperatorName { get { return "min"; } }
    }
}
