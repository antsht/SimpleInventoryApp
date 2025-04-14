using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace SimpleInventoryApp
{
    public class CsvManager
    {
        private readonly DataStorage _dataStorage;

        public CsvManager(DataStorage dataStorage)
        {
            _dataStorage = dataStorage;
        }

        public void ExportToCsv(string filePath)
        {
            try
            {
                var items = _dataStorage.LoadItems();
                using var writer = new StreamWriter(filePath);
                using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
                csv.WriteRecords(items);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error exporting to CSV: {ex.Message}");
                throw;
            }
        }

        public void ImportFromCsv(string filePath)
        {
            try
            {
                using var reader = new StreamReader(filePath);
                using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
                
                var records = csv.GetRecords<InventoryItem>().ToList();
                
                // Load existing items to get the next available ID
                var existingItems = _dataStorage.LoadItems();
                int nextId = _dataStorage.GetNextId(existingItems);

                // Assign new IDs to imported items
                foreach (var item in records)
                {
                    item.Id = nextId++;
                    item.LastUpdated = DateTime.Now;
                }

                // Save the imported items
                _dataStorage.SaveItems(records);

                // Update locations if needed
                var locations = new HashSet<string>(_dataStorage.LoadLocations());
                var newLocations = records.Select(r => r.Location).Where(l => !string.IsNullOrEmpty(l)).Distinct();
                locations.UnionWith(newLocations);
                _dataStorage.SaveLocations(locations.ToList());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error importing from CSV: {ex.Message}");
                throw;
            }
        }
    }
} 