using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;
using NStack;
using SimpleInventoryApp.UI;

namespace SimpleInventoryApp.Operations
{
    public static class LocationOperations
    {
        private static List<string> locations = new List<string>();
        private static DataStorage dataStorage = null!;
        
        public static void Initialize(List<string> locationItems, DataStorage storage)
        {
            locations = locationItems;
            dataStorage = storage;
        }
        
        public static void ListLocations()
        {
            // Create a dialog to display locations
            int width = 60;
            int height = Math.Min(25, locations.Count + 7); // Limit height but ensure enough space
            
            var dialog = new Dialog("Locations", width, height);
            
            // Create a ListView to display locations
            var listView = new ListView(new List<string>(locations).OrderBy(l => l).ToList())
            {
                X = 1,
                Y = 1,
                Width = Dim.Fill(2),
                Height = Dim.Fill(4), // Leave space for buttons at bottom
            };
            
            dialog.Add(listView);
            
            // Add a "Close" button at the bottom
            var btnContainer = new View() { X = 0, Y = Pos.Bottom(dialog) - 5, Width = Dim.Fill(), Height = 1 };
            
            var closeButton = new Button("Close") { X = Pos.Center() };
            closeButton.Clicked += () => { Application.RequestStop(); };
            btnContainer.Add(closeButton);
            
            dialog.Add(btnContainer);
            
            // Run the dialog
            Application.Run(dialog);
            
            UserInterface.UpdateStatus($"Displayed {locations.Count} location(s).");
        }
        
        public static void AddLocation()
        {
            bool locationAdded = false;
            
            // Create a dialog for adding a new location
            var dialog = new Dialog("Add New Location", 50, 8);
            
            var nameLabel = new Label("Location Name:") { X = 1, Y = 1 };
            var nameText = new TextField("") { X = 20, Y = 1, Width = 25 };
            
            dialog.Add(nameLabel, nameText);
            
            // Add buttons
            var btnContainer = new View() { X = 0, Y = Pos.Bottom(dialog) - 5, Width = Dim.Fill(), Height = 1 };
            
            var okButton = new Button("OK") { X = Pos.Center() - 10 };
            okButton.Clicked += () => {
                string locationName = nameText.Text.ToString()?.Trim() ?? string.Empty;
                
                // Validate input
                if (string.IsNullOrWhiteSpace(locationName))
                {
                    UserInterface.ShowMessage("Invalid Input", "Location name cannot be empty.");
                    return;
                }
                
                // Check for duplicates
                if (locations.Contains(locationName, StringComparer.OrdinalIgnoreCase))
                {
                    UserInterface.ShowMessage("Duplicate", $"Location '{locationName}' already exists.");
                    return;
                }
                
                try {
                    // Add new location
                    locations.Add(locationName);
                    locations.Sort(); // Keep the list sorted
                    
                    UserInterface.SetHasUnsavedChanges(true);
                    locationAdded = true;
                    Application.RequestStop();
                } catch (Exception ex) {
                    UserInterface.ShowMessage("Add Error", $"Failed to add location: {ex.Message}");
                }
            };
            
            var cancelButton = new Button("Cancel") { X = Pos.Center() + 2 };
            cancelButton.Clicked += () => { Application.RequestStop(); };
            
            btnContainer.Add(okButton, cancelButton);
            dialog.Add(btnContainer);
            
            // Set focus to the location name field
            nameText.SetFocus();
            
            // Run the dialog
            Application.Run(dialog);
            
            if(locationAdded) {
                UserInterface.UpdateStatus($"Location added successfully.");
            } else {
                UserInterface.UpdateStatus("Add location cancelled or failed.");
            }
        }
    }
} 