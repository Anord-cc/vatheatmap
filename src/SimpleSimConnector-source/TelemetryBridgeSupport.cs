using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace SimpleSimConnector
{
    public sealed class SimVarDefinition
    {
        public SimVarDefinition(
            string structFieldName,
            string simVarName,
            int? simVarIndex,
            string simConnectUnit,
            string simConnectType,
            string rawUnit,
            string outputUnit,
            string conversionFunction,
            string sanityRange,
            bool isString = false)
        {
            StructFieldName = structFieldName;
            SimVarName = simVarName;
            SimVarIndex = simVarIndex;
            SimConnectUnit = simConnectUnit;
            SimConnectType = simConnectType;
            RawUnit = rawUnit;
            OutputUnit = outputUnit;
            ConversionFunction = conversionFunction;
            SanityRange = sanityRange;
            IsString = isString;
        }

        public string StructFieldName { get; private set; }
        public string SimVarName { get; private set; }
        public int? SimVarIndex { get; private set; }
        public string SimConnectUnit { get; private set; }
        public string SimConnectType { get; private set; }
        public string RawUnit { get; private set; }
        public string OutputUnit { get; private set; }
        public string ConversionFunction { get; private set; }
        public string SanityRange { get; private set; }
        public bool IsString { get; private set; }
    }

    public sealed class JsonFieldMapping
    {
        public JsonFieldMapping(
            string jsonField,
            string simVarName,
            int? simVarIndex,
            string simConnectUnit,
            string simConnectType,
            string rawUnit,
            string outputUnit,
            string conversionFunction,
            double? minValue,
            double? maxValue,
            string sanityRange)
        {
            JsonField = jsonField;
            SimVarName = simVarName;
            SimVarIndex = simVarIndex;
            SimConnectUnit = simConnectUnit;
            SimConnectType = simConnectType;
            RawUnit = rawUnit;
            OutputUnit = outputUnit;
            ConversionFunction = conversionFunction;
            MinValue = minValue;
            MaxValue = maxValue;
            SanityRange = sanityRange;
        }

        public string JsonField { get; private set; }
        public string SimVarName { get; private set; }
        public int? SimVarIndex { get; private set; }
        public string SimConnectUnit { get; private set; }
        public string SimConnectType { get; private set; }
        public string RawUnit { get; private set; }
        public string OutputUnit { get; private set; }
        public string ConversionFunction { get; private set; }
        public double? MinValue { get; private set; }
        public double? MaxValue { get; private set; }
        public string SanityRange { get; private set; }
    }

    public sealed class TelemetryRejectedValue
    {
        public string JsonField { get; set; }
        public string SimVarName { get; set; }
        public string RequestedUnit { get; set; }
        public string RawValue { get; set; }
        public string ConvertedValue { get; set; }
        public string SanityRange { get; set; }
        public string Reason { get; set; }
        public string Action { get; set; }
    }

    public static class TelemetryBridgeCatalog
    {
        public static readonly SimVarDefinition[] IdentityDefinitions =
        {
            new SimVarDefinition(
                "title",
                "TITLE",
                null,
                null,
                "STRING256",
                "string",
                "string",
                "identity",
                "n/a",
                isString: true)
        };

        public static readonly SimVarDefinition[] NumericDefinitions =
        {
            new SimVarDefinition("latitude", "PLANE LATITUDE", null, "degrees", "FLOAT64", "degrees", "degrees", "identity", "-90..90"),
            new SimVarDefinition("longitude", "PLANE LONGITUDE", null, "degrees", "FLOAT64", "degrees", "degrees", "identity", "-180..180"),
            new SimVarDefinition("altitudeMeters", "PLANE ALTITUDE", null, "meters", "FLOAT64", "meters", "meters", "identity", "n/a"),
            new SimVarDefinition("groundSpeedMetersPerSecond", "GROUND VELOCITY", null, "knots", "FLOAT64", "knots", "m/s", "knots * 0.514444", "0..150"),
            new SimVarDefinition("headingTrueDegrees", "PLANE HEADING DEGREES TRUE", null, "degrees", "FLOAT64", "degrees", "degrees", "normalize heading", "0..360"),
            new SimVarDefinition("headingMagneticDegrees", "PLANE HEADING DEGREES MAGNETIC", null, "degrees", "FLOAT64", "degrees", "degrees", "normalize heading", "0..360"),
            new SimVarDefinition("onGround", "SIM ON GROUND", null, "bool", "FLOAT64", "bool", "bool", "raw bool", "0|1"),
            new SimVarDefinition("verticalSpeedMetersPerSecond", "VERTICAL SPEED", null, "feet per second", "FLOAT64", "ft/s", "m/s", "ft/s * 0.3048", "n/a"),
            new SimVarDefinition("pitchDegrees", "PLANE PITCH DEGREES", null, "degrees", "FLOAT64", "degrees", "degrees", "identity", "n/a"),
            new SimVarDefinition("bankDegrees", "PLANE BANK DEGREES", null, "degrees", "FLOAT64", "degrees", "degrees", "identity", "n/a"),
            new SimVarDefinition("gForce", "G FORCE", null, "gforce", "FLOAT64", "g", "g", "identity", "n/a"),
            new SimVarDefinition("groundElevationMeters", "GROUND ALTITUDE", null, "meters", "FLOAT64", "meters", "meters", "identity", "n/a"),
            new SimVarDefinition("landingRateMetersPerSecond", "PLANE TOUCHDOWN NORMAL VELOCITY", null, "meters per second", "FLOAT64", "m/s", "m/s", "identity", "n/a"),
            new SimVarDefinition("indicatedAirspeedMetersPerSecond", "AIRSPEED INDICATED", null, "knots", "FLOAT64", "knots", "m/s", "knots * 0.514444", "0..250"),
            new SimVarDefinition("trueAirspeedMetersPerSecond", "AIRSPEED TRUE", null, "knots", "FLOAT64", "knots", "m/s", "knots * 0.514444", "0..350"),
            new SimVarDefinition("barberPoleAirspeedMetersPerSecond", "AIRSPEED BARBER POLE", null, "knots", "FLOAT64", "knots", "m/s", "knots * 0.514444", "0..400"),
            new SimVarDefinition("parkingBrake", "BRAKE PARKING POSITION", null, "bool", "FLOAT64", "bool", "bool", "raw bool", "0|1"),
            new SimVarDefinition("numFlapPositions", "TRAILING EDGE FLAPS NUM HANDLE POSITIONS", null, "number", "FLOAT64", "count", "count", "identity", "0..20"),
            new SimVarDefinition("gearDown", "GEAR HANDLE POSITION", null, "bool", "FLOAT64", "bool", "bool", "raw bool", "0|1"),
            new SimVarDefinition("lightNavigation", "LIGHT NAV", null, "bool", "FLOAT64", "bool", "bool", "raw bool", "0|1"),
            new SimVarDefinition("lightBeacon", "LIGHT BEACON", null, "bool", "FLOAT64", "bool", "bool", "raw bool", "0|1"),
            new SimVarDefinition("lightStrobes", "LIGHT STROBE", null, "bool", "FLOAT64", "bool", "bool", "raw bool", "0|1"),
            new SimVarDefinition("lightInstruments", "LIGHT PANEL", null, "bool", "FLOAT64", "bool", "bool", "raw bool", "0|1"),
            new SimVarDefinition("lightLogo", "LIGHT LOGO", null, "bool", "FLOAT64", "bool", "bool", "raw bool", "0|1"),
            new SimVarDefinition("lightCabin", "LIGHT CABIN", null, "bool", "FLOAT64", "bool", "bool", "raw bool", "0|1"),
            new SimVarDefinition("com1Frequency", "COM ACTIVE FREQUENCY:1", 1, "Frequency BCD16", "FLOAT64", "BCD16", "MHz string", "decode BCD16", "118.00..136.99"),
            new SimVarDefinition("com2Frequency", "COM ACTIVE FREQUENCY:2", 2, "Frequency BCD16", "FLOAT64", "BCD16", "MHz string", "decode BCD16", "118.00..136.99"),
            new SimVarDefinition("nav1Frequency", "NAV ACTIVE FREQUENCY:1", 1, "MHz", "FLOAT64", "MHz", "MHz string", "identity format", "108.00..117.95"),
            new SimVarDefinition("nav2Frequency", "NAV ACTIVE FREQUENCY:2", 2, "MHz", "FLOAT64", "MHz", "MHz string", "identity format", "108.00..117.95"),
            new SimVarDefinition("transponderCode", "TRANSPONDER CODE:1", 1, "number", "FLOAT64", "number", "octal string", "format 0000", "0000..7777"),
            new SimVarDefinition("engineType", "ENGINE TYPE", null, "enum", "FLOAT64", "enum", "enum string", "enum mapping", "known enum"),
            new SimVarDefinition("itt1DegreesCelsius", "TURB ENG ITT:1", 1, "celsius", "FLOAT64", "celsius", "celsius", "identity", "n/a"),
            new SimVarDefinition("itt2DegreesCelsius", "TURB ENG ITT:2", 2, "celsius", "FLOAT64", "celsius", "celsius", "identity", "n/a"),
            new SimVarDefinition("antiIce1Enabled", "ENG ANTI ICE:1", 1, "bool", "FLOAT64", "bool", "bool", "raw bool", "0|1"),
            new SimVarDefinition("antiIce2Enabled", "ENG ANTI ICE:2", 2, "bool", "FLOAT64", "bool", "bool", "raw bool", "0|1"),
            new SimVarDefinition("exitOpen", "EXIT OPEN", null, "percent over 100", "FLOAT64", "0..1", "bool", "> 0", "0..1"),
            new SimVarDefinition("apuPctRpm", "APU PCT RPM", null, "percent over 100", "FLOAT64", "0..100", "status input", "APU status", "0..100"),
            new SimVarDefinition("apuSwitch", "APU SWITCH", null, "bool", "FLOAT64", "bool", "status input", "APU status", "0|1"),
            new SimVarDefinition("apuGeneratorActive", "APU GENERATOR ACTIVE:1", 1, "bool", "FLOAT64", "bool", "status input", "APU status", "0|1"),
            new SimVarDefinition("fuelWeightPerGallon", "FUEL WEIGHT PER GALLON", null, "pounds", "FLOAT64", "lb/gal", "kg factor", "lb * 0.45359237", "> 0"),
            new SimVarDefinition("fuelTankCenterCapacityGallons", "FUEL TANK CENTER CAPACITY", null, "gallons", "FLOAT64", "gallons", "capacity kg input", "gal * density", "0..300000 kg"),
            new SimVarDefinition("fuelTankCenterQuantityGallons", "FUEL TANK CENTER QUANTITY", null, "gallons", "FLOAT64", "gallons", "percent input", "qty/cap", "0..100%"),
            new SimVarDefinition("fuelTankCenter2CapacityGallons", "FUEL TANK CENTER2 CAPACITY", null, "gallons", "FLOAT64", "gallons", "capacity kg input", "gal * density", "0..300000 kg"),
            new SimVarDefinition("fuelTankCenter2QuantityGallons", "FUEL TANK CENTER2 QUANTITY", null, "gallons", "FLOAT64", "gallons", "percent input", "qty/cap", "0..100%"),
            new SimVarDefinition("fuelTankCenter3CapacityGallons", "FUEL TANK CENTER3 CAPACITY", null, "gallons", "FLOAT64", "gallons", "capacity kg input", "gal * density", "0..300000 kg"),
            new SimVarDefinition("fuelTankCenter3QuantityGallons", "FUEL TANK CENTER3 QUANTITY", null, "gallons", "FLOAT64", "gallons", "percent input", "qty/cap", "0..100%"),
            new SimVarDefinition("fuelTankLeftMainCapacityGallons", "FUEL TANK LEFT MAIN CAPACITY", null, "gallons", "FLOAT64", "gallons", "capacity kg input", "gal * density", "0..300000 kg"),
            new SimVarDefinition("fuelTankLeftMainQuantityGallons", "FUEL TANK LEFT MAIN QUANTITY", null, "gallons", "FLOAT64", "gallons", "percent input", "qty/cap", "0..100%"),
            new SimVarDefinition("fuelTankLeftAuxCapacityGallons", "FUEL TANK LEFT AUX CAPACITY", null, "gallons", "FLOAT64", "gallons", "capacity kg input", "gal * density", "0..300000 kg"),
            new SimVarDefinition("fuelTankLeftAuxQuantityGallons", "FUEL TANK LEFT AUX QUANTITY", null, "gallons", "FLOAT64", "gallons", "percent input", "qty/cap", "0..100%"),
            new SimVarDefinition("fuelTankLeftTipCapacityGallons", "FUEL TANK LEFT TIP CAPACITY", null, "gallons", "FLOAT64", "gallons", "capacity kg input", "gal * density", "0..300000 kg"),
            new SimVarDefinition("fuelTankLeftTipQuantityGallons", "FUEL TANK LEFT TIP QUANTITY", null, "gallons", "FLOAT64", "gallons", "percent input", "qty/cap", "0..100%"),
            new SimVarDefinition("fuelTankRightMainCapacityGallons", "FUEL TANK RIGHT MAIN CAPACITY", null, "gallons", "FLOAT64", "gallons", "capacity kg input", "gal * density", "0..300000 kg"),
            new SimVarDefinition("fuelTankRightMainQuantityGallons", "FUEL TANK RIGHT MAIN QUANTITY", null, "gallons", "FLOAT64", "gallons", "percent input", "qty/cap", "0..100%"),
            new SimVarDefinition("fuelTankRightAuxCapacityGallons", "FUEL TANK RIGHT AUX CAPACITY", null, "gallons", "FLOAT64", "gallons", "capacity kg input", "gal * density", "0..300000 kg"),
            new SimVarDefinition("fuelTankRightAuxQuantityGallons", "FUEL TANK RIGHT AUX QUANTITY", null, "gallons", "FLOAT64", "gallons", "percent input", "qty/cap", "0..100%"),
            new SimVarDefinition("fuelTankRightTipCapacityGallons", "FUEL TANK RIGHT TIP CAPACITY", null, "gallons", "FLOAT64", "gallons", "capacity kg input", "gal * density", "0..300000 kg"),
            new SimVarDefinition("fuelTankRightTipQuantityGallons", "FUEL TANK RIGHT TIP QUANTITY", null, "gallons", "FLOAT64", "gallons", "percent input", "qty/cap", "0..100%"),
            new SimVarDefinition("fuelTankExternal1CapacityGallons", "FUEL TANK EXTERNAL1 CAPACITY", null, "gallons", "FLOAT64", "gallons", "capacity kg input", "gal * density", "0..300000 kg"),
            new SimVarDefinition("fuelTankExternal1QuantityGallons", "FUEL TANK EXTERNAL1 QUANTITY", null, "gallons", "FLOAT64", "gallons", "percent input", "qty/cap", "0..100%"),
            new SimVarDefinition("fuelTankExternal2CapacityGallons", "FUEL TANK EXTERNAL2 CAPACITY", null, "gallons", "FLOAT64", "gallons", "capacity kg input", "gal * density", "0..300000 kg"),
            new SimVarDefinition("fuelTankExternal2QuantityGallons", "FUEL TANK EXTERNAL2 QUANTITY", null, "gallons", "FLOAT64", "gallons", "percent input", "qty/cap", "0..100%"),
            new SimVarDefinition("outsideAirTemperatureCelsius", "AMBIENT TEMPERATURE", null, "celsius", "FLOAT64", "celsius", "celsius", "identity", "-100..70"),
            new SimVarDefinition("visibilityMeters", "AMBIENT VISIBILITY", null, "meters", "FLOAT64", "meters", "km", "meters / 1000", ">= 0"),
            new SimVarDefinition("windSpeedKnots", "AMBIENT WIND VELOCITY", null, "knots", "FLOAT64", "knots", "m/s", "knots * 0.514444", "0..150"),
            new SimVarDefinition("windDirectionDegrees", "AMBIENT WIND DIRECTION", null, "degrees", "FLOAT64", "degrees true", "degrees", "normalize heading", "0..360"),
            new SimVarDefinition("ambientPressureInchesHg", "AMBIENT PRESSURE", null, "inHg", "FLOAT64", "inHg", "pascal", "inHg * 3386.389", "15000..110000"),
            new SimVarDefinition("seaLevelPressurePascal", "SEA LEVEL PRESSURE", null, "millibars", "FLOAT64", "millibars", "pascal", "mbar * 100", "80000..110000"),
            new SimVarDefinition("barometerSettingMillibars", "KOHLSMAN SETTING MB:1", 1, "millibars", "FLOAT64", "millibars", "pascal", "mbar * 100", "80000..110000"),
            new SimVarDefinition("cabinAltitudeMeters", "PRESSURIZATION CABIN ALTITUDE", null, "meters", "FLOAT64", "meters", "meters", "identity", "n/a"),
            new SimVarDefinition("yawDamperEnabled", "AUTOPILOT YAW DAMPER", null, "bool", "FLOAT64", "bool", "bool", "raw bool", "0|1"),
            new SimVarDefinition("flightDirectorEnabled", "AUTOPILOT FLIGHT DIRECTOR ACTIVE", null, "bool", "FLOAT64", "bool", "bool", "raw bool", "0|1"),
            new SimVarDefinition("autopilotAirspeedHoldKnots", "AUTOPILOT AIRSPEED HOLD VAR", null, "knots", "FLOAT64", "knots", "m/s", "knots * 0.514444", "0..250"),
            new SimVarDefinition("autopilotMachHoldMach", "AUTOPILOT MACH HOLD VAR", null, "mach", "FLOAT64", "mach", "mach", "identity", "0..1.2"),
            new SimVarDefinition("autopilotAltitudeHoldFeet", "AUTOPILOT ALTITUDE LOCK VAR", null, "feet", "FLOAT64", "feet", "meters", "feet * 0.3048", "n/a"),
            new SimVarDefinition("autopilotHeadingLockDegrees", "AUTOPILOT HEADING LOCK DIR", null, "degrees", "FLOAT64", "degrees", "degrees", "normalize heading", "0..360"),
            new SimVarDefinition("autopilotPitchHoldRadians", "AUTOPILOT PITCH HOLD REF", null, "radians", "FLOAT64", "radians", "degrees", "rad * 57.2957795", "n/a"),
            new SimVarDefinition("autopilotVerticalSpeedHoldFeetPerMinute", "AUTOPILOT VERTICAL HOLD VAR", null, "feet per minute", "FLOAT64", "ft/min", "m/s", "ft/min * 0.00508", "n/a"),
            new SimVarDefinition("autopilotAltitudeHoldActive", "AUTOPILOT ALTITUDE LOCK", null, "bool", "FLOAT64", "bool", "bool", "raw bool", "0|1"),
            new SimVarDefinition("autopilotHeadingLockActive", "AUTOPILOT HEADING LOCK", null, "bool", "FLOAT64", "bool", "bool", "raw bool", "0|1"),
            new SimVarDefinition("autopilotAirspeedHoldActive", "AUTOPILOT AIRSPEED HOLD", null, "bool", "FLOAT64", "bool", "bool", "raw bool", "0|1"),
            new SimVarDefinition("autopilotMachHoldActive", "AUTOPILOT MACH HOLD", null, "bool", "FLOAT64", "bool", "bool", "raw bool", "0|1"),
            new SimVarDefinition("autopilotVerticalSpeedHoldActive", "AUTOPILOT VERTICAL HOLD", null, "bool", "FLOAT64", "bool", "bool", "raw bool", "0|1")
        };

        public static readonly JsonFieldMapping[] JsonFieldMappings =
        {
            new JsonFieldMapping("latitude", "PLANE LATITUDE", null, "degrees", "FLOAT64", "degrees", "degrees", "identity", -90, 90, "-90..90"),
            new JsonFieldMapping("position.latitude", "PLANE LATITUDE", null, "degrees", "FLOAT64", "degrees", "degrees", "identity", -90, 90, "-90..90"),
            new JsonFieldMapping("longitude", "PLANE LONGITUDE", null, "degrees", "FLOAT64", "degrees", "degrees", "identity", -180, 180, "-180..180"),
            new JsonFieldMapping("position.longitude", "PLANE LONGITUDE", null, "degrees", "FLOAT64", "degrees", "degrees", "identity", -180, 180, "-180..180"),
            new JsonFieldMapping("altitude", "PLANE ALTITUDE", null, "meters", "FLOAT64", "meters", "meters", "identity", null, null, "n/a"),
            new JsonFieldMapping("altitudeMeters", "PLANE ALTITUDE", null, "meters", "FLOAT64", "meters", "meters", "identity", null, null, "n/a"),
            new JsonFieldMapping("groundspeed", "GROUND VELOCITY", null, "knots", "FLOAT64", "knots", "m/s", "knots * 0.514444", 0, 150, "0..150"),
            new JsonFieldMapping("groundSpeedMetersPerSecond", "GROUND VELOCITY", null, "knots", "FLOAT64", "knots", "m/s", "knots * 0.514444", 0, 150, "0..150"),
            new JsonFieldMapping("heading", "PLANE HEADING DEGREES TRUE", null, "degrees", "FLOAT64", "degrees", "degrees", "normalize heading", 0, 360, "0..360"),
            new JsonFieldMapping("headingTrueDegrees", "PLANE HEADING DEGREES TRUE", null, "degrees", "FLOAT64", "degrees", "degrees", "normalize heading", 0, 360, "0..360"),
            new JsonFieldMapping("headingMagneticDegrees", "PLANE HEADING DEGREES MAGNETIC", null, "degrees", "FLOAT64", "degrees", "degrees", "normalize heading", 0, 360, "0..360"),
            new JsonFieldMapping("outsideAirTemperatureCelsius", "AMBIENT TEMPERATURE", null, "celsius", "FLOAT64", "celsius", "celsius", "identity", -100, 70, "-100..70"),
            new JsonFieldMapping("visibilityKm", "AMBIENT VISIBILITY", null, "meters", "FLOAT64", "meters", "km", "meters / 1000", 0, null, ">= 0"),
            new JsonFieldMapping("windSpeedMetersPerSecond", "AMBIENT WIND VELOCITY", null, "knots", "FLOAT64", "knots", "m/s", "knots * 0.514444", 0, 150, "0..150"),
            new JsonFieldMapping("windDirectionDegrees", "AMBIENT WIND DIRECTION", null, "degrees", "FLOAT64", "degrees true", "degrees", "normalize heading", 0, 360, "0..360"),
            new JsonFieldMapping("ambientPressurePascal", "AMBIENT PRESSURE", null, "inHg", "FLOAT64", "inHg", "pascal", "inHg * 3386.389", 15000, 110000, "15000..110000"),
            new JsonFieldMapping("seaLevelPressurePascal", "SEA LEVEL PRESSURE", null, "millibars", "FLOAT64", "millibars", "pascal", "mbar * 100", 80000, 110000, "80000..110000"),
            new JsonFieldMapping("barometerSettingPascal", "KOHLSMAN SETTING MB:1", 1, "millibars", "FLOAT64", "millibars", "pascal", "mbar * 100", 80000, 110000, "80000..110000"),
            new JsonFieldMapping("autopilot.airspeedHoldMetersPerSecond", "AUTOPILOT AIRSPEED HOLD VAR", null, "knots", "FLOAT64", "knots", "m/s", "knots * 0.514444", 0, 250, "0..250"),
            new JsonFieldMapping("autopilot.machHoldMach", "AUTOPILOT MACH HOLD VAR", null, "mach", "FLOAT64", "mach", "mach", "identity", 0, 1.2, "0..1.2"),
            new JsonFieldMapping("autopilot.altitudeHoldMeters", "AUTOPILOT ALTITUDE LOCK VAR", null, "feet", "FLOAT64", "feet", "meters", "feet * 0.3048", null, null, "n/a"),
            new JsonFieldMapping("autopilot.altitudeArmMeters", "AUTOPILOT ALTITUDE LOCK VAR", null, "feet", "FLOAT64", "feet", "meters", "feet * 0.3048", null, null, "n/a"),
            new JsonFieldMapping("autopilot.headingLockDegrees", "AUTOPILOT HEADING LOCK DIR", null, "degrees", "FLOAT64", "degrees", "degrees", "normalize heading", 0, 360, "0..360"),
            new JsonFieldMapping("autopilot.pitchHoldDegrees", "AUTOPILOT PITCH HOLD REF", null, "radians", "FLOAT64", "radians", "degrees", "rad * 57.2957795", null, null, "n/a"),
            new JsonFieldMapping("autopilot.verticalSpeedHoldMetersPerSecond", "AUTOPILOT VERTICAL HOLD VAR", null, "feet per minute", "FLOAT64", "ft/min", "m/s", "ft/min * 0.00508", null, null, "n/a"),
            new JsonFieldMapping("com1", "COM ACTIVE FREQUENCY:1", 1, "Frequency BCD16", "FLOAT64", "BCD16", "MHz string", "decode BCD16", 118, 136.99, "118.00..136.99"),
            new JsonFieldMapping("com2", "COM ACTIVE FREQUENCY:2", 2, "Frequency BCD16", "FLOAT64", "BCD16", "MHz string", "decode BCD16", 118, 136.99, "118.00..136.99"),
            new JsonFieldMapping("nav1", "NAV ACTIVE FREQUENCY:1", 1, "MHz", "FLOAT64", "MHz", "MHz string", "identity format", 108, 117.95, "108.00..117.95"),
            new JsonFieldMapping("nav2", "NAV ACTIVE FREQUENCY:2", 2, "MHz", "FLOAT64", "MHz", "MHz string", "identity format", 108, 117.95, "108.00..117.95"),
            new JsonFieldMapping("fuelTanks[*].capacityKgs", "FUEL TANK * CAPACITY + FUEL WEIGHT PER GALLON", null, "gallons/pounds", "FLOAT64", "gallons + lb/gal", "kg", "gal * lb/gal * 0.45359237", 0, 300000, "0..300000"),
            new JsonFieldMapping("fuelTanks[*].percentageFilled", "FUEL TANK * QUANTITY / CAPACITY", null, "gallons", "FLOAT64", "gallons", "percent", "qty / cap * 100", 0, 100, "0..100")
        };

        private static readonly Dictionary<string, JsonFieldMapping> JsonFieldLookup =
            JsonFieldMappings.ToDictionary(mapping => mapping.JsonField, mapping => mapping, StringComparer.Ordinal);

        public static JsonFieldMapping FindJsonField(string jsonField)
        {
            JsonFieldMapping mapping;
            return JsonFieldLookup.TryGetValue(jsonField, out mapping) ? mapping : null;
        }

        public static void ValidateStructOrder<T>(IReadOnlyList<SimVarDefinition> definitions)
        {
            FieldInfo[] fields = typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public);

            if (fields.Length != definitions.Count)
            {
                throw new InvalidOperationException(
                    "Struct " + typeof(T).Name +
                    " field count " + fields.Length +
                    " does not match SimConnect definition count " + definitions.Count + ".");
            }

            for (int i = 0; i < fields.Length; i++)
            {
                if (!string.Equals(fields[i].Name, definitions[i].StructFieldName, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "Struct " + typeof(T).Name +
                        " field #" + (i + 1).ToString(CultureInfo.InvariantCulture) +
                        " is '" + fields[i].Name +
                        "' but SimConnect definition #" + (i + 1).ToString(CultureInfo.InvariantCulture) +
                        " expects '" + definitions[i].StructFieldName + "'.");
                }
            }
        }
    }

    public static class TelemetryMath
    {
        public static double FeetToMeters(double feet)
        {
            return IsFinite(feet) ? feet * 0.3048 : feet;
        }

        public static double MetersToFeet(double meters)
        {
            return IsFinite(meters) ? meters / 0.3048 : meters;
        }

        public static double KnotsToMetersPerSecond(double knots)
        {
            return IsFinite(knots) ? knots * 0.514444 : knots;
        }

        public static double MetersPerSecondToKnots(double metersPerSecond)
        {
            return IsFinite(metersPerSecond) ? metersPerSecond / 0.514444 : metersPerSecond;
        }

        public static double FeetPerSecondToMetersPerSecond(double feetPerSecond)
        {
            return IsFinite(feetPerSecond) ? feetPerSecond * 0.3048 : feetPerSecond;
        }

        public static double FeetPerMinuteToMetersPerSecond(double feetPerMinute)
        {
            return IsFinite(feetPerMinute) ? feetPerMinute * 0.00508 : feetPerMinute;
        }

        public static double RadiansToDegrees(double radians)
        {
            return IsFinite(radians) ? radians * (180.0 / Math.PI) : radians;
        }

        public static double MetersToKilometers(double meters)
        {
            return IsFinite(meters) ? meters / 1000.0 : meters;
        }

        public static double InchesHgToPascals(double inchesHg)
        {
            return IsFinite(inchesHg) ? inchesHg * 3386.389 : inchesHg;
        }

        public static double MillibarsToPascals(double millibars)
        {
            return IsFinite(millibars) ? millibars * 100.0 : millibars;
        }

        public static double GallonsToKilograms(double gallons, double poundsPerGallon)
        {
            if (!IsFinite(gallons) || gallons < 0 || !IsFinite(poundsPerGallon) || poundsPerGallon <= 0)
            {
                return double.NaN;
            }

            return gallons * poundsPerGallon * 0.45359237;
        }

        public static double QuantityToPercent(double quantityGallons, double capacityGallons)
        {
            if (!IsFinite(quantityGallons) || quantityGallons < 0 || !IsFinite(capacityGallons) || capacityGallons <= 0)
            {
                return double.NaN;
            }

            return (quantityGallons / capacityGallons) * 100.0;
        }

        public static string FormatNumeric(double value)
        {
            return IsFinite(value) ? value.ToString("G17", CultureInfo.InvariantCulture) : "null";
        }

        public static string FormatFrequency(double value)
        {
            if (!IsFinite(value) || value <= 0)
            {
                return null;
            }

            return value.ToString("0.00", CultureInfo.InvariantCulture);
        }

        public static string FormatComFrequencyBcd16(double value)
        {
            if (!IsFinite(value) || value <= 0)
            {
                return null;
            }

            int bcd = (int)Math.Round(value);
            int nibble1 = (bcd >> 12) & 0xF;
            int nibble2 = (bcd >> 8) & 0xF;
            int nibble3 = (bcd >> 4) & 0xF;
            int nibble4 = bcd & 0xF;

            double mhz = 100.0 + (nibble1 * 10.0) + nibble2 + (nibble3 / 10.0) + (nibble4 / 100.0);
            return mhz.ToString("0.00", CultureInfo.InvariantCulture);
        }

        public static double? ValidateNumeric(
            string jsonField,
            double rawValue,
            double convertedValue,
            IList<TelemetryRejectedValue> rejected,
            string reason = null)
        {
            JsonFieldMapping mapping = TelemetryBridgeCatalog.FindJsonField(jsonField);

            if (!IsFinite(convertedValue))
            {
                AddRejected(rejected, mapping, rawValue, convertedValue, reason ?? "not_finite");
                return null;
            }

            if (mapping != null)
            {
                if (mapping.MinValue.HasValue && convertedValue < mapping.MinValue.Value)
                {
                    AddRejected(rejected, mapping, rawValue, convertedValue, reason ?? "below_min");
                    return null;
                }

                if (mapping.MaxValue.HasValue && convertedValue > mapping.MaxValue.Value)
                {
                    AddRejected(rejected, mapping, rawValue, convertedValue, reason ?? "above_max");
                    return null;
                }
            }

            return convertedValue;
        }

        public static string ValidateFrequencyString(
            string jsonField,
            double rawValue,
            string formattedFrequency,
            IList<TelemetryRejectedValue> rejected)
        {
            if (string.IsNullOrWhiteSpace(formattedFrequency))
            {
                return null;
            }

            double numericValue;
            if (!double.TryParse(formattedFrequency, NumberStyles.Float, CultureInfo.InvariantCulture, out numericValue))
            {
                AddRejected(
                    rejected,
                    TelemetryBridgeCatalog.FindJsonField(jsonField),
                    rawValue,
                    double.NaN,
                    "invalid_frequency_format");
                return null;
            }

            double? validated = ValidateNumeric(jsonField, rawValue, numericValue, rejected);
            return validated.HasValue ? formattedFrequency : null;
        }

        public static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private static void AddRejected(
            IList<TelemetryRejectedValue> rejected,
            JsonFieldMapping mapping,
            double rawValue,
            double convertedValue,
            string reason)
        {
            rejected.Add(new TelemetryRejectedValue
            {
                JsonField = mapping != null ? mapping.JsonField : null,
                SimVarName = mapping != null ? mapping.SimVarName : null,
                RequestedUnit = mapping != null ? mapping.SimConnectUnit : null,
                RawValue = FormatNumeric(rawValue),
                ConvertedValue = FormatNumeric(convertedValue),
                SanityRange = mapping != null ? mapping.SanityRange : null,
                Reason = reason,
                Action = "null"
            });
        }
    }
}
