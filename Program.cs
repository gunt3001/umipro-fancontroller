using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace umipro_fancontroller
{
    class Program
    {
        const int DefaultCheckIntervalSeconds = 5;
        const string DefaultTempSensorIdentifier = "TC0E";
        const int DefaultBaseTemperature = 50000;
        const int DefaultMaxTemperature = 80000;

        const string SensorsPath = "/sensors";

        static void Main(string[] args)
        {
            // Check scan interval
            var intervalSetting = Environment.GetEnvironmentVariable("UMIPRO_FANCONTROLLER_CHECK_INTERVAL");
            var interval = DefaultCheckIntervalSeconds;
            if (intervalSetting != null) {
                interval = int.Parse(intervalSetting);
            }
            Console.WriteLine($"Interval set as {interval} seconds");

            // Check temp sensor
            var sensorIdentifierSetting = Environment.GetEnvironmentVariable("UMIPRO_FANCONTROLLER_TEMP_SENSOR_IDENTIFIER");
            var sensorIdentifier = DefaultTempSensorIdentifier;
            if (sensorIdentifierSetting != null) {
                sensorIdentifier = sensorIdentifierSetting;
            }
            // Look for corresponding file
            var tempSensorFile = Directory
                .EnumerateFiles(SensorsPath, "temp*_label")
                .First(x => ReadValue(x) == sensorIdentifier)
                .Replace("_label", "_input");
            Console.WriteLine($"Using temperature sensor with identifier {sensorIdentifier} at file {tempSensorFile}");

            // Get Temperature Range
            var baseTempSetting = Environment.GetEnvironmentVariable("UMIPRO_FANCONTROLLER_BASE_TEMP");
            var maxTempSetting = Environment.GetEnvironmentVariable("UMIPRO_FANCONTROLLER_MAX_TEMP");
            var baseTemp = DefaultBaseTemperature;
            var maxTemp = DefaultMaxTemperature;
            if (baseTempSetting != null) {
                baseTemp = int.Parse(baseTempSetting);
            }
            if (maxTempSetting != null) {
                maxTemp = int.Parse(maxTempSetting);
            }
            Console.WriteLine($"Temperature range is {baseTemp} - {maxTemp}");

            // Get fan default values
            var fan1Base = ReadIntValue(Path.Combine(SensorsPath, "fan1_min"));
            var fan1Max = ReadIntValue(Path.Combine(SensorsPath, "fan1_max"));
            var fan2Base = ReadIntValue(Path.Combine(SensorsPath, "fan1_min"));
            var fan2Max = ReadIntValue(Path.Combine(SensorsPath, "fan2_max"));
            Console.WriteLine($"Fan 1 speed range is {fan1Base} to {fan1Max}");
            Console.WriteLine($"Fan 2 speed range is {fan2Base} to {fan2Max}");

            Console.WriteLine("Starting monitoring...");

            Console.WriteLine("Enabling manual fan control");
            WriteValue("fan1_manual", 1);
            WriteValue("fan2_manual", 1);      

            while (true) {
                var temp = ReadIntValue(tempSensorFile);
                var fan1rpm = GetFanSpeed(fan1Base, fan1Max, baseTemp, maxTemp, temp);
                var fan2rpm = GetFanSpeed(fan2Base, fan2Max, baseTemp, maxTemp, temp);
                WriteValue("fan1_output", fan1rpm);
                WriteValue("fan2_output", fan2rpm);
                Console.WriteLine($"T:{temp} F1:{fan1rpm} F2:{fan2rpm}");
                Thread.Sleep(TimeSpan.FromSeconds(5));
            }
        }

        private static string ReadValue(string filePath) {
            return File.ReadLines(filePath).First();
        }

        private static int ReadIntValue(string filePath) {
            return int.Parse(ReadValue(filePath));
        }

        private static void WriteValue(string filePath, int value){
            File.WriteAllText(filePath, value.ToString());
        }

        private static int GetFanSpeed(int baseRpm, int maxRpm, int baseTemp, int maxTemp, int currentTemp) {
            if (currentTemp <= baseTemp) {
                return baseRpm;
            }
            else if (currentTemp >= maxTemp) {
                return maxRpm;
            }
            else {
                var tempRange = maxTemp - baseTemp;
                var tempFromBase = currentTemp - baseTemp;
                var tempScale = (float)tempFromBase / tempRange;
                var rpmRange = maxRpm - baseRpm;
                return baseRpm + Convert.ToInt32(tempScale * rpmRange);
            }
        }
    }
}
