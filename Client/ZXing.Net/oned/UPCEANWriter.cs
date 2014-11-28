namespace ZXing.OneD
{
    /// <summary>
    ///     <p>
    ///         Encapsulates functionality and implementation that is common to UPC and EAN families
    ///         of one-dimensional barcodes.
    ///     </p>
    ///     <author>aripollak@gmail.com (Ari Pollak)</author>
    ///     <author>dsbnatut@gmail.com (Kazuki Nishiura)</author>
    /// </summary>
    public abstract class UPCEANWriter : OneDimensionalCodeWriter
    {
        /// <summary>
        ///     Gets the default margin.
        /// </summary>
        public override int DefaultMargin
        {
            get
            {
                // Use a different default more appropriate for UPC/EAN
                return UPCEANReader.START_END_PATTERN.Length;
            }
        }
    }
}
