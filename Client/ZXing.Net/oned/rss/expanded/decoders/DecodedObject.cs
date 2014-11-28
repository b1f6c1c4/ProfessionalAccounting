namespace ZXing.OneD.RSS.Expanded.Decoders
{
    /// <summary>
    ///     <author>Pablo Orduña, University of Deusto (pablo.orduna@deusto.es)</author>
    /// </summary>
    internal abstract class DecodedObject
    {
        internal int NewPosition { get; private set; }

        internal DecodedObject(int newPosition) { NewPosition = newPosition; }
    }
}
