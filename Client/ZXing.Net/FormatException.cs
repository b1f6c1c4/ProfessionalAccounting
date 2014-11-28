namespace ZXing
{
    /// <summary>
    ///     Thrown when a barcode was successfully detected, but some aspect of
    ///     the content did not conform to the barcode's format rules. This could have
    ///     been due to a mis-detection.
    ///     <author>Sean Owen</author>
    /// </summary>
    public sealed class FormatException : ReaderException
    {
        private static readonly FormatException instance = new FormatException();

        private FormatException()
        {
            // do nothing
        }

        public new static FormatException Instance { get { return instance; } }
    }
}
