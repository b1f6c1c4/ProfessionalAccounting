using System;

namespace ZXing
{
    /// <summary>
    ///     Thrown when a barcode was not found in the image. It might have been
    ///     partially detected but could not be confirmed.
    ///     <author>Sean Owen</author>
    /// </summary>
    [Obsolete("Isn't used anymore, will be removed with next version")]
    public sealed class NotFoundException : ReaderException
    {
        private static readonly NotFoundException instance = new NotFoundException();

        private NotFoundException()
        {
            // do nothing
        }

        public new static NotFoundException Instance { get { return instance; } }
    }
}
