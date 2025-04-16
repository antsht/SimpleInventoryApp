using System;
using System.Collections.Generic;
using System.Diagnostics; // Required for Process
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
            // Get the directory of the currently running executable
            string exePath = Process.GetCurrentProcess().MainModule?.FileName ?? AppDomain.CurrentDomain.BaseDirectory;
            string baseDir = Path.GetDirectoryName(exePath) ?? AppDomain.CurrentDomain.BaseDirectory;

            // If running in debug/dev environment, the path might be in bin/Debug/netX.Y
            // Consider adjusting the path if needed for development vs published scenarios,
            // but for published EXE, this baseDir should be correct.
            Console.WriteLine($"Using base directory for data: {baseDir}");
            
            _inventoryFilePath = Path.Combine(baseDir, inventoryFile);
            _locationsFilePath = Path.Combine(baseDir, locationsFile);
            
            Console.WriteLine($"Inventory file path: {_inventoryFilePath}");
            Console.WriteLine($"Locations file path: {_locationsFilePath}");
            
            // Ensure the directory exists (important for first run of published app)
            try 
            {
                if (!string.IsNullOrEmpty(baseDir))
                {
                    Directory.CreateDirectory(baseDir);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating data directory '{baseDir}': {ex.Message}");
                // Optionally handle this error more gracefully, e.g., disable saving/loading
            }
        }

        // --- Inventory Item Methods (Unchanged) ---
        public List<InventoryItem> LoadItems()
        {
            if (!File.Exists(_inventoryFilePath))
            {
                Console.WriteLine($"Inventory file does not exist at {_inventoryFilePath}");
                return new List<InventoryItem>();
            }
            try
            {
                string jsonString = File.ReadAllText(_inventoryFilePath);
                if (string.IsNullOrWhiteSpace(jsonString)) 
                {
                    Console.WriteLine("Inventory file is empty");
                    return new List<InventoryItem>();
                }
                
                var items = JsonSerializer.Deserialize<List<InventoryItem>>(jsonString);
                Console.WriteLine($"Loaded {items?.Count ?? 0} items from inventory file");
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
                Console.WriteLine($"Saving {items.Count} items to {_inventoryFilePath}");
                var options = new JsonSerializerOptions { WriteIndented = true };
                string jsonString = JsonSerializer.Serialize(items, options);
                
                // Create directory if it doesn't exist (just to be sure)
                string? directory = Path.GetDirectoryName(_inventoryFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                File.WriteAllText(_inventoryFilePath, jsonString);
                Console.WriteLine($"Items saved successfully to {_inventoryFilePath}");
            }
            catch (Exception ex) // Catch broader exceptions for saving
            {
                Console.WriteLine($"Error saving inventory to {_inventoryFilePath}: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
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
                Console.WriteLine($"Locations file does not exist at {_locationsFilePath}");
                return new List<string>();
            }

            try
            {
                string jsonString = File.ReadAllText(_locationsFilePath);
                if (string.IsNullOrWhiteSpace(jsonString))
                {
                    Console.WriteLine("Locations file is empty");
                    return new List<string>();
                }
                    
                // Locations are just a list of strings
                var locations = JsonSerializer.Deserialize<List<string>>(jsonString);
                Console.WriteLine($"Loaded {locations?.Count ?? 0} locations from file");
                return locations ?? new List<string>();
            }
            catch (Exception ex) // Catch broader exceptions
            {
                Console.WriteLine($"Error loading locations from {_locationsFilePath}: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                // Return an empty list or default list on error
                return new List<string>();
            }
        }

        public void SaveLocations(List<string> locations)
        {
            try
            {
                Console.WriteLine($"Saving {locations.Count} locations to {_locationsFilePath}");
                var options = new JsonSerializerOptions { WriteIndented = true };
                // Ensure sorting for consistency (optional but nice)
                string jsonString = JsonSerializer.Serialize(locations.OrderBy(l => l).ToList(), options);
                
                // Create directory if it doesn't exist (just to be sure)
                string? directory = Path.GetDirectoryName(_locationsFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                File.WriteAllText(_locationsFilePath, jsonString);
                Console.WriteLine($"Locations saved successfully to {_locationsFilePath}");
            }
            catch (Exception ex) // Catch broader exceptions
            {
                Console.WriteLine($"Error saving locations to {_locationsFilePath}: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}