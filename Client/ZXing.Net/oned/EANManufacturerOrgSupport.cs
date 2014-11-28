using System;
using System.Collections.Generic;

namespace ZXing.OneD
{
    /// <summary>
    ///     Records EAN prefix to GS1 Member Organization, where the member organization
    ///     correlates strongly with a country. This is an imperfect means of identifying
    ///     a country of origin by EAN-13 barcode value. See
    ///     <a href="http://en.wikipedia.org/wiki/List_of_GS1_country_codes">
    ///         http://en.wikipedia.org/wiki/List_of_GS1_country_codes
    ///     </a>
    ///     .
    ///     <author>Sean Owen</author>
    /// </summary>
    internal sealed class EANManufacturerOrgSupport
    {
        private readonly List<int[]> ranges = new List<int[]>();
        private readonly List<String> countryIdentifiers = new List<String>();

        internal String lookupCountryIdentifier(String productCode)
        {
            initIfNeeded();
            var prefix = Int32.Parse(productCode.Substring(0, 3));
            var max = ranges.Count;
            for (var i = 0; i < max; i++)
            {
                var range = ranges[i];
                var start = range[0];
                if (prefix < start)
                    return null;
                var end = range.Length == 1 ? start : range[1];
                if (prefix <= end)
                    return countryIdentifiers[i];
            }
            return null;
        }

        private void add(int[] range, String id)
        {
            ranges.Add(range);
            countryIdentifiers.Add(id);
        }

        private void initIfNeeded()
        {
            if (ranges.Count != 0)
                return;
            add(new[] {0, 19}, "US/CA");
            add(new[] {30, 39}, "US");
            add(new[] {60, 139}, "US/CA");
            add(new[] {300, 379}, "FR");
            add(new[] {380}, "BG");
            add(new[] {383}, "SI");
            add(new[] {385}, "HR");
            add(new[] {387}, "BA");
            add(new[] {400, 440}, "DE");
            add(new[] {450, 459}, "JP");
            add(new[] {460, 469}, "RU");
            add(new[] {471}, "TW");
            add(new[] {474}, "EE");
            add(new[] {475}, "LV");
            add(new[] {476}, "AZ");
            add(new[] {477}, "LT");
            add(new[] {478}, "UZ");
            add(new[] {479}, "LK");
            add(new[] {480}, "PH");
            add(new[] {481}, "BY");
            add(new[] {482}, "UA");
            add(new[] {484}, "MD");
            add(new[] {485}, "AM");
            add(new[] {486}, "GE");
            add(new[] {487}, "KZ");
            add(new[] {489}, "HK");
            add(new[] {490, 499}, "JP");
            add(new[] {500, 509}, "GB");
            add(new[] {520}, "GR");
            add(new[] {528}, "LB");
            add(new[] {529}, "CY");
            add(new[] {531}, "MK");
            add(new[] {535}, "MT");
            add(new[] {539}, "IE");
            add(new[] {540, 549}, "BE/LU");
            add(new[] {560}, "PT");
            add(new[] {569}, "IS");
            add(new[] {570, 579}, "DK");
            add(new[] {590}, "PL");
            add(new[] {594}, "RO");
            add(new[] {599}, "HU");
            add(new[] {600, 601}, "ZA");
            add(new[] {603}, "GH");
            add(new[] {608}, "BH");
            add(new[] {609}, "MU");
            add(new[] {611}, "MA");
            add(new[] {613}, "DZ");
            add(new[] {616}, "KE");
            add(new[] {618}, "CI");
            add(new[] {619}, "TN");
            add(new[] {621}, "SY");
            add(new[] {622}, "EG");
            add(new[] {624}, "LY");
            add(new[] {625}, "JO");
            add(new[] {626}, "IR");
            add(new[] {627}, "KW");
            add(new[] {628}, "SA");
            add(new[] {629}, "AE");
            add(new[] {640, 649}, "FI");
            add(new[] {690, 695}, "CN");
            add(new[] {700, 709}, "NO");
            add(new[] {729}, "IL");
            add(new[] {730, 739}, "SE");
            add(new[] {740}, "GT");
            add(new[] {741}, "SV");
            add(new[] {742}, "HN");
            add(new[] {743}, "NI");
            add(new[] {744}, "CR");
            add(new[] {745}, "PA");
            add(new[] {746}, "DO");
            add(new[] {750}, "MX");
            add(new[] {754, 755}, "CA");
            add(new[] {759}, "VE");
            add(new[] {760, 769}, "CH");
            add(new[] {770}, "CO");
            add(new[] {773}, "UY");
            add(new[] {775}, "PE");
            add(new[] {777}, "BO");
            add(new[] {779}, "AR");
            add(new[] {780}, "CL");
            add(new[] {784}, "PY");
            add(new[] {785}, "PE");
            add(new[] {786}, "EC");
            add(new[] {789, 790}, "BR");
            add(new[] {800, 839}, "IT");
            add(new[] {840, 849}, "ES");
            add(new[] {850}, "CU");
            add(new[] {858}, "SK");
            add(new[] {859}, "CZ");
            add(new[] {860}, "YU");
            add(new[] {865}, "MN");
            add(new[] {867}, "KP");
            add(new[] {868, 869}, "TR");
            add(new[] {870, 879}, "NL");
            add(new[] {880}, "KR");
            add(new[] {885}, "TH");
            add(new[] {888}, "SG");
            add(new[] {890}, "IN");
            add(new[] {893}, "VN");
            add(new[] {896}, "PK");
            add(new[] {899}, "ID");
            add(new[] {900, 919}, "AT");
            add(new[] {930, 939}, "AU");
            add(new[] {940, 949}, "AZ");
            add(new[] {955}, "MY");
            add(new[] {958}, "MO");
        }
    }
}
