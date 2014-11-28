namespace ZXing
{
    /// <summary>
    ///     Callback which is invoked when a possible result point (significant
    ///     point in the barcode image such as a corner) is found.
    /// </summary>
    /// <seealso cref="DecodeHintType.NEED_RESULT_POINT_CALLBACK">
    /// </seealso>
    public delegate void ResultPointCallback(ResultPoint point);
}
