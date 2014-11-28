using System;
using System.Collections.Generic;

namespace ZXing.Common
{
    /// <summary>
    ///     Encapsulates the result of decoding a matrix of bits. This typically
    ///     applies to 2D barcode formats. For now it contains the raw bytes obtained,
    ///     as well as a String interpretation of those bytes, if applicable.
    ///     <author>Sean Owen</author>
    /// </summary>
    public sealed class DecoderResult
    {
        public byte[] RawBytes { get; private set; }

        public String Text { get; private set; }

        public IList<byte[]> ByteSegments { get; private set; }

        public String ECLevel { get; private set; }

        public bool StructuredAppend
        {
            get { return StructuredAppendParity >= 0 && StructuredAppendSequenceNumber >= 0; }
        }

        public int ErrorsCorrected { get; set; }

        public int StructuredAppendSequenceNumber { get; private set; }

        public int Erasures { get; set; }

        public int StructuredAppendParity { get; private set; }

        /// <summary>
        ///     Miscellanseous data value for the various decoders
        /// </summary>
        /// <value>The other.</value>
        public object Other { get; set; }

        public DecoderResult(byte[] rawBytes, String text, IList<byte[]> byteSegments, String ecLevel)
            : this(rawBytes, text, byteSegments, ecLevel, -1, -1) {}

        public DecoderResult(byte[] rawBytes, String text, IList<byte[]> byteSegments, String ecLevel, int saSequence,
                             int saParity)
        {
            if (rawBytes == null &&
                text == null)
                throw new ArgumentException();
            RawBytes = rawBytes;
            Text = text;
            ByteSegments = byteSegments;
            ECLevel = ecLevel;
            StructuredAppendParity = saParity;
            StructuredAppendSequenceNumber = saSequence;
        }
    }
}
