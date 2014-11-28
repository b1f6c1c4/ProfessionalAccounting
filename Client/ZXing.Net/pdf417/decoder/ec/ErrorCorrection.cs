namespace ZXing.PDF417.Internal.EC
{
    /// <summary>
    ///     <p>PDF417 error correction implementation.</p>
    ///     <p>
    ///         This <a href="http://en.wikipedia.org/wiki/Reed%E2%80%93Solomon_error_correction#Example">example</a>
    ///         is quite useful in understanding the algorithm.
    ///     </p>
    ///     <author>Sean Owen</author>
    ///     <see cref="ZXing.Common.ReedSolomon.ReedSolomonDecoder" />
    /// </summary>
    public sealed class ErrorCorrection
    {
        private readonly ModulusGF field;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ErrorCorrection" /> class.
        /// </summary>
        public ErrorCorrection() { field = ModulusGF.PDF417_GF; }

        /// <summary>
        ///     Decodes the specified received.
        /// </summary>
        /// <param name="received">received codewords</param>
        /// <param name="numECCodewords">number of those codewords used for EC</param>
        /// <param name="erasures">location of erasures</param>
        /// <param name="errorLocationsCount">The error locations count.</param>
        /// <returns></returns>
        public bool decode(int[] received, int numECCodewords, int[] erasures, out int errorLocationsCount)
        {
            var poly = new ModulusPoly(field, received);
            var S = new int[numECCodewords];
            var error = false;
            errorLocationsCount = 0;
            for (var i = numECCodewords; i > 0; i--)
            {
                var eval = poly.evaluateAt(field.exp(i));
                S[numECCodewords - i] = eval;
                if (eval != 0)
                    error = true;
            }

            if (!error)
                return true;

            var knownErrors = field.One;
            if (erasures != null)
                foreach (var erasure in erasures)
                {
                    var b = field.exp(received.Length - 1 - erasure);
                    // Add (1 - bx) term:
                    var term = new ModulusPoly(field, new[] {field.subtract(0, b), 1});
                    knownErrors = knownErrors.multiply(term);
                }

            var syndrome = new ModulusPoly(field, S);
            //syndrome = syndrome.multiply(knownErrors);

            var sigmaOmega = runEuclideanAlgorithm(field.buildMonomial(numECCodewords, 1), syndrome, numECCodewords);

            if (sigmaOmega == null)
                return false;

            var sigma = sigmaOmega[0];
            var omega = sigmaOmega[1];

            if (sigma == null ||
                omega == null)
                return false;

            //sigma = sigma.multiply(knownErrors);

            var errorLocations = findErrorLocations(sigma);

            if (errorLocations == null)
                return false;

            var errorMagnitudes = findErrorMagnitudes(omega, sigma, errorLocations);

            for (var i = 0; i < errorLocations.Length; i++)
            {
                var position = received.Length - 1 - field.log(errorLocations[i]);
                if (position < 0)
                    return false;
                received[position] = field.subtract(received[position], errorMagnitudes[i]);
            }
            errorLocationsCount = errorLocations.Length;
            return true;
        }

        /// <summary>
        ///     Runs the euclidean algorithm (Greatest Common Divisor) until r's degree is less than R/2
        /// </summary>
        /// <returns>The euclidean algorithm.</returns>
        private ModulusPoly[] runEuclideanAlgorithm(ModulusPoly a, ModulusPoly b, int R)
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
                    q = q.add(field.buildMonomial(degreeDiff, scale));
                    r = r.subtract(rLast.multiplyByMonomial(degreeDiff, scale));
                }

                t = q.multiply(tLast).subtract(tLastLast).getNegative();
            }

            var sigmaTildeAtZero = t.getCoefficient(0);
            if (sigmaTildeAtZero == 0)
                return null;

            var inverse = field.inverse(sigmaTildeAtZero);
            var sigma = t.multiply(inverse);
            var omega = r.multiply(inverse);
            return new[] {sigma, omega};
        }

        /// <summary>
        ///     Finds the error locations as a direct application of Chien's search
        /// </summary>
        /// <returns>The error locations.</returns>
        /// <param name="errorLocator">Error locator.</param>
        private int[] findErrorLocations(ModulusPoly errorLocator)
        {
            // This is a direct application of Chien's search
            var numErrors = errorLocator.Degree;
            var result = new int[numErrors];
            var e = 0;
            for (var i = 1; i < field.Size && e < numErrors; i++)
                if (errorLocator.evaluateAt(i) == 0)
                {
                    result[e] = field.inverse(i);
                    e++;
                }
            if (e != numErrors)
                return null;
            return result;
        }

        /// <summary>
        ///     Finds the error magnitudes by directly applying Forney's Formula
        /// </summary>
        /// <returns>The error magnitudes.</returns>
        /// <param name="errorEvaluator">Error evaluator.</param>
        /// <param name="errorLocator">Error locator.</param>
        /// <param name="errorLocations">Error locations.</param>
        private int[] findErrorMagnitudes(ModulusPoly errorEvaluator,
                                          ModulusPoly errorLocator,
                                          int[] errorLocations)
        {
            var errorLocatorDegree = errorLocator.Degree;
            var formalDerivativeCoefficients = new int[errorLocatorDegree];
            for (var i = 1; i <= errorLocatorDegree; i++)
                formalDerivativeCoefficients[errorLocatorDegree - i] =
                    field.multiply(i, errorLocator.getCoefficient(i));
            var formalDerivative = new ModulusPoly(field, formalDerivativeCoefficients);

            // This is directly applying Forney's Formula
            var s = errorLocations.Length;
            var result = new int[s];
            for (var i = 0; i < s; i++)
            {
                var xiInverse = field.inverse(errorLocations[i]);
                var numerator = field.subtract(0, errorEvaluator.evaluateAt(xiInverse));
                var denominator = field.inverse(formalDerivative.evaluateAt(xiInverse));
                result[i] = field.multiply(numerator, denominator);
            }
            return result;
        }
    }
}
