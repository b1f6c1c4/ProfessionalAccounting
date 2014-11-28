namespace ZXing.Datamatrix.Encoder
{
    internal sealed class DataMatrixSymbolInfo144 : SymbolInfo
    {
        public DataMatrixSymbolInfo144()
            : base(false, 1558, 620, 22, 22, 36, -1, 62) { }

        public override int getInterleavedBlockCount() { return 10; }

        public override int getDataLengthForInterleavedBlock(int index) { return (index <= 8) ? 156 : 155; }
    }
}
