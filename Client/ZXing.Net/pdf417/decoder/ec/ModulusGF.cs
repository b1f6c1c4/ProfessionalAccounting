using System;

namespace ZXing.PDF417.Internal.EC
{
    /// <summary>
    ///     <p>A field based on powers of a generator integer, modulo some modulus.</p>
    ///     @see com.google.zxing.common.reedsolomon.GenericGF
    /// </summary>
    /// <author>Sean Owen</author>
    internal sealed class ModulusGF
    {
        public static ModulusGF PDF417_GF = new ModulusGF(PDF417Common.NUMBER_OF_CODEWORDS, 3);

        private readonly int[] expTable;
        private readonly int[] logTable;
        public ModulusPoly Zero { get; private set; }
        public ModulusPoly One { get; private set; }
        private readonly int modulus;

        public ModulusGF(int modulus, int generator)
        {
            this.modulus = modulus;
            expTable = new int[modulus];
            logTable = new int[modulus];
            var x = 1;
            for (var i = 0; i < modulus; i++)
            {
                expTable[i] = x;
                x = (x * generator) % modulus;
            }
            for (var i = 0; i < modulus - 1; i++)
                logTable[expTable[i]] = i;
            // logTable[0] == 0 but this should never be used
            Zero = new ModulusPoly(this, new[] {0});
            One = new ModulusPoly(this, new[] {1});
        }

        internal ModulusPoly buildMonomial(int degree, int coefficient)
        {
            if (degree < 0)
                throw new ArgumentException();
            if (coefficient == 0)
                return Zero;
            var coefficients = new int[degree + 1];
            coefficients[0] = coefficient;
            return new ModulusPoly(this, coefficients);
        }

        internal int add(int a, int b) { return (a + b) % modulus; }

        internal int subtract(int a, int b) { return (modulus + a - b) % modulus; }

        internal int exp(int a) { return expTable[a]; }

        internal int log(int a)
        {
            if (a == 0)
                throw new ArgumentException();
            return logTable[a];
        }

        internal int inverse(int a)
        {
            if (a == 0)
                throw new ArithmeticException();
            return expTable[modulus - logTable[a] - 1];
        }

        internal int multiply(int a, int b)
        {
            if (a == 0 ||
                b == 0)
                return 0;
            return expTable[(logTable[a] + logTable[b]) % (modulus - 1)];
        }

        internal int Size { get { return modulus; } }
    }
}
