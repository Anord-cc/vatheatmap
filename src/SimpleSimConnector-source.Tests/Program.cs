using System;
using System.Collections.Generic;
using SimpleSimConnector;

namespace SimpleSimConnectorSourceTests
{
    internal static class Program
    {
        private static int failures;

        private static void Main()
        {
            Run("Knots convert to meters per second", TestKnotsToMetersPerSecond);
            Run("Feet convert to meters", TestFeetToMeters);
            Run("Pressure conversions are correct", TestPressureConversions);
            Run("BCD16 COM frequency decodes correctly", TestComFrequencyDecode);
            Run("Sanity validation rejects impossible sample values", TestSanityValidation);
            Run("Bad sample replay is rejected where expected", TestBadSampleReplay);

            if (failures > 0)
            {
                Console.Error.WriteLine("FAILED: " + failures);
                Environment.Exit(1);
            }

            Console.WriteLine("All telemetry bridge tests passed.");
        }

        private static void TestKnotsToMetersPerSecond()
        {
            AssertNear(TelemetryMath.KnotsToMetersPerSecond(100.0), 51.4444, 0.0001, "100kt");
            AssertNear(TelemetryMath.FeetPerMinuteToMetersPerSecond(1000.0), 5.08, 0.00001, "1000fpm");
            AssertNear(TelemetryMath.FeetPerSecondToMetersPerSecond(10.0), 3.048, 0.00001, "10ft/s");
        }

        private static void TestFeetToMeters()
        {
            AssertNear(TelemetryMath.FeetToMeters(1000.0), 304.8, 0.00001, "1000ft");
            AssertNear(TelemetryMath.MetersToFeet(304.8), 1000.0, 0.0001, "304.8m");
            AssertNear(TelemetryMath.RadiansToDegrees(Math.PI / 2.0), 90.0, 0.00001, "pi/2");
        }

        private static void TestPressureConversions()
        {
            AssertNear(TelemetryMath.MillibarsToPascals(1013.25), 101325.0, 0.001, "1013.25mbar");
            AssertNear(TelemetryMath.InchesHgToPascals(29.92), 101320.73888, 0.1, "29.92inHg");
            AssertNear(TelemetryMath.MetersToKilometers(5000.0), 5.0, 0.00001, "5000m");
        }

        private static void TestComFrequencyDecode()
        {
            AssertEqual(TelemetryMath.FormatComFrequencyBcd16(0x2150), "121.50", "com1");
            AssertEqual(TelemetryMath.FormatFrequency(113.90), "113.90", "nav1");
        }

        private static void TestSanityValidation()
        {
            var rejected = new List<TelemetryRejectedValue>();

            AssertNull(TelemetryMath.ValidateNumeric("outsideAirTemperatureCelsius", 10330, 10330, rejected), "temperature");
            AssertNull(TelemetryMath.ValidateNumeric("ambientPressurePascal", 344372839.51410526, 344372839.51410526, rejected), "ambient pressure");
            AssertNull(TelemetryMath.ValidateNumeric("seaLevelPressurePascal", 1013.25, 1013.25, rejected), "sea level pressure");
            AssertNull(TelemetryMath.ValidateNumeric("barometerSettingPascal", -2347.185437299946, -2347.185437299946, rejected), "barometer");
            AssertNull(TelemetryMath.ValidateNumeric("autopilot.machHoldMach", 21000, 21000, rejected), "mach");
            AssertNull(TelemetryMath.ValidateNumeric("fuelTanks[*].capacityKgs", 10080180.691482533, 10080180.691482533, rejected), "fuel capacity");
            AssertNull(TelemetryMath.ValidateFrequencyString("nav2", 2000.0, "2000.00", rejected), "nav2");

            if (rejected.Count < 7)
            {
                throw new Exception("Expected at least 7 rejected values, got " + rejected.Count + ".");
            }
        }

        private static void TestBadSampleReplay()
        {
            var rejected = new List<TelemetryRejectedValue>();

            double? latitude = TelemetryMath.ValidateNumeric("latitude", 25.794172087766341, 25.794172087766341, rejected);
            double? longitude = TelemetryMath.ValidateNumeric("longitude", -80.284599810375283, -80.284599810375283, rejected);
            double? heading = TelemetryMath.ValidateNumeric("headingTrueDegrees", 38.750382290200008, 38.750382290200008, rejected);
            string com1 = TelemetryMath.ValidateFrequencyString("com1", 0x2150, "121.50", rejected);
            string com2 = TelemetryMath.ValidateFrequencyString("com2", 100.72, "100.72", rejected);

            AssertTrue(latitude.HasValue, "latitude accepted");
            AssertTrue(longitude.HasValue, "longitude accepted");
            AssertTrue(heading.HasValue, "heading accepted");
            AssertEqual(com1, "121.50", "com1 accepted");
            AssertNull(com2, "com2 rejected");
        }

        private static void Run(string name, Action test)
        {
            try
            {
                test();
                Console.WriteLine("PASS: " + name);
            }
            catch (Exception ex)
            {
                failures++;
                Console.Error.WriteLine("FAIL: " + name + " - " + ex.Message);
            }
        }

        private static void AssertNear(double actual, double expected, double tolerance, string label)
        {
            if (double.IsNaN(actual) || Math.Abs(actual - expected) > tolerance)
            {
                throw new Exception(label + " expected " + expected + " got " + actual);
            }
        }

        private static void AssertEqual(string actual, string expected, string label)
        {
            if (!string.Equals(actual, expected, StringComparison.Ordinal))
            {
                throw new Exception(label + " expected " + expected + " got " + (actual ?? "null"));
            }
        }

        private static void AssertNull(object value, string label)
        {
            if (value != null)
            {
                throw new Exception(label + " expected null but got " + value);
            }
        }

        private static void AssertTrue(bool condition, string label)
        {
            if (!condition)
            {
                throw new Exception(label + " expected true");
            }
        }
    }
}
