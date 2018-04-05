﻿/***************************************************************************************************

Copyright (c) 2018 Intellectual Ventures Property Holdings, LLC (IVPH) All rights reserved.

EMOD is licensed under the Creative Commons Attribution-Noncommercial-ShareAlike 4.0 License.
To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-sa/4.0/legalcode

***************************************************************************************************/

using System;
using compartments.emod.interfaces;

namespace compartments.emod.expressions
{
    public abstract class BinaryOperation : IValue
    {
        protected IValue ArgumentA;
        protected IValue ArgumentB;

        protected BinaryOperation(IValue argA, IValue argB)
        {
            ArgumentA = argA;
            ArgumentB = argB;
        }

        public abstract double Value { get; }
    }

    // Add, Subtract, Multiply, Divide, Modulo, Power, Minimum, Maximum

    public class Adder : BinaryOperation
    {
        public Adder(IValue argA, IValue argB) : base(argA, argB)
        {
        }

        public override double Value
        {
            get { return ArgumentA.Value + ArgumentB.Value; }
        }
    }

    public class Subtract : BinaryOperation
    {
        public Subtract(IValue argA, IValue argB) : base(argA, argB)
        {
        }

        public override double Value
        {
            get { return ArgumentA.Value - ArgumentB.Value; }
        }
    }

    public class Multiply : BinaryOperation
    {
        public Multiply(IValue argA, IValue argB) : base(argA, argB)
        {
        }

        public override double Value
        {
            get { return ArgumentA.Value*ArgumentB.Value; }
        }
    }

    public class Divide : BinaryOperation
    {
        public Divide(IValue argA, IValue argB) : base(argA, argB)
        {
        }

        public override double Value
        {
            get { return ArgumentA.Value/ArgumentB.Value; }
        }
    }

    public class Modulo : BinaryOperation
    {
        public Modulo(IValue argA, IValue argB) : base(argA, argB)
        {
        }

        public override double Value
        {
            get { return (double) ((int) ArgumentA.Value%(int) ArgumentB.Value); }
        }
    }

    public class Power : BinaryOperation
    {
        public Power(IValue argA, IValue argB) : base(argA, argB)
        {
        }

        public override double Value
        {
            get { return Math.Pow(ArgumentA.Value, ArgumentB.Value); }
        }
    }

    public class Minimum : BinaryOperation
    {
        public Minimum(IValue argA, IValue argB) : base(argA, argB)
        {
        }

        public override double Value
        {
            get { return Math.Min(ArgumentA.Value, ArgumentB.Value); }
        }
    }

    public class Maximum : BinaryOperation
    {
        public Maximum(IValue argA, IValue argB) : base(argA, argB)
        {
        }

        public override double Value
        {
            get { return Math.Max(ArgumentA.Value, ArgumentB.Value); }
        }
    }
}
