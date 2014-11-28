using System.ComponentModel;

namespace System.Drawing
{
    [TypeConverter(typeof(ColorConverter))]
#if ONLY_1_1
	[ComVisible (true)]
#endif
#if !TARGET_JVM
    //[Editor ("System.Drawing.Design.ColorEditor, " + Consts.AssemblySystem_Drawing_Design, typeof (System.Drawing.Design.UITypeEditor))]
#endif
    [Serializable]
    public struct Color
    {
        // Private transparency (A) and R,G,B fields.
        private long value;

        // The specs also indicate that all three of these properties are true
        // if created with FromKnownColor or FromNamedColor, false otherwise (FromARGB).
        // Per Microsoft and ECMA specs these varibles are set by which constructor is used, not by their values.
        [Flags]
        internal enum ColorType : short
        {
            Empty = 0,
            Known = 1,
            ARGB = 2,
            Named = 4,
            System = 8
        }

        internal short state;
        internal short knownColor;
        // #if ONLY_1_1
        // Mono bug #324144 is holding this change
        // MS 1.1 requires this member to be present for serialization (not so in 2.0)
        // however it's bad to keep a string (reference) in a struct
        internal string name;
        // #endif
#if TARGET_JVM
		internal java.awt.Color NativeObject {
			get {
				return new java.awt.Color (R, G, B, A);
			}
		}

		internal static Color FromArgbNamed (int alpha, int red, int green, int blue, string name, KnownColor knownColor)
		{
			Color color = FromArgb (alpha, red, green, blue);
			color.state = (short) (ColorType.Known|ColorType.Named);
			color.name = KnownColors.GetName (knownColor);
			color.knownColor = (short) knownColor;
			return color;
		}

		internal static Color FromArgbSystem (int alpha, int red, int green, int blue, string name, KnownColor knownColor)
		{
			Color color = FromArgbNamed (alpha, red, green, blue, name, knownColor);
			color.state |= (short) ColorType.System;
			return color;
		}
#endif

        public string Name
        {
            get
            {
#if NET_2_0_ONCE_MONO_BUG_324144_IS_FIXED
				if (IsNamedColor)
					return KnownColors.GetName (knownColor);
				else
					return String.Format ("{0:x}", ToArgb ());
#else
                // name is required for serialization under 1.x, but not under 2.0
                if (name == null) // Can happen with stuff deserialized from MS
                    if (IsNamedColor)
                        name = KnownColors.GetName(knownColor);
                    else
                        name = String.Format("{0:x}", ToArgb());
                return name;
#endif
            }
        }

        public bool IsKnownColor { get { return (state & ((short)ColorType.Known)) != 0; } }

        public bool IsSystemColor { get { return (state & ((short)ColorType.System)) != 0; } }

        public bool IsNamedColor { get { return (state & (short)(ColorType.Known | ColorType.Named)) != 0; } }

        internal long Value
        {
            get
            {
                // Optimization for known colors that were deserialized
                // from an MS serialized stream.  
                if (value == 0 && IsKnownColor)
                    value = KnownColors.FromKnownColor((KnownColor)knownColor).ToArgb() & 0xFFFFFFFF;
                return value;
            }
            set { this.value = value; }
        }

        public static Color FromArgb(int red, int green, int blue) { return FromArgb(255, red, green, blue); }

        public static Color FromArgb(int alpha, int red, int green, int blue)
        {
            CheckARGBValues(alpha, red, green, blue);
            var color = new Color();
            color.state = (short)ColorType.ARGB;
            color.Value = (int)((uint)alpha << 24) + (red << 16) + (green << 8) + blue;
            return color;
        }

        public int ToArgb() { return (int)Value; }

        public static Color FromArgb(int alpha, Color baseColor)
        {
            return FromArgb(alpha, baseColor.R, baseColor.G, baseColor.B);
        }

        public static Color FromArgb(int argb)
        {
            return FromArgb((argb >> 24) & 0x0FF, (argb >> 16) & 0x0FF, (argb >> 8) & 0x0FF, argb & 0x0FF);
        }

        public static Color FromKnownColor(KnownColor color) { return KnownColors.FromKnownColor(color); }

        public static Color FromName(string name)
        {
            try
            {
                var kc = (KnownColor)Enum.Parse(typeof(KnownColor), name, true);
                return KnownColors.FromKnownColor(kc);
            }
            catch
            {
                // This is what it returns! 	 
                var d = FromArgb(0, 0, 0, 0);
                d.name = name;
                d.state |= (short)ColorType.Named;
                return d;
            }
        }


        // -----------------------
        // Public Shared Members
        // -----------------------

        /// <summary>
        ///     Empty Shared Field
        /// </summary>
        /// <remarks>
        ///     An uninitialized Color Structure
        /// </remarks>
        public static readonly Color Empty;

        /// <summary>
        ///     Equality Operator
        /// </summary>
        /// <remarks>
        ///     Compares two Color objects. The return value is
        ///     based on the equivalence of the A,R,G,B properties
        ///     of the two Colors.
        /// </remarks>
        public static bool operator ==(Color left, Color right)
        {
            if (left.Value != right.Value)
                return false;
            if (left.IsNamedColor != right.IsNamedColor)
                return false;
            if (left.IsSystemColor != right.IsSystemColor)
                return false;
            if (left.IsEmpty != right.IsEmpty)
                return false;
            if (left.IsNamedColor) // then both are named (see previous check) and so we need to compare them
                // but otherwise we don't as it kills performance (Name calls String.Format)
                if (left.Name != right.Name)
                    return false;
            return true;
        }

        /// <summary>
        ///     Inequality Operator
        /// </summary>
        /// <remarks>
        ///     Compares two Color objects. The return value is
        ///     based on the equivalence of the A,R,G,B properties
        ///     of the two colors.
        /// </remarks>
        public static bool operator !=(Color left, Color right)
        {
            return ! (left == right);
        }

        public float GetBrightness()
        {
            var minval = Math.Min(R, Math.Min(G, B));
            var maxval = Math.Max(R, Math.Max(G, B));

            return (float)(maxval + minval) / 510;
        }

        public float GetSaturation()
        {
            var minval = Math.Min(R, Math.Min(G, B));
            var maxval = Math.Max(R, Math.Max(G, B));

            if (maxval == minval)
                return 0.0f;

            var sum = maxval + minval;
            if (sum > 255)
                sum = 510 - sum;

            return (float)(maxval - minval) / sum;
        }

        public float GetHue()
        {
            int r = R;
            int g = G;
            int b = B;
            var minval = (byte)Math.Min(r, Math.Min(g, b));
            var maxval = (byte)Math.Max(r, Math.Max(g, b));

            if (maxval == minval)
                return 0.0f;

            float diff = maxval - minval;
            var rnorm = (maxval - r) / diff;
            var gnorm = (maxval - g) / diff;
            var bnorm = (maxval - b) / diff;

            var hue = 0.0f;
            if (r == maxval)
                hue = 60.0f * (6.0f + bnorm - gnorm);
            if (g == maxval)
                hue = 60.0f * (2.0f + rnorm - bnorm);
            if (b == maxval)
                hue = 60.0f * (4.0f + gnorm - rnorm);
            if (hue > 360.0f)
                hue = hue - 360.0f;

            return hue;
        }

        // -----------------------
        // Public Instance Members
        // -----------------------

        /// <summary>
        ///     ToKnownColor method
        /// </summary>
        /// <remarks>
        ///     Returns the KnownColor enum value for this color, 0 if is not known.
        /// </remarks>
        public KnownColor ToKnownColor()
        {
            return (KnownColor)knownColor;
        }

        /// <summary>
        ///     IsEmpty Property
        /// </summary>
        /// <remarks>
        ///     Indicates transparent black. R,G,B = 0; A=0?
        /// </remarks>
        public bool IsEmpty { get { return state == (short)ColorType.Empty; } }

        public byte A { get { return (byte)(Value >> 24); } }

        public byte R { get { return (byte)(Value >> 16); } }

        public byte G { get { return (byte)(Value >> 8); } }

        public byte B { get { return (byte)Value; } }

        /// <summary>
        ///     Equals Method
        /// </summary>
        /// <remarks>
        ///     Checks equivalence of this Color and another object.
        /// </remarks>
        public override bool Equals(object obj)
        {
            if (!(obj is Color))
                return false;
            var c = (Color)obj;
            return this == c;
        }

        //public bool ReferenceEquals (object o)
        //{
        //	if (!(o is Color))return false;
        //	return (this == (Color) o);
        //}
        /// <summary>
        ///     Reference Equals Method
        ///     Is commented out because this is handled by the base class.
        ///     TODO: Is it correct to let the base class handel reference equals
        /// </summary>
        /// <remarks>
        ///     Checks equivalence of this Color and another object.
        /// </remarks>
        /// <summary>
        ///     GetHashCode Method
        /// </summary>
        /// <remarks>
        ///     Calculates a hashing value.
        /// </remarks>
        public override int GetHashCode()
        {
            var hc = (int)(Value ^ (Value >> 32) ^ state ^ (knownColor >> 16));
            if (IsNamedColor)
                hc ^= Name.GetHashCode();
            return hc;
        }

        /// <summary>
        ///     ToString Method
        /// </summary>
        /// <remarks>
        ///     Formats the Color as a string in ARGB notation.
        /// </remarks>
        public override string ToString()
        {
            if (IsEmpty)
                return "Color [Empty]";

            // Use the property here, not the field.
            if (IsNamedColor)
                return "Color [" + Name + "]";

            return String.Format("Color [A={0}, R={1}, G={2}, B={3}]", A, R, G, B);
        }

        private static void CheckRGBValues(int red, int green, int blue)
        {
            if ((red > 255) ||
                (red < 0))
                throw CreateColorArgumentException(red, "red");
            if ((green > 255) ||
                (green < 0))
                throw CreateColorArgumentException(green, "green");
            if ((blue > 255) ||
                (blue < 0))
                throw CreateColorArgumentException(blue, "blue");
        }

        private static ArgumentException CreateColorArgumentException(int value, string color)
        {
            return new ArgumentException(
                string.Format(
                              "'{0}' is not a valid"
                              + " value for '{1}'. '{1}' should be greater or equal to 0 and"
                              + " less than or equal to 255.",
                              value,
                              color));
        }

        private static void CheckARGBValues(int alpha, int red, int green, int blue)
        {
            if ((alpha > 255) ||
                (alpha < 0))
                throw CreateColorArgumentException(alpha, "alpha");
            CheckRGBValues(red, green, blue);
        }


        public static Color Transparent { get { return KnownColors.FromKnownColor(KnownColor.Transparent); } }

        public static Color AliceBlue { get { return KnownColors.FromKnownColor(KnownColor.AliceBlue); } }

        public static Color AntiqueWhite { get { return KnownColors.FromKnownColor(KnownColor.AntiqueWhite); } }

        public static Color Aqua { get { return KnownColors.FromKnownColor(KnownColor.Aqua); } }

        public static Color Aquamarine { get { return KnownColors.FromKnownColor(KnownColor.Aquamarine); } }

        public static Color Azure { get { return KnownColors.FromKnownColor(KnownColor.Azure); } }

        public static Color Beige { get { return KnownColors.FromKnownColor(KnownColor.Beige); } }

        public static Color Bisque { get { return KnownColors.FromKnownColor(KnownColor.Bisque); } }

        public static Color Black { get { return KnownColors.FromKnownColor(KnownColor.Black); } }

        public static Color BlanchedAlmond { get { return KnownColors.FromKnownColor(KnownColor.BlanchedAlmond); } }

        public static Color Blue { get { return KnownColors.FromKnownColor(KnownColor.Blue); } }

        public static Color BlueViolet { get { return KnownColors.FromKnownColor(KnownColor.BlueViolet); } }

        public static Color Brown { get { return KnownColors.FromKnownColor(KnownColor.Brown); } }

        public static Color BurlyWood { get { return KnownColors.FromKnownColor(KnownColor.BurlyWood); } }

        public static Color CadetBlue { get { return KnownColors.FromKnownColor(KnownColor.CadetBlue); } }

        public static Color Chartreuse { get { return KnownColors.FromKnownColor(KnownColor.Chartreuse); } }

        public static Color Chocolate { get { return KnownColors.FromKnownColor(KnownColor.Chocolate); } }

        public static Color Coral { get { return KnownColors.FromKnownColor(KnownColor.Coral); } }

        public static Color CornflowerBlue { get { return KnownColors.FromKnownColor(KnownColor.CornflowerBlue); } }

        public static Color Cornsilk { get { return KnownColors.FromKnownColor(KnownColor.Cornsilk); } }

        public static Color Crimson { get { return KnownColors.FromKnownColor(KnownColor.Crimson); } }

        public static Color Cyan { get { return KnownColors.FromKnownColor(KnownColor.Cyan); } }

        public static Color DarkBlue { get { return KnownColors.FromKnownColor(KnownColor.DarkBlue); } }

        public static Color DarkCyan { get { return KnownColors.FromKnownColor(KnownColor.DarkCyan); } }

        public static Color DarkGoldenrod { get { return KnownColors.FromKnownColor(KnownColor.DarkGoldenrod); } }

        public static Color DarkGray { get { return KnownColors.FromKnownColor(KnownColor.DarkGray); } }

        public static Color DarkGreen { get { return KnownColors.FromKnownColor(KnownColor.DarkGreen); } }

        public static Color DarkKhaki { get { return KnownColors.FromKnownColor(KnownColor.DarkKhaki); } }

        public static Color DarkMagenta { get { return KnownColors.FromKnownColor(KnownColor.DarkMagenta); } }

        public static Color DarkOliveGreen { get { return KnownColors.FromKnownColor(KnownColor.DarkOliveGreen); } }

        public static Color DarkOrange { get { return KnownColors.FromKnownColor(KnownColor.DarkOrange); } }

        public static Color DarkOrchid { get { return KnownColors.FromKnownColor(KnownColor.DarkOrchid); } }

        public static Color DarkRed { get { return KnownColors.FromKnownColor(KnownColor.DarkRed); } }

        public static Color DarkSalmon { get { return KnownColors.FromKnownColor(KnownColor.DarkSalmon); } }

        public static Color DarkSeaGreen { get { return KnownColors.FromKnownColor(KnownColor.DarkSeaGreen); } }

        public static Color DarkSlateBlue { get { return KnownColors.FromKnownColor(KnownColor.DarkSlateBlue); } }

        public static Color DarkSlateGray { get { return KnownColors.FromKnownColor(KnownColor.DarkSlateGray); } }

        public static Color DarkTurquoise { get { return KnownColors.FromKnownColor(KnownColor.DarkTurquoise); } }

        public static Color DarkViolet { get { return KnownColors.FromKnownColor(KnownColor.DarkViolet); } }

        public static Color DeepPink { get { return KnownColors.FromKnownColor(KnownColor.DeepPink); } }

        public static Color DeepSkyBlue { get { return KnownColors.FromKnownColor(KnownColor.DeepSkyBlue); } }

        public static Color DimGray { get { return KnownColors.FromKnownColor(KnownColor.DimGray); } }

        public static Color DodgerBlue { get { return KnownColors.FromKnownColor(KnownColor.DodgerBlue); } }

        public static Color Firebrick { get { return KnownColors.FromKnownColor(KnownColor.Firebrick); } }

        public static Color FloralWhite { get { return KnownColors.FromKnownColor(KnownColor.FloralWhite); } }

        public static Color ForestGreen { get { return KnownColors.FromKnownColor(KnownColor.ForestGreen); } }

        public static Color Fuchsia { get { return KnownColors.FromKnownColor(KnownColor.Fuchsia); } }

        public static Color Gainsboro { get { return KnownColors.FromKnownColor(KnownColor.Gainsboro); } }

        public static Color GhostWhite { get { return KnownColors.FromKnownColor(KnownColor.GhostWhite); } }

        public static Color Gold { get { return KnownColors.FromKnownColor(KnownColor.Gold); } }

        public static Color Goldenrod { get { return KnownColors.FromKnownColor(KnownColor.Goldenrod); } }

        public static Color Gray { get { return KnownColors.FromKnownColor(KnownColor.Gray); } }

        public static Color Green { get { return KnownColors.FromKnownColor(KnownColor.Green); } }

        public static Color GreenYellow { get { return KnownColors.FromKnownColor(KnownColor.GreenYellow); } }

        public static Color Honeydew { get { return KnownColors.FromKnownColor(KnownColor.Honeydew); } }

        public static Color HotPink { get { return KnownColors.FromKnownColor(KnownColor.HotPink); } }

        public static Color IndianRed { get { return KnownColors.FromKnownColor(KnownColor.IndianRed); } }

        public static Color Indigo { get { return KnownColors.FromKnownColor(KnownColor.Indigo); } }

        public static Color Ivory { get { return KnownColors.FromKnownColor(KnownColor.Ivory); } }

        public static Color Khaki { get { return KnownColors.FromKnownColor(KnownColor.Khaki); } }

        public static Color Lavender { get { return KnownColors.FromKnownColor(KnownColor.Lavender); } }

        public static Color LavenderBlush { get { return KnownColors.FromKnownColor(KnownColor.LavenderBlush); } }

        public static Color LawnGreen { get { return KnownColors.FromKnownColor(KnownColor.LawnGreen); } }

        public static Color LemonChiffon { get { return KnownColors.FromKnownColor(KnownColor.LemonChiffon); } }

        public static Color LightBlue { get { return KnownColors.FromKnownColor(KnownColor.LightBlue); } }

        public static Color LightCoral { get { return KnownColors.FromKnownColor(KnownColor.LightCoral); } }

        public static Color LightCyan { get { return KnownColors.FromKnownColor(KnownColor.LightCyan); } }

        public static Color LightGoldenrodYellow
        {
            get { return KnownColors.FromKnownColor(KnownColor.LightGoldenrodYellow); }
        }

        public static Color LightGreen { get { return KnownColors.FromKnownColor(KnownColor.LightGreen); } }

        public static Color LightGray { get { return KnownColors.FromKnownColor(KnownColor.LightGray); } }

        public static Color LightPink { get { return KnownColors.FromKnownColor(KnownColor.LightPink); } }

        public static Color LightSalmon { get { return KnownColors.FromKnownColor(KnownColor.LightSalmon); } }

        public static Color LightSeaGreen { get { return KnownColors.FromKnownColor(KnownColor.LightSeaGreen); } }

        public static Color LightSkyBlue { get { return KnownColors.FromKnownColor(KnownColor.LightSkyBlue); } }

        public static Color LightSlateGray { get { return KnownColors.FromKnownColor(KnownColor.LightSlateGray); } }

        public static Color LightSteelBlue { get { return KnownColors.FromKnownColor(KnownColor.LightSteelBlue); } }

        public static Color LightYellow { get { return KnownColors.FromKnownColor(KnownColor.LightYellow); } }

        public static Color Lime { get { return KnownColors.FromKnownColor(KnownColor.Lime); } }

        public static Color LimeGreen { get { return KnownColors.FromKnownColor(KnownColor.LimeGreen); } }

        public static Color Linen { get { return KnownColors.FromKnownColor(KnownColor.Linen); } }

        public static Color Magenta { get { return KnownColors.FromKnownColor(KnownColor.Magenta); } }

        public static Color Maroon { get { return KnownColors.FromKnownColor(KnownColor.Maroon); } }

        public static Color MediumAquamarine { get { return KnownColors.FromKnownColor(KnownColor.MediumAquamarine); } }

        public static Color MediumBlue { get { return KnownColors.FromKnownColor(KnownColor.MediumBlue); } }

        public static Color MediumOrchid { get { return KnownColors.FromKnownColor(KnownColor.MediumOrchid); } }

        public static Color MediumPurple { get { return KnownColors.FromKnownColor(KnownColor.MediumPurple); } }

        public static Color MediumSeaGreen { get { return KnownColors.FromKnownColor(KnownColor.MediumSeaGreen); } }

        public static Color MediumSlateBlue { get { return KnownColors.FromKnownColor(KnownColor.MediumSlateBlue); } }

        public static Color MediumSpringGreen
        {
            get { return KnownColors.FromKnownColor(KnownColor.MediumSpringGreen); }
        }

        public static Color MediumTurquoise { get { return KnownColors.FromKnownColor(KnownColor.MediumTurquoise); } }

        public static Color MediumVioletRed { get { return KnownColors.FromKnownColor(KnownColor.MediumVioletRed); } }

        public static Color MidnightBlue { get { return KnownColors.FromKnownColor(KnownColor.MidnightBlue); } }

        public static Color MintCream { get { return KnownColors.FromKnownColor(KnownColor.MintCream); } }

        public static Color MistyRose { get { return KnownColors.FromKnownColor(KnownColor.MistyRose); } }

        public static Color Moccasin { get { return KnownColors.FromKnownColor(KnownColor.Moccasin); } }

        public static Color NavajoWhite { get { return KnownColors.FromKnownColor(KnownColor.NavajoWhite); } }

        public static Color Navy { get { return KnownColors.FromKnownColor(KnownColor.Navy); } }

        public static Color OldLace { get { return KnownColors.FromKnownColor(KnownColor.OldLace); } }

        public static Color Olive { get { return KnownColors.FromKnownColor(KnownColor.Olive); } }

        public static Color OliveDrab { get { return KnownColors.FromKnownColor(KnownColor.OliveDrab); } }

        public static Color Orange { get { return KnownColors.FromKnownColor(KnownColor.Orange); } }

        public static Color OrangeRed { get { return KnownColors.FromKnownColor(KnownColor.OrangeRed); } }

        public static Color Orchid { get { return KnownColors.FromKnownColor(KnownColor.Orchid); } }

        public static Color PaleGoldenrod { get { return KnownColors.FromKnownColor(KnownColor.PaleGoldenrod); } }

        public static Color PaleGreen { get { return KnownColors.FromKnownColor(KnownColor.PaleGreen); } }

        public static Color PaleTurquoise { get { return KnownColors.FromKnownColor(KnownColor.PaleTurquoise); } }

        public static Color PaleVioletRed { get { return KnownColors.FromKnownColor(KnownColor.PaleVioletRed); } }

        public static Color PapayaWhip { get { return KnownColors.FromKnownColor(KnownColor.PapayaWhip); } }

        public static Color PeachPuff { get { return KnownColors.FromKnownColor(KnownColor.PeachPuff); } }

        public static Color Peru { get { return KnownColors.FromKnownColor(KnownColor.Peru); } }

        public static Color Pink { get { return KnownColors.FromKnownColor(KnownColor.Pink); } }

        public static Color Plum { get { return KnownColors.FromKnownColor(KnownColor.Plum); } }

        public static Color PowderBlue { get { return KnownColors.FromKnownColor(KnownColor.PowderBlue); } }

        public static Color Purple { get { return KnownColors.FromKnownColor(KnownColor.Purple); } }

        public static Color Red { get { return KnownColors.FromKnownColor(KnownColor.Red); } }

        public static Color RosyBrown { get { return KnownColors.FromKnownColor(KnownColor.RosyBrown); } }

        public static Color RoyalBlue { get { return KnownColors.FromKnownColor(KnownColor.RoyalBlue); } }

        public static Color SaddleBrown { get { return KnownColors.FromKnownColor(KnownColor.SaddleBrown); } }

        public static Color Salmon { get { return KnownColors.FromKnownColor(KnownColor.Salmon); } }

        public static Color SandyBrown { get { return KnownColors.FromKnownColor(KnownColor.SandyBrown); } }

        public static Color SeaGreen { get { return KnownColors.FromKnownColor(KnownColor.SeaGreen); } }

        public static Color SeaShell { get { return KnownColors.FromKnownColor(KnownColor.SeaShell); } }

        public static Color Sienna { get { return KnownColors.FromKnownColor(KnownColor.Sienna); } }

        public static Color Silver { get { return KnownColors.FromKnownColor(KnownColor.Silver); } }

        public static Color SkyBlue { get { return KnownColors.FromKnownColor(KnownColor.SkyBlue); } }

        public static Color SlateBlue { get { return KnownColors.FromKnownColor(KnownColor.SlateBlue); } }

        public static Color SlateGray { get { return KnownColors.FromKnownColor(KnownColor.SlateGray); } }

        public static Color Snow { get { return KnownColors.FromKnownColor(KnownColor.Snow); } }

        public static Color SpringGreen { get { return KnownColors.FromKnownColor(KnownColor.SpringGreen); } }

        public static Color SteelBlue { get { return KnownColors.FromKnownColor(KnownColor.SteelBlue); } }

        public static Color Tan { get { return KnownColors.FromKnownColor(KnownColor.Tan); } }

        public static Color Teal { get { return KnownColors.FromKnownColor(KnownColor.Teal); } }

        public static Color Thistle { get { return KnownColors.FromKnownColor(KnownColor.Thistle); } }

        public static Color Tomato { get { return KnownColors.FromKnownColor(KnownColor.Tomato); } }

        public static Color Turquoise { get { return KnownColors.FromKnownColor(KnownColor.Turquoise); } }

        public static Color Violet { get { return KnownColors.FromKnownColor(KnownColor.Violet); } }

        public static Color Wheat { get { return KnownColors.FromKnownColor(KnownColor.Wheat); } }

        public static Color White { get { return KnownColors.FromKnownColor(KnownColor.White); } }

        public static Color WhiteSmoke { get { return KnownColors.FromKnownColor(KnownColor.WhiteSmoke); } }

        public static Color Yellow { get { return KnownColors.FromKnownColor(KnownColor.Yellow); } }

        public static Color YellowGreen { get { return KnownColors.FromKnownColor(KnownColor.YellowGreen); } }
    }
}
