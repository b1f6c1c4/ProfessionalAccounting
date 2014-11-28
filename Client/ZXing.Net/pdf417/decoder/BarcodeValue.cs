using System.Collections.Generic;

namespace ZXing.PDF417.Internal
{
    /// <summary>
    ///     A Barcode Value for the PDF417 barcode.
    ///     The scanner will iterate through the bitmatrix,
    ///     and given the different methods or iterations
    ///     will increment a given barcode value's confidence.
    ///     When done, this will return the values of highest confidence.
    /// </summary>
    /// <author>Guenther Grau</author>
    public sealed class BarcodeValue
    {
        private readonly IDictionary<int, int> values = new Dictionary<int, int>();

        /// <summary>
        ///     Incremenets the Confidence for a given value. (Adds an occurance of a value)
        /// </summary>
        /// <param name="value">Value.</param>
        public void setValue(int @value)
        {
            int confidence;
            values.TryGetValue(@value, out confidence);
            confidence++;
            values[@value] = confidence;
        }

        /// <summary>
        ///     Determines the maximum occurrence of a set value and returns all values which were set with this occurrence.
        /// </summary>
        /// <returns>an array of int, containing the values with the highest occurrence, or null, if no value was set.</returns>
        public int[] getValue()
        {
            var maxConfidence = -1;
            var result = new List<int>();
            foreach (var entry in values)
                if (entry.Value > maxConfidence)
                {
                    maxConfidence = entry.Value;
                    result.Clear();
                    result.Add(entry.Key);
                }
                else if (entry.Value == maxConfidence)
                    result.Add(entry.Key);
            return result.ToArray();
        }

        /// <summary>
        ///     Returns the confience value for a given barcode value
        /// </summary>
        /// <param name="barcodeValue">Barcode value.</param>
        public int getConfidence(int barcodeValue)
        {
            return values.ContainsKey(barcodeValue) ? values[barcodeValue] : 0;
        }
    }
}
