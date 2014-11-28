using System;

namespace ZXing.Client.Result
{
    /// <author>dswitkin@google.com (Daniel Switkin)</author>
    public sealed class ProductParsedResult : ParsedResult
    {
        internal ProductParsedResult(String productID)
            : this(productID, productID) { }

        internal ProductParsedResult(String productID, String normalizedProductID)
            : base(ParsedResultType.PRODUCT)
        {
            ProductID = productID;
            NormalizedProductID = normalizedProductID;
            displayResultValue = productID;
        }

        public String ProductID { get; private set; }

        public String NormalizedProductID { get; private set; }
    }
}
