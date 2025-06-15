using Location_Tracker.Models;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Location_Tracker.Services
{
    public class DatabaseService
    {
        private SQLiteAsyncConnection _database;

        public DatabaseService()
        {
            InitializeDatabaseAsync();
        }

        private async Task InitializeDatabaseAsync()
        {
            if (_database != null)
                return;

            var databasePath = Path.Combine(FileSystem.AppDataDirectory, "locations.db");
            _database = new SQLiteAsyncConnection(databasePath);

            await _database.CreateTableAsync<LocationPoint>();
        }

        public async Task<List<LocationPoint>> GetAllLocationsAsync()
        {
            await InitializeDatabaseAsync();
            return await _database.Table<LocationPoint>().ToListAsync();
        }

        public async Task<int> SaveLocationAsync(LocationPoint location)
        {
            await InitializeDatabaseAsync();
            return await _database.InsertAsync(location);
        }

        public async Task<int> DeleteLocationAsync(LocationPoint location)
        {
            await InitializeDatabaseAsync();
            return await _database.DeleteAsync(location);
        }

        public async Task<int> ClearAllLocationsAsync()
        {
            await InitializeDatabaseAsync();
            return await _database.DeleteAllAsync<LocationPoint>();
        }

        public async Task<List<LocationPoint>> GetLocationsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            await InitializeDatabaseAsync();
            return await _database.Table<LocationPoint>()
                .Where(l => l.Timestamp >= startDate && l.Timestamp <= endDate)
                .ToListAsync();
        }
    }
}
