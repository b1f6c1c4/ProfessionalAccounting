using System.Collections.Generic;

namespace ZXing.Mobile
{
    public class MobileBarcodeScanningOptions
    {
        /// <summary>
        ///     Camera resolution selector delegate, must return the selected Resolution from the list of available resolutions
        /// </summary>
        public delegate CameraResolution CameraResolutionSelectorDelegate(List<CameraResolution> availableResolutions);

        public MobileBarcodeScanningOptions()
        {
            PossibleFormats = new List<BarcodeFormat>();
            //this.AutoRotate = true;
            DelayBetweenAnalyzingFrames = 150;
            InitialDelayBeforeAnalyzingFrames = 300;
        }

        public CameraResolutionSelectorDelegate CameraResolutionSelector { get; set; }
        public List<BarcodeFormat> PossibleFormats { get; set; }
        public bool? TryHarder { get; set; }
        public bool? PureBarcode { get; set; }
        public bool? AutoRotate { get; set; }
        public string CharacterSet { get; set; }
        public bool? TryInverted { get; set; }
        public bool? UseFrontCameraIfAvailable { get; set; }

        public int DelayBetweenAnalyzingFrames { get; set; }
        public int InitialDelayBeforeAnalyzingFrames { get; set; }

        public static MobileBarcodeScanningOptions Default { get { return new MobileBarcodeScanningOptions(); } }

        public BarcodeReader BuildBarcodeReader()
        {
            var reader = new BarcodeReader();
            if (TryHarder.HasValue)
                reader.Options.TryHarder = TryHarder.Value;
            if (PureBarcode.HasValue)
                reader.Options.PureBarcode = PureBarcode.Value;
            if (AutoRotate.HasValue)
                reader.AutoRotate = AutoRotate.Value;
            if (!string.IsNullOrEmpty(CharacterSet))
                reader.Options.CharacterSet = CharacterSet;
            if (TryInverted.HasValue)
                reader.TryInverted = TryInverted.Value;

            if (PossibleFormats != null &&
                PossibleFormats.Count > 0)
            {
                reader.Options.PossibleFormats = new List<BarcodeFormat>();

                foreach (var pf in PossibleFormats)
                    reader.Options.PossibleFormats.Add(pf);
            }

            return reader;
        }

        public MultiFormatReader BuildMultiFormatReader()
        {
            var reader = new MultiFormatReader();

            var hints = new Dictionary<DecodeHintType, object>();

            if (TryHarder.HasValue &&
                TryHarder.Value)
                hints.Add(DecodeHintType.TRY_HARDER, TryHarder.Value);
            if (PureBarcode.HasValue &&
                PureBarcode.Value)
                hints.Add(DecodeHintType.PURE_BARCODE, PureBarcode.Value);

            if (PossibleFormats != null &&
                PossibleFormats.Count > 0)
                hints.Add(DecodeHintType.POSSIBLE_FORMATS, PossibleFormats);

            reader.Hints = hints;

            return reader;
        }

        internal CameraResolution GetResolution(List<CameraResolution> availableResolutions)
        {
            CameraResolution r = null;

            var dg = CameraResolutionSelector;

            if (dg != null)
                r = dg(availableResolutions);

            return r;
        }
    }
}
