using System;
using ZXing.OneD;

namespace ZXing.Client.Result
{
    /// <summary>
    ///     Parses strings of digits that represent a UPC code.
    /// </summary>
    /// <author>dswitkin@google.com (Daniel Switkin)</author>
    internal sealed class ProductResultParser : ResultParser
    {
        // Treat all UPC and EAN variants as UPCs, in the sense that they are all product barcodes.
        public override ParsedResult parse(ZXing.Result result)
        {
            var format = result.BarcodeFormat;
            if (!(format == BarcodeFormat.UPC_A || format == BarcodeFormat.UPC_E ||
                  format == BarcodeFormat.EAN_8 || format == BarcodeFormat.EAN_13))
                return null;
            // Really neither of these should happen:
            var rawText = result.Text;
            if (rawText == null)
                return null;

            if (!isStringOfDigits(rawText, rawText.Length))
                return null;
            // Not actually checking the checksum again here    

            String normalizedProductID;
            // Expand UPC-E for purposes of searching
            if (format == BarcodeFormat.UPC_E &&
                rawText.Length == 8)
                normalizedProductID = UPCEReader.convertUPCEtoUPCA(rawText);
            else
                normalizedProductID = rawText;

            return new ProductParsedResult(rawText, normalizedProductID);
        }
    }
}
