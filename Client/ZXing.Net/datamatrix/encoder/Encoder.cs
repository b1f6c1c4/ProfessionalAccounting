namespace ZXing.Datamatrix.Encoder
{
    internal interface Encoder
    {
        int EncodingMode { get; }

        void encode(EncoderContext context);
    }
}
