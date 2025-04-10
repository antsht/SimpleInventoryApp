using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace SimpleInventoryApp
{
    public class DataStorage
    {
        private readonly string _inventoryFilePath;
        private readonly string _locationsFilePath; // New field for locations file

        public DataStorage(string inventoryFile = "inventory.json", string locationsFile = "locations.json") // Add locations filename
        {
            _inventoryFilePath = Path.Combine(AppContext.BaseDirectory, inventoryFile);
            _locationsFilePath = Path.Combine(AppContext.BaseDirectory, locationsFile); // Initialize path
        }

        // --- Inventory Item Methods (Unchanged) ---
        public List<InventoryItem> LoadItems()
        {
            if (!File.Exists(_inventoryFilePath))
            {
                return new List<InventoryItem>();
            }
            try
            {
                string jsonString = File.ReadAllText(_inventoryFilePath);
                if (string.IsNullOrWhiteSpace(jsonString)) return new List<InventoryItem>();
                var items = JsonSerializer.Deserialize<List<InventoryItem>>(jsonString);
                return items ?? new List<InventoryItem>();
            }
            catch (Exception ex) // Catch broader exceptions for loading
            {
                Console.WriteLine($"Error loading inventory from {_inventoryFilePath}: {ex.Message}");
                return new List<InventoryItem>();
            }
        }

        public void SaveItems(List<InventoryItem> items)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string jsonString = JsonSerializer.Serialize(items, options);
                File.WriteAllText(_inventoryFilePath, jsonString);
            }
            catch (Exception ex) // Catch broader exceptions for saving
            {
                Console.WriteLine($"Error saving inventory to {_inventoryFilePath}: {ex.Message}");
            }
        }

        public int GetNextId(List<InventoryItem> items)
        {
            if (items == null || !items.Any()) return 1;
            return items.Max(item => item.Id) + 1;
        }

        // --- New Location Methods ---

        public List<string> LoadLocations()
        {
            if (!File.Exists(_locationsFilePath))
            {
                // Create a default list if the file doesn't exist
                // return new List<string> { "Supply Closet", "IT Room", "Reception Desk" };
                return new List<string>(); // Or just start empty
            }

            try
            {
                string jsonString = File.ReadAllText(_locationsFilePath);
                if (string.IsNullOrWhiteSpace(jsonString)) return new List<string>();
                // Locations are just a list of strings
                var locations = JsonSerializer.Deserialize<List<string>>(jsonString);
                return locations ?? new List<string>();
            }
            catch (Exception ex) // Catch broader exceptions
            {
                Console.WriteLine($"Error loading locations from {_locationsFilePath}: {ex.Message}");
                // Return an empty list or default list on error
                return new List<string>();
            }
        }

        public void SaveLocations(List<string> locations)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                // Ensure sorting for consistency (optional but nice)
                string jsonString = JsonSerializer.Serialize(locations.OrderBy(l => l).ToList(), options);
                File.WriteAllText(_locationsFilePath, jsonString);
            }
            catch (Exception ex) // Catch broader exceptions
            {
                Console.WriteLine($"Error saving locations to {_locationsFilePath}: {ex.Message}");
            }
        }
    }
}