using System;
using System.Collections.Generic;

namespace ZXing.Client.Result
{
    /// <summary>
    ///     Implements the "BIZCARD" address book entry format, though this has been
    ///     largely reverse-engineered from examples observed in the wild -- still
    ///     looking for a definitive reference.
    /// </summary>
    /// <author>
    ///     Sean Owen
    /// </author>
    /// <author>
    ///     www.Redivivus.in (suraj.supekar@redivivus.in) - Ported from ZXING Java Source
    /// </author>
    internal sealed class BizcardResultParser : AbstractDoCoMoResultParser
    {
        // Yes, we extend AbstractDoCoMoResultParser since the format is very much
        // like the DoCoMo MECARD format, but this is not technically one of 
        // DoCoMo's proposed formats

        public override ParsedResult parse(ZXing.Result result)
        {
            var rawText = result.Text;
            if (rawText == null ||
                !rawText.StartsWith("BIZCARD:"))
                return null;
            var firstName = matchSingleDoCoMoPrefixedField("N:", rawText, true);
            var lastName = matchSingleDoCoMoPrefixedField("X:", rawText, true);
            var fullName = buildName(firstName, lastName);
            var title = matchSingleDoCoMoPrefixedField("T:", rawText, true);
            var org = matchSingleDoCoMoPrefixedField("C:", rawText, true);
            var addresses = matchDoCoMoPrefixedField("A:", rawText, true);
            var phoneNumber1 = matchSingleDoCoMoPrefixedField("B:", rawText, true);
            var phoneNumber2 = matchSingleDoCoMoPrefixedField("M:", rawText, true);
            var phoneNumber3 = matchSingleDoCoMoPrefixedField("F:", rawText, true);
            var email = matchSingleDoCoMoPrefixedField("E:", rawText, true);

            return new AddressBookParsedResult(
                maybeWrap(fullName),
                null,
                null,
                buildPhoneNumbers(phoneNumber1, phoneNumber2, phoneNumber3),
                null,
                maybeWrap(email),
                null,
                null,
                null,
                addresses,
                null,
                org,
                null,
                title,
                null,
                null);
        }

        private static String[] buildPhoneNumbers(String number1, String number2, String number3)
        {
            var numbers = new List<string>();
            if (number1 != null)
                numbers.Add(number1);
            if (number2 != null)
                numbers.Add(number2);
            if (number3 != null)
                numbers.Add(number3);
            var size = numbers.Count;
            if (size == 0)
                return null;
            return SupportClass.toStringArray(numbers);
        }

        private static String buildName(String firstName, String lastName)
        {
            if (firstName == null)
                return lastName;
            return lastName == null ? firstName : firstName + ' ' + lastName;
        }
    }
}
