using System;

namespace ZXing.OneD.RSS
{
    /// <summary>
    /// </summary>
    public class DataCharacter
    {
        /// <summary>
        ///     Gets the value.
        /// </summary>
        public int Value { get; private set; }

        /// <summary>
        ///     Gets the checksum portion.
        /// </summary>
        public int ChecksumPortion { get; private set; }

        /// <summary>
        ///     Initializes a new instance of the <see cref="DataCharacter" /> class.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="checksumPortion">The checksum portion.</param>
        public DataCharacter(int value, int checksumPortion)
        {
            Value = value;
            ChecksumPortion = checksumPortion;
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override String ToString() { return Value + "(" + ChecksumPortion + ')'; }

        /// <summary>
        ///     Determines whether the specified <see cref="System.Object" /> is equal to this instance.
        /// </summary>
        /// <param name="o">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>
        ///     <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(Object o)
        {
            if (!(o is DataCharacter))
                return false;
            var that = (DataCharacter)o;
            return Value == that.Value && ChecksumPortion == that.ChecksumPortion;
        }

        /// <summary>
        ///     Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        ///     A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.
        /// </returns>
        public override int GetHashCode() { return Value ^ ChecksumPortion; }
    }
}
