using System;
using System.Collections.Generic;
using System.IO;
using Terminal.Gui;
using SimpleInventoryApp.UI;
using NStack;

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
                Console.WriteLine("DataOperations.SaveData: Saving through ApplicationService...");
                
                // First, update the shared collections in case they changed
                SimpleInventoryApp.Services.ApplicationService.Instance.InventoryService.GetInventory().Clear();
                foreach (var item in inventory)
                {
                    SimpleInventoryApp.Services.ApplicationService.Instance.InventoryService.GetInventory().Add(item);
                }
                
                // Also sync locations to the LocationService
                var locationService = SimpleInventoryApp.Services.ApplicationService.Instance.LocationService;
                var serviceLocations = locationService.GetLocations();
                serviceLocations.Clear();
                foreach (var location in locations)
                {
                    serviceLocations.Add(location);
                }
                
                // Save through ApplicationService to ensure proper state management
                SimpleInventoryApp.Services.ApplicationService.Instance.SaveAllData();
                
                UserInterface.SetHasUnsavedChanges(false); // Reset flag
                UserInterface.UpdateStatus("Data saved successfully.");
                Console.WriteLine("DataOperations.SaveData: Data saved successfully");
            }
            catch (Exception ex)
            {
                UserInterface.ShowMessage("Save Error", $"Failed to save data: {ex.Message}");
                UserInterface.UpdateStatus("Save data failed.");
                Console.WriteLine($"DataOperations.SaveData Error: {ex.Message}");
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
                    
                    // Load into temporary lists first
                    var loadedItems = dataStorage.LoadItems() ?? new List<InventoryItem>(); 
                    var loadedLocations = dataStorage.LoadLocations() ?? new List<string>(); 
                    
                    // Clear the *existing* shared lists
                    inventory.Clear();
                    locations.Clear();
                    
                    // Add the loaded data into the shared lists
                    inventory.AddRange(loadedItems);
                    locations.AddRange(loadedLocations);
                    
                    UserInterface.SetHasUnsavedChanges(false); // Reset flag
                    
                    // Force a complete UI refresh
                    InventoryTable.Initialize(inventory); // Ensure table references the (now updated) shared list
                    
                    // Apply any active sort/filter
                    var displayItems = InventoryOperations.ApplySortAndFilter();
                    
                    // Force refresh of the table with the filtered data
                    InventoryTable.PopulateInventoryTable(displayItems);
                    
                    // Force a refresh of the UI
                    Terminal.Gui.Application.Refresh();
                    
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
            var dialog = new Dialog("Export to CSV", 60, 11);
            
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
            
            // Add checkbox for UTF-8 BOM
            var bomCheckbox = new CheckBox("Include UTF-8 BOM (helps with Excel/other apps)", true) 
            { 
                X = 1, 
                Y = 5,
                Width = Dim.Fill(2)
            };
            
            dialog.Add(pathLabel, pathText, helpLabel, bomCheckbox);
            
            // Add buttons using dialog's built-in functionality
            var exportButton = new Button("Export");
            exportButton.Clicked += () => {
                string path = pathText.Text?.ToString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(path))
                {
                    UserInterface.ShowMessage("Invalid Path", "Please enter a valid export path.");
                    return;
                }
                
                try
                {
                    // Export the data with the selected BOM option
                    csvManager.ExportToCsv(path, bomCheckbox.Checked);
                    Terminal.Gui.Application.RequestStop(); // Close the dialog
                    UserInterface.ShowMessage("Export Complete", $"Export successful to: {path}");
                    UserInterface.UpdateStatus($"Data exported to CSV: {Path.GetFileName(path)}");
                }
                catch (Exception ex)
                {
                    UserInterface.ShowMessage("Export Error", $"Failed to export: {ex.Message}");
                    UserInterface.UpdateStatus("Export to CSV failed.");
                }
            };
            
            var cancelButton = new Button("Cancel");
            cancelButton.Clicked += () => { Terminal.Gui.Application.RequestStop(); };
            
            dialog.AddButton(cancelButton);
            dialog.AddButton(exportButton);
            
            // Set focus to the path field
            dialog.FocusFirst();
            pathText.SetFocus();
            
            // Run the dialog
            Terminal.Gui.Application.Run(dialog);
        }
        
        public static void ImportFromCsv()
        {
            // Create a dialog for CSV import
            var dialog = new Dialog("Import from CSV", 60, 15);
            
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

            // Add radio button group for import type
            var importTypeFrame = new FrameView("Import Type") 
            { 
                X = 1, 
                Y = 5, 
                Width = Dim.Fill(2), 
                Height = 5 
            };
            
            // Use RadioGroup instead of individual RadioButton controls
            var importOptions = new ustring[] {
                "Standard Import",
                "Enhanced Import (for Cyrillic/UTF-8)"
            };
            // Correct constructor for RadioGroup
            var importTypeRadio = new RadioGroup(importOptions) { X = 1, Y = 0 };
            importTypeRadio.SelectedItem = 1; // Select the enhanced option by default
            importTypeFrame.Add(importTypeRadio);
            
            dialog.Add(pathLabel, pathText, helpLabel, importTypeFrame);
            
            // Add buttons using dialog's built-in functionality
            var importButton = new Button("Import");
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
                        // Check the selected item index from RadioGroup
                        if (importTypeRadio.SelectedItem == 0)
                        {
                            // Use standard import
                            csvManager.ImportFromCsv(filePath);
                        }
                        else
                        {
                            // Use enhanced import for Cyrillic/international characters
                            csvManager.ImportFromExternalCsv(filePath);
                        }

                        inventory = dataStorage.LoadItems() ?? new List<InventoryItem>();
                        locations = dataStorage.LoadLocations() ?? new List<string>();

                        UserInterface.SetHasUnsavedChanges(true); // FIX: Import SHOULD mark changes as unsaved
                        InventoryTable.Initialize(inventory);
                        InventoryTable.PopulateInventoryTable(InventoryOperations.ApplySortAndFilter());
                        
                        Terminal.Gui.Application.RequestStop(); // Close dialog after successful import attempt
                        UserInterface.ShowMessage("Import Complete", $"Import process finished from '{Path.GetFileName(filePath)}'. Check table for results.");
                        UserInterface.UpdateStatus("Import process complete.");
                        Terminal.Gui.Application.MainLoop.Invoke(() => Terminal.Gui.Application.Refresh()); // Force UI refresh
                    }
                    catch (Exception ex)
                    {
                        Terminal.Gui.Application.RequestStop(); // Close dialog even on error
                        UserInterface.ShowMessage("Import Error", $"Failed to import: {ex.Message}");
                        UserInterface.UpdateStatus("Import from CSV failed.");
                        Terminal.Gui.Application.MainLoop.Invoke(() => Terminal.Gui.Application.Refresh()); // Force UI refresh even on error
                    }
                }
            };
            
            var cancelButton = new Button("Cancel");
            cancelButton.Clicked += () => { Terminal.Gui.Application.RequestStop(); };
            
            dialog.AddButton(cancelButton);
            dialog.AddButton(importButton);
            
            // Set focus to the path field
            dialog.FocusFirst();
            pathText.SetFocus();
            
            // Run the dialog
            Terminal.Gui.Application.Run(dialog);
        }
    }
} 