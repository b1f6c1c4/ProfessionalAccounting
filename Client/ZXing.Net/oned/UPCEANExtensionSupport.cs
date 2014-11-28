using ZXing.Common;

namespace ZXing.OneD
{
    internal sealed class UPCEANExtensionSupport
    {
        private static readonly int[] EXTENSION_START_PATTERN = {1, 1, 2};

        private readonly UPCEANExtension2Support twoSupport = new UPCEANExtension2Support();
        private readonly UPCEANExtension5Support fiveSupport = new UPCEANExtension5Support();

        internal Result decodeRow(int rowNumber, BitArray row, int rowOffset)
        {
            var extensionStartRange = UPCEANReader.findGuardPattern(row, rowOffset, false, EXTENSION_START_PATTERN);
            if (extensionStartRange == null)
                return null;
            var result = fiveSupport.decodeRow(rowNumber, row, extensionStartRange);
            if (result == null)
                result = twoSupport.decodeRow(rowNumber, row, extensionStartRange);
            return result;
        }
    }
}
