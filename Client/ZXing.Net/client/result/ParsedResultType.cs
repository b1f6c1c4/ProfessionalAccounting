namespace ZXing.Client.Result
{
    /// <summary>
    ///     Represents the type of data encoded by a barcode -- from plain text, to a
    ///     URI, to an e-mail address, etc.
    /// </summary>
    /// <author>Sean Owen</author>
    public enum ParsedResultType
    {
        ADDRESSBOOK,
        EMAIL_ADDRESS,
        PRODUCT,
        URI,
        TEXT,
        GEO,
        TEL,
        SMS,
        CALENDAR,
        WIFI,
        ISBN,
        VIN
    }
}
