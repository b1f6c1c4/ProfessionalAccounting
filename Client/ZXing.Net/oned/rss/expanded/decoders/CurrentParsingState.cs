namespace ZXing.OneD.RSS.Expanded.Decoders
{
    /// <summary>
    ///     <author>Pablo Orduña, University of Deusto (pablo.orduna@deusto.es)</author>
    /// </summary>
    internal sealed class CurrentParsingState
    {
        private int position;
        private State encoding;

        private enum State
        {
            NUMERIC,
            ALPHA,
            ISO_IEC_646
        }

        internal CurrentParsingState()
        {
            position = 0;
            encoding = State.NUMERIC;
        }

        internal int getPosition() { return position; }

        internal void setPosition(int position) { this.position = position; }

        internal void incrementPosition(int delta) { position += delta; }

        internal bool isAlpha() { return encoding == State.ALPHA; }

        internal bool isNumeric() { return encoding == State.NUMERIC; }

        internal bool isIsoIec646() { return encoding == State.ISO_IEC_646; }

        internal void setNumeric() { encoding = State.NUMERIC; }

        internal void setAlpha() { encoding = State.ALPHA; }

        internal void setIsoIec646() { encoding = State.ISO_IEC_646; }
    }
}
