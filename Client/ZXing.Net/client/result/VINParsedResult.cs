using System;
using System.Text;

namespace ZXing.Client.Result
{
    public class VINParsedResult : ParsedResult
    {
        public String VIN { get; private set; }
        public String WorldManufacturerID { get; private set; }
        public String VehicleDescriptorSection { get; private set; }
        public String VehicleIdentifierSection { get; private set; }
        public String CountryCode { get; private set; }
        public String VehicleAttributes { get; private set; }
        public int ModelYear { get; private set; }
        public char PlantCode { get; private set; }
        public String SequentialNumber { get; private set; }

        public VINParsedResult(String vin,
                               String worldManufacturerID,
                               String vehicleDescriptorSection,
                               String vehicleIdentifierSection,
                               String countryCode,
                               String vehicleAttributes,
                               int modelYear,
                               char plantCode,
                               String sequentialNumber)
            : base(ParsedResultType.VIN)
        {
            VIN = vin;
            WorldManufacturerID = worldManufacturerID;
            VehicleDescriptorSection = vehicleDescriptorSection;
            VehicleIdentifierSection = vehicleIdentifierSection;
            CountryCode = countryCode;
            VehicleAttributes = vehicleAttributes;
            ModelYear = modelYear;
            PlantCode = plantCode;
            SequentialNumber = sequentialNumber;
        }

        public override string DisplayResult
        {
            get
            {
                var result = new StringBuilder(50);
                result.Append(WorldManufacturerID).Append(' ');
                result.Append(VehicleDescriptorSection).Append(' ');
                result.Append(VehicleIdentifierSection).Append('\n');
                if (CountryCode != null)
                    result.Append(CountryCode).Append(' ');
                result.Append(ModelYear).Append(' ');
                result.Append(PlantCode).Append(' ');
                result.Append(SequentialNumber).Append('\n');
                return result.ToString();
            }
        }
    }
}
