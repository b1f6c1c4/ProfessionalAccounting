namespace System.Drawing
{
    public sealed class SystemColors
    {
        private SystemColors() { }

        public static Color ActiveBorder { get { return KnownColors.FromKnownColor(KnownColor.ActiveBorder); } }

        public static Color ActiveCaption { get { return KnownColors.FromKnownColor(KnownColor.ActiveCaption); } }

        public static Color ActiveCaptionText
        {
            get { return KnownColors.FromKnownColor(KnownColor.ActiveCaptionText); }
        }

        public static Color AppWorkspace { get { return KnownColors.FromKnownColor(KnownColor.AppWorkspace); } }

        public static Color Control { get { return KnownColors.FromKnownColor(KnownColor.Control); } }

        public static Color ControlDark { get { return KnownColors.FromKnownColor(KnownColor.ControlDark); } }

        public static Color ControlDarkDark { get { return KnownColors.FromKnownColor(KnownColor.ControlDarkDark); } }

        public static Color ControlLight { get { return KnownColors.FromKnownColor(KnownColor.ControlLight); } }

        public static Color ControlLightLight
        {
            get { return KnownColors.FromKnownColor(KnownColor.ControlLightLight); }
        }

        public static Color ControlText { get { return KnownColors.FromKnownColor(KnownColor.ControlText); } }

        public static Color Desktop { get { return KnownColors.FromKnownColor(KnownColor.Desktop); } }

        public static Color GrayText { get { return KnownColors.FromKnownColor(KnownColor.GrayText); } }

        public static Color Highlight { get { return KnownColors.FromKnownColor(KnownColor.Highlight); } }

        public static Color HighlightText { get { return KnownColors.FromKnownColor(KnownColor.HighlightText); } }

        public static Color HotTrack { get { return KnownColors.FromKnownColor(KnownColor.HotTrack); } }

        public static Color InactiveBorder { get { return KnownColors.FromKnownColor(KnownColor.InactiveBorder); } }

        public static Color InactiveCaption { get { return KnownColors.FromKnownColor(KnownColor.InactiveCaption); } }

        public static Color InactiveCaptionText
        {
            get { return KnownColors.FromKnownColor(KnownColor.InactiveCaptionText); }
        }

        public static Color Info { get { return KnownColors.FromKnownColor(KnownColor.Info); } }

        public static Color InfoText { get { return KnownColors.FromKnownColor(KnownColor.InfoText); } }

        public static Color Menu { get { return KnownColors.FromKnownColor(KnownColor.Menu); } }

        public static Color MenuText { get { return KnownColors.FromKnownColor(KnownColor.MenuText); } }

        public static Color ScrollBar { get { return KnownColors.FromKnownColor(KnownColor.ScrollBar); } }

        public static Color Window { get { return KnownColors.FromKnownColor(KnownColor.Window); } }

        public static Color WindowFrame { get { return KnownColors.FromKnownColor(KnownColor.WindowFrame); } }

        public static Color WindowText { get { return KnownColors.FromKnownColor(KnownColor.WindowText); } }
#if NET_2_0
		static public Color ButtonFace {
			get { return KnownColors.FromKnownColor (KnownColor.ButtonFace); }
		}

		static public Color ButtonHighlight {
			get { return KnownColors.FromKnownColor (KnownColor.ButtonHighlight); }
		}

		static public Color ButtonShadow {
			get { return KnownColors.FromKnownColor (KnownColor.ButtonShadow); }
		}

		static public Color GradientActiveCaption {
			get { return KnownColors.FromKnownColor (KnownColor.GradientActiveCaption); }
		}

		static public Color GradientInactiveCaption {
			get { return KnownColors.FromKnownColor (KnownColor.GradientInactiveCaption); }
		}

		static public Color MenuBar {
			get { return KnownColors.FromKnownColor (KnownColor.MenuBar); }
		}

		static public Color MenuHighlight {
			get { return KnownColors.FromKnownColor (KnownColor.MenuHighlight); }
		}
#endif
    }
}
