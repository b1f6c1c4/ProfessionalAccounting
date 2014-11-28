namespace ZXing.Common.ReedSolomon
{
    /// <summary>
    ///     <p>Implements Reed-Solomon decoding, as the name implies.</p>
    ///     <p>
    ///         The algorithm will not be explained here, but the following references were helpful
    ///         in creating this implementation:
    ///     </p>
    ///     <ul>
    ///         <li>
    ///             Bruce Maggs.
    ///             <a href="http://www.cs.cmu.edu/afs/cs.cmu.edu/project/pscico-guyb/realworld/www/rs_decode.ps">
    ///                 "Decoding Reed-Solomon Codes"
    ///             </a>
    ///             (see discussion of Forney's Formula)
    ///         </li>
    ///         <li>
    ///             J.I. Hall.
    ///             <a href="www.mth.msu.edu/~jhall/classes/codenotes/GRS.pdf">
    ///                 "Chapter 5. Generalized Reed-Solomon Codes"
    ///             </a>
    ///             (see discussion of Euclidean algorithm)
    ///         </li>
    ///     </ul>
    ///     <p>
    ///         Much credit is due to William Rucklidge since portions of this code are an indirect
    ///         port of his C++ Reed-Solomon implementation.
    ///     </p>
    /// </summary>
    /// <author>Sean Owen</author>
    /// <author>William Rucklidge</author>
    /// <author>sanfordsquires</author>
    public sealed class ReedSolomonDecoder
    {
        private readonly GenericGF field;

        public ReedSolomonDecoder(GenericGF field) { this.field = field; }

        /// <summary>
        ///     <p>
        ///         Decodes given set of received codewords, which include both data and error-correction
        ///         codewords. Really, this means it uses Reed-Solomon to detect and correct errors, in-place,
        ///         in the input.
        ///     </p>
        /// </summary>
        /// <param name="received">data and error-correction codewords</param>
        /// <param name="twoS">number of error-correction codewords available</param>
        /// <returns>false: decoding fails</returns>
        public bool decode(int[] received, int twoS)
        {
            var poly = new GenericGFPoly(field, received);
            var syndromeCoefficients = new int[twoS];
            var noError = true;
            for (var i = 0; i < twoS; i++)
            {
                var eval = poly.evaluateAt(field.exp(i + field.GeneratorBase));
                syndromeCoefficients[syndromeCoefficients.Length - 1 - i] = eval;
                if (eval != 0)
                    noError = false;
            }
            if (noError)
                return true;
            var syndrome = new GenericGFPoly(field, syndromeCoefficients);

            var sigmaOmega = runEuclideanAlgorithm(field.buildMonomial(twoS, 1), syndrome, twoS);
            if (sigmaOmega == null)
                return false;

            var sigma = sigmaOmega[0];
            var errorLocations = findErrorLocations(sigma);
            if (errorLocations == null)
                return false;

            var omega = sigmaOmega[1];
            var errorMagnitudes = findErrorMagnitudes(omega, errorLocations);
            for (var i = 0; i < errorLocations.Length; i++)
            {
                var position = received.Length - 1 - field.log(errorLocations[i]);
                if (position < 0)
                    // throw new ReedSolomonException("Bad error location");
                    return false;
                received[position] = GenericGF.addOrSubtract(received[position], errorMagnitudes[i]);
            }

            return true;
        }

        internal GenericGFPoly[] runEuclideanAlgorithm(GenericGFPoly a, GenericGFPoly b, int R)
        {
            // Assume a's degree is >= b's
            if (a.Degree < b.Degree)
            {
                var temp = a;
                a = b;
                b = temp;
            }

            var rLast = a;
            var r = b;
            var tLast = field.Zero;
            var t = field.One;

            // Run Euclidean algorithm until r's degree is less than R/2
            while (r.Degree >= R / 2)
            {
                var rLastLast = rLast;
                var tLastLast = tLast;
                rLast = r;
                tLast = t;

                // Divide rLastLast by rLast, with quotient in q and remainder in r
                if (rLast.isZero)
                    // Oops, Euclidean algorithm already terminated?
                    // throw new ReedSolomonException("r_{i-1} was zero");
                    return null;
                r = rLastLast;
                var q = field.Zero;
                var denominatorLeadingTerm = rLast.getCoefficient(rLast.Degree);
                var dltInverse = field.inverse(denominatorLeadingTerm);
                while (r.Degree >= rLast.Degree &&
                       !r.isZero)
                {
                    var degreeDiff = r.Degree - rLast.Degree;
                    var scale = field.multiply(r.getCoefficient(r.Degree), dltInverse);
                    q = q.addOrSubtract(field.buildMonomial(degreeDiff, scale));
                    r = r.addOrSubtract(rLast.multiplyByMonomial(degreeDiff, scale));
                }

                t = q.multiply(tLast).addOrSubtract(tLastLast);

                if (r.Degree >= rLast.Degree)
                    // throw new IllegalStateException("Division algorithm failed to reduce polynomial?");
                    return null;
            }

            var sigmaTildeAtZero = t.getCoefficient(0);
            if (sigmaTildeAtZero == 0)
                // throw new ReedSolomonException("sigmaTilde(0) was zero");
                return null;

            var inverse = field.inverse(sigmaTildeAtZero);
            var sigma = t.multiply(inverse);
            var omega = r.multiply(inverse);
            return new[] {sigma, omega};
        }

        private int[] findErrorLocations(GenericGFPoly errorLocator)
        {
            // This is a direct application of Chien's search
            var numErrors = errorLocator.Degree;
            if (numErrors == 1)
                // shortcut
                return new[] {errorLocator.getCoefficient(1)};
            var result = new int[numErrors];
            var e = 0;
            for (var i = 1; i < field.Size && e < numErrors; i++)
                if (errorLocator.evaluateAt(i) == 0)
                {
                    result[e] = field.inverse(i);
                    e++;
                }
            if (e != numErrors)
                // throw new ReedSolomonException("Error locator degree does not match number of roots");
                return null;
            return result;
        }

        private int[] findErrorMagnitudes(GenericGFPoly errorEvaluator, int[] errorLocations)
        {
            // This is directly applying Forney's Formula
            var s = errorLocations.Length;
            var result = new int[s];
            for (var i = 0; i < s; i++)
            {
                var xiInverse = field.inverse(errorLocations[i]);
                var denominator = 1;
                for (var j = 0; j < s; j++)
                    if (i != j)
                    {
                        //denominator = field.multiply(denominator,
                        //    GenericGF.addOrSubtract(1, field.multiply(errorLocations[j], xiInverse)));
                        // Above should work but fails on some Apple and Linux JDKs due to a Hotspot bug.
                        // Below is a funny-looking workaround from Steven Parkes
                        var term = field.multiply(errorLocations[j], xiInverse);
                        var termPlus1 = (term & 0x1) == 0 ? term | 1 : term & ~1;
                        denominator = field.multiply(denominator, termPlus1);

                        // removed in java version, not sure if this is right
                        // denominator = field.multiply(denominator, GenericGF.addOrSubtract(1, field.multiply(errorLocations[j], xiInverse)));
                    }
                result[i] = field.multiply(errorEvaluator.evaluateAt(xiInverse), field.inverse(denominator));
                if (field.GeneratorBase != 0)
                    result[i] = field.multiply(result[i], xiInverse);
            }
            return result;
        }
    }
}
