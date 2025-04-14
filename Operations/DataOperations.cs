using System;
using System.Collections.Generic;
using System.IO;
using Terminal.Gui;
using SimpleInventoryApp.UI;

namespace SimpleInventoryApp.Operations
{
    public static class DataOperations
    {
        private static List<InventoryItem> inventory = new List<InventoryItem>();
        private static List<string> locations = new List<string>();
        private static DataStorage dataStorage = null!;
        private static CsvManager csvManager = null!;
        
        public static void Initialize(
            List<InventoryItem> inventoryItems, 
            List<string> locationItems, 
            DataStorage storage,
            CsvManager csv)
        {
            inventory = inventoryItems;
            locations = locationItems;
            dataStorage = storage;
            csvManager = csv;
        }
        
        public static void SaveData()
        {
            try
            {
                UserInterface.UpdateStatus("Saving data...");
                dataStorage.SaveItems(inventory);
                dataStorage.SaveLocations(locations);
                UserInterface.SetHasUnsavedChanges(false); // Reset flag
                UserInterface.UpdateStatus("Data saved successfully.");
                // Optional: Show a brief confirmation message?
                // UserInterface.ShowMessage("Save", "Data saved successfully.");
            }
            catch (Exception ex)
            {
                UserInterface.ShowMessage("Save Error", $"Failed to save data: {ex.Message}");
                UserInterface.UpdateStatus("Save data failed.");
            }
        }

        public static void RestoreData()
        {
            bool confirmed = UserInterface.ConfirmAction("Confirm Restore", "Reload data from disk?\nThis will discard any unsaved changes.");

            if (confirmed)
            {
                try
                {
                    UserInterface.UpdateStatus("Restoring data from disk...");
                    inventory = dataStorage.LoadItems() ?? new List<InventoryItem>();
                    locations = dataStorage.LoadLocations() ?? new List<string>();
                    
                    UserInterface.SetHasUnsavedChanges(false); // Reset flag
                    InventoryTable.Initialize(inventory); // Update the inventory table reference
                    InventoryTable.PopulateInventoryTable(InventoryOperations.ApplySortAndFilter()); // Refresh view with restored data + sort/filter
                    UserInterface.UpdateStatus("Data restored successfully from disk.");
                }
                catch (Exception ex)
                {
                    UserInterface.ShowMessage("Restore Error", $"Failed to restore data: {ex.Message}");
                    UserInterface.UpdateStatus("Restore data failed.");
                }
            }
            else
            {
                UserInterface.UpdateStatus("Restore data cancelled.");
            }
        }
        
        public static void ExportToCsv()
        {
            // Create a dialog for CSV export
            var dialog = new Dialog("Export to CSV", 60, 9);
            
            var pathLabel = new Label("Export Path:") { X = 1, Y = 1 };
            var pathText = new TextField(Path.Combine(Environment.CurrentDirectory, "export.csv")) 
            { 
                X = 15, 
                Y = 1, 
                Width = Dim.Fill(2) 
            };
            
            var helpLabel = new Label("Export will save all inventory items to a CSV file") 
            { 
                X = 1, 
                Y = 3, 
                Width = Dim.Fill(2) 
            };
            
            dialog.Add(pathLabel, pathText, helpLabel);
            
            // Add buttons
            var btnContainer = new View() { X = 0, Y = Pos.Bottom(dialog) - 5, Width = Dim.Fill(), Height = 1 };
            
            var exportButton = new Button("Export") { X = Pos.Center() - 10 };
            exportButton.Clicked += () => {
                string path = pathText.Text.ToString();
                if (string.IsNullOrWhiteSpace(path))
                {
                    UserInterface.ShowMessage("Invalid Path", "Please enter a valid export path.");
                    return;
                }
                
                try
                {
                    // Export the data
                    csvManager.ExportToCsv(path);
                    Application.RequestStop(); // Close the dialog
                    UserInterface.ShowMessage("Export Complete", $"Export successful to: {path}");
                    UserInterface.UpdateStatus($"Data exported to CSV: {Path.GetFileName(path)}");
                }
                catch (Exception ex)
                {
                    UserInterface.ShowMessage("Export Error", $"Failed to export: {ex.Message}");
                    UserInterface.UpdateStatus("Export to CSV failed.");
                }
            };
            
            var cancelButton = new Button("Cancel") { X = Pos.Center() + 2 };
            cancelButton.Clicked += () => { Application.RequestStop(); };
            
            btnContainer.Add(exportButton, cancelButton);
            dialog.Add(btnContainer);
            
            // Set focus to the path field
            pathText.SetFocus();
            
            // Run the dialog
            Application.Run(dialog);
        }
        
        public static void ImportFromCsv()
        {
            // Create a dialog for CSV import
            var dialog = new Dialog("Import from CSV", 60, 9);
            
            var pathLabel = new Label("Import Path:") { X = 1, Y = 1 };
            var pathText = new TextField(Path.Combine(Environment.CurrentDirectory, "import.csv")) 
            { 
                X = 15, 
                Y = 1, 
                Width = Dim.Fill(2) 
            };
            
            var helpLabel = new Label("Import will merge CSV data with existing inventory") 
            { 
                X = 1, 
                Y = 3, 
                Width = Dim.Fill(2) 
            };
            
            dialog.Add(pathLabel, pathText, helpLabel);
            
            // Add buttons
            var btnContainer = new View() { X = 0, Y = Pos.Bottom(dialog) - 5, Width = Dim.Fill(), Height = 1 };
            
            var importButton = new Button("Import") { X = Pos.Center() - 10 };
            importButton.Clicked += () => {
                string filePath = pathText.Text?.ToString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                {
                    UserInterface.ShowMessage("Invalid File", "Please enter a valid CSV file path that exists.");
                    return;
                }
                
                bool confirm = UserInterface.ConfirmAction("Confirm Import", 
                    "Importing will overwrite current unsaved data. Continue?");
                
                if (confirm)
                {
                    try
                    {
                        csvManager.ImportFromCsv(filePath);
                        inventory = dataStorage.LoadItems() ?? new List<InventoryItem>();
                        locations = dataStorage.LoadLocations() ?? new List<string>();

                        UserInterface.SetHasUnsavedChanges(false); // Data on disk IS current after import
                        InventoryTable.Initialize(inventory); // Update the inventory table reference
                        InventoryTable.PopulateInventoryTable(InventoryOperations.ApplySortAndFilter()); // Refresh view with imported data + sort/filter
                        
                        UserInterface.ShowMessage("Import Complete", $"Import process finished from '{Path.GetFileName(filePath)}'. Check table for results.");
                        UserInterface.UpdateStatus("Import process complete.");
                    }
                    catch (Exception ex)
                    {
                        UserInterface.ShowMessage("Import Error", $"Failed to import: {ex.Message}");
                        UserInterface.UpdateStatus("Import from CSV failed.");
                    }
                }
            };
            
            var cancelButton = new Button("Cancel") { X = Pos.Center() + 2 };
            cancelButton.Clicked += () => { Application.RequestStop(); };
            
            btnContainer.Add(importButton, cancelButton);
            dialog.Add(btnContainer);
            
            // Set focus to the path field
            pathText.SetFocus();
            
            // Run the dialog
            Application.Run(dialog);
        }
    }
} 