using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Terminal.Gui;
using SimpleInventoryApp.UI;

namespace SimpleInventoryApp.Operations
{
    public static class ReportOperations
    {
        private static List<InventoryItem> inventory = new List<InventoryItem>();
        private static List<string> locations = new List<string>();
        private static PdfLabelGenerator pdfGenerator = new PdfLabelGenerator();
        
        public static void Initialize(
            List<InventoryItem> inventoryItems, 
            List<string> locationItems, 
            PdfLabelGenerator generator)
        {
            inventory = inventoryItems;
            locations = locationItems;
            pdfGenerator = generator;
        }
        
        public static void GenerateLabelsForAllItems()
        {
            try
            {
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string filename = $"InventoryLabels_All_{timestamp}.pdf";
                string title = "Inventory Labels - All Items";
                
                pdfGenerator.GenerateLabels(inventory, filename, title);
                UserInterface.ShowMessage("PDF Generated", $"Labels generated successfully as:\n{filename}");
                UserInterface.UpdateStatus($"Generated labels for {inventory.Count} items.");
            }
            catch (Exception ex)
            {
                UserInterface.ShowMessage("PDF Error", $"Failed to generate labels: {ex.Message}");
                UserInterface.UpdateStatus("PDF generation failed.");
            }
        }
        
        public static void GenerateLabelsByLocation()
        {
            // Sort locations
            var sortedLocations = new List<string>(locations);
            sortedLocations.Sort();
            
            if (sortedLocations.Count == 0)
            {
                UserInterface.ShowMessage("No Locations", "No locations defined. Please add locations first.");
                return;
            }
            
            // Create dialog for selecting a location
            var dialog = new Dialog("Generate Labels by Location", 60, 10);
            
            var lblLocation = new Label("Select Location:") { X = 1, Y = 1 };
            
            // Create a ComboBox to display locations
            var locCombo = new ComboBox() {
                X = 20,
                Y = 1,
                Width = 30,
                Height = 1,
                ReadOnly = true // Always read-only for this dialog
            };
            
            // Add locations to the combo
            locCombo.SetSource(sortedLocations);
            
            // Set initial value
            locCombo.SelectedItem = 0;
            
            dialog.Add(lblLocation, locCombo);
            
            // Add buttons using dialog's built-in functionality
            var generateButton = new Button("Generate");
            generateButton.Clicked += () => {
                if (locCombo.SelectedItem >= 0 && locCombo.SelectedItem < sortedLocations.Count)
                {
                    string selectedLocation = sortedLocations[locCombo.SelectedItem];
                    
                    // Close dialog
                    Application.RequestStop();
                    
                    try
                    {
                        // Filter inventory by selected location
                        var filteredItems = inventory.Where(i => i.Location == selectedLocation).ToList();
                        
                        if (filteredItems.Count == 0)
                        {
                            UserInterface.ShowMessage("No Items", $"No items found at location: {selectedLocation}");
                            UserInterface.UpdateStatus("No labels to generate.");
                            return;
                        }
                        
                        // Generate labels for filtered items
                        string safeLocation = string.Join("_", selectedLocation.Split(Path.GetInvalidFileNameChars())); // Sanitize filename
                        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                        string filename = $"InventoryLabels_Location_{safeLocation}_{timestamp}.pdf";
                        string title = $"Inventory Labels - Location: {selectedLocation}";
                        
                        pdfGenerator.GenerateLabels(filteredItems, filename, title);
                        UserInterface.ShowMessage("PDF Generated", $"Labels generated successfully as:\n{filename}");
                        UserInterface.UpdateStatus($"Generated labels for {filteredItems.Count} items at location: {selectedLocation}.");
                    }
                    catch (Exception ex)
                    {
                        UserInterface.ShowMessage("PDF Error", $"Failed to generate labels: {ex.Message}");
                        UserInterface.UpdateStatus("PDF generation failed.");
                    }
                }
                else
                {
                    UserInterface.ShowMessage("Invalid Selection", "Please select a valid location.");
                }
            };
            
            var cancelButton = new Button("Cancel");
            cancelButton.Clicked += () => { Application.RequestStop(); };
            
            dialog.AddButton(cancelButton);
            dialog.AddButton(generateButton);
            
            // Set initial focus (optional, ComboBox might get it by default)
            dialog.FocusFirst();
            locCombo.SetFocus();

            // Run dialog
            Application.Run(dialog);
        }
        
        public static void GenerateLabelsByInventoryNumber()
        {
            var dialog = new Dialog("Generate Labels by Inventory Number", 60, 10);
            
            var lblPattern = new Label("Inv. Number Pattern:") { X = 1, Y = 1 };
            var txtPattern = new TextField("") { X = 22, Y = 1, Width = 30 };
            
            var lblHelp = new Label("Use a pattern like 'A-123' or a partial match like 'A-'") { X = 1, Y = 3, Width = Dim.Fill(2) };
            var lblHelp2 = new Label("Leave empty to generate labels for all items") { X = 1, Y = 4, Width = Dim.Fill(2) };
            
            dialog.Add(lblPattern, txtPattern, lblHelp, lblHelp2);
            
            // Add buttons using dialog's built-in functionality
            var generateButton = new Button("Generate");
            generateButton.Clicked += () => {
                string pattern = txtPattern.Text.ToString()?.Trim() ?? string.Empty;
                
                // Close dialog
                Application.RequestStop();
                
                try
                {
                    List<InventoryItem> filteredItems;
                    
                    if (string.IsNullOrWhiteSpace(pattern))
                    {
                        // If no pattern, use all items
                        filteredItems = new List<InventoryItem>(inventory);
                    }
                    else
                    {
                        // Filter by pattern
                        var regex = new Regex(Regex.Escape(pattern), RegexOptions.IgnoreCase);
                        filteredItems = inventory.Where(i => regex.IsMatch(i.InventoryNumber)).ToList();
                    }
                    
                    if (filteredItems.Count == 0)
                    {
                        UserInterface.ShowMessage("No Items", $"No items found matching pattern: {pattern}");
                        UserInterface.UpdateStatus("No labels to generate.");
                        return;
                    }
                    
                    // Generate labels for filtered items
                    string safePattern = !string.IsNullOrWhiteSpace(pattern) 
                        ? string.Join("_", pattern.Split(Path.GetInvalidFileNameChars())) 
                        : "ALL"; // Sanitize filename
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    string filename = $"InventoryLabels_Pattern_{safePattern}_{timestamp}.pdf";
                    string title = !string.IsNullOrWhiteSpace(pattern) 
                        ? $"Inventory Labels - Pattern: {pattern}" 
                        : "Inventory Labels - All Items";
                    
                    pdfGenerator.GenerateLabels(filteredItems, filename, title);
                    UserInterface.ShowMessage("PDF Generated", $"Labels generated successfully as:\n{filename}");
                    UserInterface.UpdateStatus($"Generated labels for {filteredItems.Count} items matching pattern: {(string.IsNullOrWhiteSpace(pattern) ? "[ALL]" : pattern)}");
                }
                catch (Exception ex)
                {
                    UserInterface.ShowMessage("PDF Error", $"Failed to generate labels: {ex.Message}");
                    UserInterface.UpdateStatus("PDF generation failed.");
                }
            };
            
            var cancelButton = new Button("Cancel");
            cancelButton.Clicked += () => { Application.RequestStop(); };
            
            dialog.AddButton(cancelButton);
            dialog.AddButton(generateButton);
            
            // Set initial focus
            dialog.FocusFirst();
            txtPattern.SetFocus();

            // Run dialog
            Application.Run(dialog);
        }
    }
} 