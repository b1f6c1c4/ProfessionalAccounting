using System;
using System.Collections.Generic;
using ZXing.Aztec;
using ZXing.Common;
using ZXing.Datamatrix;
using ZXing.OneD;
using ZXing.PDF417;
using ZXing.QrCode;

namespace ZXing
{
    /// <summary>
    ///     This is a factory class which finds the appropriate Writer subclass for the BarcodeFormat
    ///     requested and encodes the barcode with the supplied contents.
    /// </summary>
    /// <author>
    ///     dswitkin@google.com (Daniel Switkin)
    /// </author>
    /// <author>
    ///     www.Redivivus.in (suraj.supekar@redivivus.in) - Ported from ZXING Java Source
    /// </author>
    public sealed class MultiFormatWriter : Writer
    {
        private static readonly IDictionary<BarcodeFormat, Func<Writer>> formatMap;

        static MultiFormatWriter()
        {
            formatMap = new Dictionary<BarcodeFormat, Func<Writer>>
                            {
                                {BarcodeFormat.EAN_8, () => new EAN8Writer()},
                                {BarcodeFormat.EAN_13, () => new EAN13Writer()},
                                {BarcodeFormat.UPC_A, () => new UPCAWriter()},
                                {BarcodeFormat.QR_CODE, () => new QRCodeWriter()},
                                {BarcodeFormat.CODE_39, () => new Code39Writer()},
                                {BarcodeFormat.CODE_128, () => new Code128Writer()},
                                {BarcodeFormat.ITF, () => new ITFWriter()},
                                {BarcodeFormat.PDF_417, () => new PDF417Writer()},
                                {BarcodeFormat.CODABAR, () => new CodaBarWriter()},
                                {BarcodeFormat.MSI, () => new MSIWriter()},
                                {BarcodeFormat.PLESSEY, () => new PlesseyWriter()},
                                {BarcodeFormat.DATA_MATRIX, () => new DataMatrixWriter()},
                                {BarcodeFormat.AZTEC, () => new AztecWriter()},
                            };
        }

        /// <summary>
        ///     Gets the collection of supported writers.
        /// </summary>
        public static ICollection<BarcodeFormat> SupportedWriters { get { return formatMap.Keys; } }

        public BitMatrix encode(String contents, BarcodeFormat format, int width, int height)
        {
            return encode(contents, format, width, height, null);
        }

        public BitMatrix encode(String contents, BarcodeFormat format, int width, int height,
                                IDictionary<EncodeHintType, object> hints)
        {
            if (!formatMap.ContainsKey(format))
                throw new ArgumentException("No encoder available for format " + format);

            return formatMap[format]().encode(contents, format, width, height, hints);
        }
    }
}
