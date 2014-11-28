using System;

namespace ZXing
{
    /// <summary>
    ///     A base class which covers the range of exceptions which may occur when encoding a barcode using
    ///     the Writer framework.
    /// </summary>
    /// <author>dswitkin@google.com (Daniel Switkin)</author>
    [Serializable]
    public sealed class WriterException : Exception
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="WriterException" /> class.
        /// </summary>
        public WriterException() { }

        /// <summary>
        ///     Initializes a new instance of the <see cref="WriterException" /> class.
        /// </summary>
        /// <param name="message">The message.</param>
        public WriterException(String message)
            : base(message) { }

        /// <summary>
        ///     Initializes a new instance of the <see cref="WriterException" /> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="innerExc">The inner exc.</param>
        public WriterException(String message, Exception innerExc)
            : base(message, innerExc) { }
    }
}
