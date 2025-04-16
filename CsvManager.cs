using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace SimpleInventoryApp
{
    public class CsvManager
    {
        private readonly DataStorage _dataStorage;

        public CsvManager(DataStorage dataStorage)
        {
            _dataStorage = dataStorage;
        }

        public void ExportToCsv(string filePath, bool includeUtf8Bom = true)
        {
            try
            {
                var items = _dataStorage.LoadItems();
                // Create a UTF8 encoding with or without BOM as requested
                var encoding = includeUtf8Bom ? new UTF8Encoding(true) : new UTF8Encoding(false);
                using var writer = new StreamWriter(filePath, false, encoding);
                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    Delimiter = ",",
                    Encoding = encoding
                };
                using var csv = new CsvWriter(writer, config);
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
                using var reader = new StreamReader(filePath, Encoding.UTF8);
                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    Delimiter = ",",
                    Encoding = Encoding.UTF8,
                    BadDataFound = null, // Ignore bad data
                    HeaderValidated = null, // Don't validate headers
                    MissingFieldFound = null // Don't throw on missing fields
                };
                using var csv = new CsvReader(reader, config);
                
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

        public void ImportFromExternalCsv(string filePath)
        {
            try
            {
                using var reader = new StreamReader(filePath, Encoding.UTF8);
                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    Delimiter = ",",
                    Encoding = Encoding.UTF8,
                    BadDataFound = null,
                    HeaderValidated = null,
                    MissingFieldFound = null
                };
                
                using var csv = new CsvReader(reader, config);
                
                // Register a custom DateTime converter to handle possible format differences
                csv.Context.TypeConverterCache.AddConverter<DateTime>(new DateTimeConverter());
                
                // Create a mapping from the CSV fields to our InventoryItem properties
                csv.Context.RegisterClassMap<InventoryItemMap>();
                
                var records = csv.GetRecords<InventoryItem>().ToList();
                
                if (records.Count == 0)
                {
                    throw new Exception("No records found in the CSV file or all records were invalid.");
                }
                
                // Load existing items to potentially merge data
                var existingItems = _dataStorage.LoadItems();
                
                // Use existing IDs if possible, otherwise assign new ones
                var maxId = existingItems.Any() ? existingItems.Max(i => i.Id) : 0;
                foreach (var item in records)
                {
                    // If ID is not set or is invalid, assign a new one
                    if (item.Id <= 0)
                    {
                        item.Id = ++maxId;
                    }
                    
                    // Ensure LastUpdated is set
                    if (item.LastUpdated == DateTime.MinValue)
                    {
                        item.LastUpdated = DateTime.Now;
                    }
                    
                    // Ensure other required fields have at least empty values
                    item.InventoryNumber = item.InventoryNumber ?? string.Empty;
                    item.Name = item.Name ?? string.Empty;
                    item.Description = item.Description ?? string.Empty;
                    item.Location = item.Location ?? string.Empty;
                }
                
                // Save the imported items
                _dataStorage.SaveItems(records);
                
                // Update locations if needed
                var locations = new HashSet<string>(_dataStorage.LoadLocations());
                var newLocations = records
                    .Select(r => r.Location)
                    .Where(l => !string.IsNullOrEmpty(l))
                    .Distinct();
                locations.UnionWith(newLocations);
                _dataStorage.SaveLocations(locations.ToList());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error importing from external CSV: {ex.Message}");
                throw;
            }
        }
    }
    
    // Define a custom mapping for CSV fields to InventoryItem properties
    public sealed class InventoryItemMap : ClassMap<InventoryItem>
    {
        public InventoryItemMap()
        {
            Map(m => m.Id).Name("Id");
            Map(m => m.InventoryNumber).Name("InventoryNumber");
            Map(m => m.Name).Name("Name");
            Map(m => m.Description).Name("Description");
            Map(m => m.Quantity).Name("Quantity");
            Map(m => m.Location).Name("Location");
            Map(m => m.LastUpdated).Name("LastUpdated");
        }
    }
    
    // Custom DateTime converter that's more flexible with formats
    public class DateTimeConverter : ITypeConverter
    {
        public object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
        {
            if (string.IsNullOrEmpty(text))
                return DateTime.Now;
                
            // Try to parse with different formats
            if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result))
                return result;
                
            // If parsing fails, return current date/time
            return DateTime.Now;
        }
        
        public string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
        {
            return value is DateTime dateTime 
                ? dateTime.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture) 
                : string.Empty;
        }
    }
} 