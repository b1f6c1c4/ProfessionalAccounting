namespace ZXing.QrCode.Internal
{
    /// <summary>
    ///     Meta-data container for QR Code decoding. Instances of this class may be used to convey information back to the
    ///     decoding caller. Callers are expected to process this.
    /// </summary>
    public sealed class QRCodeDecoderMetaData
    {
        private readonly bool mirrored;

        /// <summary>
        ///     Initializes a new instance of the <see cref="QRCodeDecoderMetaData" /> class.
        /// </summary>
        /// <param name="mirrored">if set to <c>true</c> [mirrored].</param>
        public QRCodeDecoderMetaData(bool mirrored) { this.mirrored = mirrored; }

        /// <summary>
        ///     true if the QR Code was mirrored.
        /// </summary>
        public bool IsMirrored { get { return mirrored; } }

        /// <summary>
        ///     Apply the result points' order correction due to mirroring.
        /// </summary>
        /// <param name="points">Array of points to apply mirror correction to.</param>
        public void applyMirroredCorrection(ResultPoint[] points)
        {
            if (!mirrored ||
                points == null ||
                points.Length < 3)
                return;
            var bottomLeft = points[0];
            points[0] = points[2];
            points[2] = bottomLeft;
            // No need to 'fix' top-left and alignment pattern.
        }
    }
}
