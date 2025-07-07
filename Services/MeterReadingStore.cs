using Microsoft.Data.Sqlite;

namespace EcoguardPoller.Services
{
    internal class MeterReadingStore
    {
        private readonly string _dbPath;

        public MeterReadingStore(string dbPath)
        {
            Console.WriteLine($"[DEBUG] Current Directory: {Directory.GetCurrentDirectory()}");
            Console.WriteLine($"[DEBUG] DB Path Configured: {dbPath}");
            Console.WriteLine($"[DEBUG] Full DB Path: {Path.GetFullPath(dbPath)}");

            _dbPath = dbPath;
            EnsureDatabase();
        }

        private void EnsureDatabase()
        {
            using var conn = new SqliteConnection($"Data Source={_dbPath}");
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS meter_readings (
    timestamp INTEGER PRIMARY KEY,
    reading_kwh REAL
)";
            cmd.ExecuteNonQuery();
        }

        public double? GetLastReading()
        {
            using var conn = new SqliteConnection($"Data Source={_dbPath}");
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT reading_kwh FROM meter_readings ORDER BY timestamp DESC LIMIT 1";
            var result = cmd.ExecuteScalar();
            return result == null ? null : Convert.ToDouble(result);
        }

        public void SaveReading(int timestamp, double value)
        {
            using var conn = new SqliteConnection($"Data Source={_dbPath}");
            conn.Open();

            var cmd = conn.CreateCommand();

            cmd.CommandText = @"
        INSERT OR REPLACE INTO meter_readings (timestamp, reading_kwh)
        VALUES ($ts, $val)"; //if timestamp already exist, so update with latest data.

            cmd.Parameters.AddWithValue("$ts", timestamp);
            cmd.Parameters.AddWithValue("$val", value);
            cmd.ExecuteNonQuery();
        }
    }
}
