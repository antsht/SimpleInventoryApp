using System;
using System.Collections.Generic;
using System.Data; // Required for DataTable
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Terminal.Gui;
using NStack;
using SimpleInventoryApp.UI;

namespace SimpleInventoryApp.Operations
{
    public static class InventoryOperations
    {
        private static List<InventoryItem> inventory = new List<InventoryItem>();
        private static List<string> locations = new List<string>();
        private static DataStorage dataStorage = null!;
        
        // --- Sort/Filter State ---
        private static string currentSortColumn = "Location"; // Default sort changed to Location
        private static bool sortAscending = true; 
        private static string currentFilterText = string.Empty;
        private static string currentLocationFilter = "All Locations"; // New state variable for location filter
        // -------------------------
        
        public static void Initialize(List<InventoryItem> inventoryItems, List<string> locationItems, DataStorage storage)
        {
            inventory = inventoryItems;
            locations = locationItems;
            dataStorage = storage;
            
            // Initialize the InventoryTable with the inventory items
            InventoryTable.Initialize(inventory);
        }

        // --- New Method for Sort/Filter Dialog ---
        public static void ShowSortFilterDialog()
        {
            var dialog = new Dialog("Sort & Filter Inventory", 65, 18); // Increased height for new controls

            // Sort Column Selection
            var sortColLabel = new Label("Sort By:") { X = 1, Y = 1 };
            var sortCols = new List<string> { "ID", "Inv Num", "Name", "Qty", "Location", "Last Updated" };
            var sortColCombo = new ComboBox() { 
                X = 15, Y = 1, Width = 20,
                Height = 5, // Allow dropdown
                ReadOnly = true
            };
            sortColCombo.SetSource(sortCols);
            // Ensure default selection matches the updated static variable
            sortColCombo.SelectedItem = sortCols.IndexOf(currentSortColumn);

            // Handle ArrowDown to prevent focus change for Sort ComboBox
            sortColCombo.KeyPress += (e) => {
                if (e.KeyEvent.Key == Key.CursorDown && !sortColCombo.IsShow && sortColCombo.HasFocus) {
                    e.Handled = true; 
                }
            };

            // Sort Direction
            var sortDirLabel = new Label("Direction:") { X = 38, Y = 1 };
            var sortDirRadio = new RadioGroup() { 
                X = 38, Y = 2, 
                RadioLabels = new NStack.ustring[] { "Ascending", "Descending" } 
            };
            sortDirRadio.SelectedItem = sortAscending ? 0 : 1;
            
            // ---- New Location Filter ----
            var locFilterLabel = new Label("Filter Location:") { X = 1, Y = 4 }; // Adjusted Y
            var locFilterCombo = new ComboBox() {
                X = 15, Y = 4, Width = 40, // Adjusted Y
                Height = 5, // Allow dropdown
                ReadOnly = true
            };
            // Prepare location list with "All" option
            var filterLocations = new List<string> { "All Locations" };
            filterLocations.AddRange(locations.OrderBy(l => l)); // Add sorted existing locations
            locFilterCombo.SetSource(filterLocations);
            // Set current selection
            int currentLocIndex = filterLocations.IndexOf(currentLocationFilter);
            locFilterCombo.SelectedItem = currentLocIndex >= 0 ? currentLocIndex : 0; 

            // Handle ArrowDown to prevent focus change for Location ComboBox
            locFilterCombo.KeyPress += (e) => {
                if (e.KeyEvent.Key == Key.CursorDown && !locFilterCombo.IsShow && locFilterCombo.HasFocus) {
                    e.Handled = true; 
                }
            };
            // ---- End New Location Filter ----

            // Filter Text
            var filterLabel = new Label("Filter Text:") { X = 1, Y = 7 }; // Adjusted Y
            var filterText = new TextField(currentFilterText) { X = 15, Y = 7, Width = 40 }; // Adjusted Y
            var filterHelp = new Label("(Filters Name, Inv Num, Desc)") { // Updated help text
                 X = 15, Y = 8, // Adjusted Y
                 ColorScheme = Colors.Error 
            };

            // Apply Button
            var applyBtn = new Button("Apply");
            applyBtn.Clicked += () => {
                currentSortColumn = sortCols[sortColCombo.SelectedItem];
                sortAscending = sortDirRadio.SelectedItem == 0;
                currentFilterText = filterText.Text?.ToString() ?? string.Empty;
                // Get selected location filter
                currentLocationFilter = filterLocations[locFilterCombo.SelectedItem];
                
                InventoryTable.PopulateInventoryTable(ApplySortAndFilter()); // Apply and refresh
                UserInterface.UpdateStatus("Sort/Filter applied.");
                Terminal.Gui.Application.RequestStop();
            };

            // Clear Filter Button
            var clearBtn = new Button("Clear Filter");
            clearBtn.Clicked += () => {
                currentFilterText = string.Empty;
                filterText.Text = string.Empty;
                currentLocationFilter = "All Locations"; // Reset location filter
                locFilterCombo.SelectedItem = 0; // Reset location combo selection
                
                // Keep current sort order when clearing filter
                InventoryTable.PopulateInventoryTable(ApplySortAndFilter()); // Apply and refresh
                UserInterface.UpdateStatus("Filter cleared.");
                // Keep dialog open to adjust sort if needed
            };

            // Cancel Button
            var cancelBtn = new Button("Cancel");
            cancelBtn.Clicked += () => { Terminal.Gui.Application.RequestStop(); };

            dialog.Add(sortColLabel, sortColCombo, sortDirLabel, sortDirRadio, 
                       locFilterLabel, locFilterCombo, // Add new location controls
                       filterLabel, filterText, filterHelp);
            dialog.AddButton(cancelBtn);
            dialog.AddButton(clearBtn);
            dialog.AddButton(applyBtn);

            dialog.FocusFirst();
            sortColCombo.SetFocus(); // Keep focus on Sort By combo box initially
            Terminal.Gui.Application.Run(dialog);
        }

        // --- New Method to Apply Current Sort/Filter --- 
        public static List<InventoryItem> ApplySortAndFilter()
        {
            string filterTextLower = string.IsNullOrWhiteSpace(currentFilterText) ? "" : currentFilterText.ToLowerInvariant();
            string sortColumn = string.IsNullOrWhiteSpace(currentSortColumn) ? "Location" : currentSortColumn;
            IEnumerable<InventoryItem> query = inventory;

            // Apply Location Filter (if not "All Locations")
            if (currentLocationFilter != "All Locations")
            {
                query = query.Where(item => 
                    item.Location != null && 
                    item.Location.Equals(currentLocationFilter, StringComparison.OrdinalIgnoreCase)
                );
            }

            // Apply Text Filter
            if (!string.IsNullOrWhiteSpace(filterTextLower))
            {
                query = query.Where(item => 
                    (item.Name?.ToLowerInvariant().Contains(filterTextLower) ?? false) ||
                    (item.InventoryNumber?.ToLowerInvariant().Contains(filterTextLower) ?? false) ||
                    (item.Description?.ToLowerInvariant().Contains(filterTextLower) ?? false)
                    // Removed Location from text filter as it's handled separately now
                );
            }

            // Apply Sorting
            switch (sortColumn)
            {
                case "Inv Num":
                    query = sortAscending ? query.OrderBy(i => i.InventoryNumber) : query.OrderByDescending(i => i.InventoryNumber);
                    break;
                case "Name":
                    query = sortAscending ? query.OrderBy(i => i.Name) : query.OrderByDescending(i => i.Name);
                    break;
                case "Qty":
                    query = sortAscending ? query.OrderBy(i => i.Quantity) : query.OrderByDescending(i => i.Quantity);
                    break;
                case "Location":
                    query = sortAscending ? query.OrderBy(i => i.Location) : query.OrderByDescending(i => i.Location);
                    break;
                case "Last Updated":
                    query = sortAscending ? query.OrderBy(i => i.LastUpdated) : query.OrderByDescending(i => i.LastUpdated);
                    break;
                case "ID": // Fallback/default
                default:
                     query = sortAscending ? query.OrderBy(i => i.Id) : query.OrderByDescending(i => i.Id);
                    break;
            }

            return query.ToList(); // Return the filtered and sorted list
        }

        // --- Updated ListAllItems ---
        public static void ListAllItems()
        {
            // No longer clears filter/sort, just refreshes view with current settings
            InventoryTable.PopulateInventoryTable(ApplySortAndFilter());
            UserInterface.UpdateStatus($"Displaying {ApplySortAndFilter().Count} of {inventory.Count} items.");
        }

        public static void AddItem()
        {
            bool itemAdded = false;

            // Create dialog for adding a new item
            var dialog = new Dialog("Add New Item", 60, 15);

            // Controls for the dialog
            var invNumLabel = new Label("Inventory Number:") { X = 1, Y = 1 };
            var invNumText = new TextField("") { X = 20, Y = 1, Width = 30 };

            var nameLabel = new Label("Name:") { X = 1, Y = 3 };
            var nameText = new TextField("") { X = 20, Y = 3, Width = 30 };

            var descLabel = new Label("Description:") { X = 1, Y = 5 };
            var descText = new TextField("") { X = 20, Y = 5, Width = 30 };

            var qtyLabel = new Label("Quantity:") { X = 1, Y = 7 };
            var qtyText = new TextField("1") { X = 20, Y = 7, Width = 10 }; // Default to 1

            var locLabel = new Label("Location:") { X = 1, Y = 9 };

            // Sort locations for the combo box
            var sortedLocations = new List<string>(locations);
            sortedLocations.Sort();

            var locCombo = new ComboBox() { 
                X = 20, 
                Y = 9, 
                Width = 30,
                Height = 6, // Allow dropdown
                ReadOnly = sortedLocations.Count > 0
            };

            // Handle ArrowDown to prevent focus change
            locCombo.KeyPress += (e) => {
                if (e.KeyEvent.Key == Key.CursorDown && !locCombo.IsShow && locCombo.HasFocus) {
                    e.Handled = true; 
                }
            };

            // Set the source for the ComboBox
            if (sortedLocations.Count > 0)
            {
                locCombo.SetSource(sortedLocations);
            }
            else
            {
                // If no locations, add a placeholder and prevent adding new ones via text input for now
                locCombo.SetSource(new List<string> { "Default" });
                locCombo.ReadOnly = true; // Make read-only if list is empty
            }

            // Add controls to dialog
            dialog.Add(
                invNumLabel, invNumText,
                nameLabel, nameText,
                descLabel, descText,
                qtyLabel, qtyText,
                locLabel, locCombo
            );

            // Fix: Use dialog's built-in button functionality
            var okBtn = new Button("OK");
            okBtn.Clicked += () => {
                // Input validation (Quantity)
                int quantity = 1; // Default
                if (!int.TryParse(qtyText.Text.ToString(), out quantity) || quantity < 0)
                {
                    UserInterface.ShowMessage("Validation Error", "Quantity must be a valid number (0 or greater).");
                    return; // Don't close dialog
                }

                // Input validation (Required fields)
                if (string.IsNullOrWhiteSpace(invNumText.Text.ToString()) || 
                    string.IsNullOrWhiteSpace(nameText.Text.ToString()))
                {
                    UserInterface.ShowMessage("Validation Error", "Inventory Number and Name are required fields.");
                    return; // Don't close dialog
                }

                // --- Location Handling ---
                string finalLocation = "Default";
                string enteredLocation = locCombo.Text?.ToString()?.Trim() ?? string.Empty;

                // Check if text was entered and if it's different from the selected item (if any)
                if (!string.IsNullOrWhiteSpace(enteredLocation))
                {
                    // Check if the entered location exists (case-insensitive)
                    bool exists = locations.Any(loc => loc.Equals(enteredLocation, StringComparison.OrdinalIgnoreCase));

                    if (exists)
                    {
                        // Use the existing location (find the exact casing)
                        finalLocation = locations.First(loc => loc.Equals(enteredLocation, StringComparison.OrdinalIgnoreCase));
                    }
                    else
                    {
                        // Location doesn't exist, ask to add
                        bool addNew = UserInterface.ConfirmAction("Add New Location?", $"The location '{enteredLocation}' does not exist. Add it?");
                        if (addNew)
                        {
                            locations.Add(enteredLocation); // Add to the main list
                            UserInterface.SetHasUnsavedChanges(true); // Mark changes
                            finalLocation = enteredLocation;

                            // Refresh the combobox source in the *current* dialog
                            sortedLocations = new List<string>(locations);
                            sortedLocations.Sort();
                            locCombo.SetSource(sortedLocations);
                            locCombo.SelectedItem = sortedLocations.IndexOf(finalLocation);
                            locCombo.Text = finalLocation; // Ensure text reflects added item
                        }
                        else
                        {
                            // User declined, stop the OK action
                            UserInterface.ShowMessage("Location Not Added", "Please select an existing location or confirm adding the new one.");
                            locCombo.SetFocus(); // Put focus back on combo box
                            return; // Don't close dialog
                        }
                    }
                } else if (locCombo.SelectedItem >= 0 && locCombo.SelectedItem < sortedLocations.Count) {
                    // No text entered, use the selected item from the list if valid
                     finalLocation = sortedLocations[locCombo.SelectedItem];
                } else if (locations.Count == 0) {
                    // Handle case where locations list was initially empty
                    finalLocation = "Default"; 
                } else {
                     // Invalid state (no text, no valid selection) - Should ideally not happen with validation
                     UserInterface.ShowMessage("Validation Error", "Please select or enter a valid location.");
                     locCombo.SetFocus();
                     return; // Don't close dialog
                }


                // --- Add Item Logic ---
                try {
                    // *** Get the next ID BEFORE creating the item ***
                    int newId = dataStorage.GetNextId(inventory);

                    // Create a new item with the input values
                    var newItem = new InventoryItem
                    {
                        Id = newId, // *** Assign the new ID ***
                        InventoryNumber = invNumText.Text.ToString()!.Trim(),
                        Name = nameText.Text.ToString()!.Trim(),
                        Description = descText.Text?.ToString()?.Trim() ?? string.Empty,
                        Quantity = quantity,
                        Location = finalLocation, // Use the determined location
                        LastUpdated = DateTime.UtcNow
                    };
                    
                    // Add to inventory and set flag
                    inventory.Add(newItem);
                    UserInterface.SetHasUnsavedChanges(true);
                    itemAdded = true;
                    Terminal.Gui.Application.RequestStop(); // Close the dialog
                } catch (Exception ex) {
                    UserInterface.ShowMessage("Add Error", $"Failed to add item: {ex.Message}");
                }
            };
            
            var cancelBtn = new Button("Cancel");
            cancelBtn.Clicked += () => { Terminal.Gui.Application.RequestStop(); };
            
            // Add the buttons to dialog's default button container
            dialog.AddButton(cancelBtn);
            dialog.AddButton(okBtn);

            // Ensure the first field gets focus when the dialog opens
            dialog.FocusFirst();
            invNumText.SetFocus();

            // Run the dialog
            Terminal.Gui.Application.Run(dialog);

            if (itemAdded)
            {
                InventoryTable.PopulateInventoryTable(ApplySortAndFilter()); // Refresh with sort/filter
                UserInterface.UpdateStatus($"Item added successfully.");
            } else {
                UserInterface.UpdateStatus("Add item cancelled or failed.");
            }
        }

        public static void EditItem(InventoryItem itemToEdit)
        {
            bool itemEdited = false;

            // Create dialog for editing an item
            var dialog = new Dialog($"Edit Item: {itemToEdit.Name}", 60, 15);

            // Controls for the dialog - similar to AddItem but pre-populated
            var invNumLabel = new Label("Inventory Number:") { X = 1, Y = 1 };
            var invNumText = new TextField(itemToEdit.InventoryNumber) { X = 20, Y = 1, Width = 30 };

            var nameLabel = new Label("Name:") { X = 1, Y = 3 };
            var nameText = new TextField(itemToEdit.Name) { X = 20, Y = 3, Width = 30 };

            var descLabel = new Label("Description:") { X = 1, Y = 5 };
            var descText = new TextField(itemToEdit.Description ?? "") { X = 20, Y = 5, Width = 30 };

            var qtyLabel = new Label("Quantity:") { X = 1, Y = 7 };
            var qtyText = new TextField(itemToEdit.Quantity.ToString()) { X = 20, Y = 7, Width = 10 };

            var locLabel = new Label("Location:") { X = 1, Y = 9 };

            // Sort locations for the combo box
            var sortedLocations = new List<string>(locations);
            sortedLocations.Sort();

            var locCombo = new ComboBox() {
                X = 20,
                Y = 9,
                Width = 30,
                Height = 6, // Allow dropdown
                ReadOnly = true
            };

            // Auto-open dropdown on focus - REMOVED

            // Handle ArrowDown to prevent focus change
            locCombo.KeyPress += (e) => {
                if (e.KeyEvent.Key == Key.CursorDown && !locCombo.IsShow && locCombo.HasFocus) {
                    e.Handled = true; 
                }
            };

            // Add the sorted locations to the combo box and find the current location's index
            int currentLocationIndex = 0;
            
            // Set source for the combo box
            locCombo.SetSource(sortedLocations);
            
            // Find the index of the current location
            for (int i = 0; i < sortedLocations.Count; i++)
            {
                if (sortedLocations[i] == itemToEdit.Location)
                {
                    currentLocationIndex = i;
                    break;
                }
            }
            
            if (sortedLocations.Count > 0)
            {
                locCombo.SelectedItem = currentLocationIndex;
            }
            else
            {
                // If no locations, add a placeholder
                locCombo.SetSource(new List<string> { "Default" });
            }

            // Add controls to dialog
            dialog.Add(
                invNumLabel, invNumText,
                nameLabel, nameText,
                descLabel, descText,
                qtyLabel, qtyText,
                locLabel, locCombo
            );

            // Fix: Use dialog's built-in button functionality instead of custom buttons container
            var okBtn = new Button("OK");
            okBtn.Clicked += () => {
                // Input validation (Quantity)
                int quantity = 1; // Default
                if (!int.TryParse(qtyText.Text.ToString(), out quantity) || quantity < 0)
                {
                    UserInterface.ShowMessage("Validation Error", "Quantity must be a valid number (0 or greater).");
                    return; // Don't close dialog
                }

                // Input validation (Required fields)
                if (string.IsNullOrWhiteSpace(invNumText.Text.ToString()) || 
                    string.IsNullOrWhiteSpace(nameText.Text.ToString()))
                {
                    UserInterface.ShowMessage("Validation Error", "Inventory Number and Name are required fields.");
                    return; // Don't close dialog
                }

                // --- Location Handling ---
                string finalLocation = "Default";
                string enteredLocation = locCombo.Text?.ToString()?.Trim() ?? string.Empty;
                bool locationChanged = false; // Flag to check if location needs update

                // Check if text was entered and if it's different from the original
                if (!string.IsNullOrWhiteSpace(enteredLocation) && !enteredLocation.Equals(itemToEdit.Location, StringComparison.OrdinalIgnoreCase))
                {
                    locationChanged = true;
                    // Check if the entered location exists (case-insensitive)
                    bool exists = locations.Any(loc => loc.Equals(enteredLocation, StringComparison.OrdinalIgnoreCase));

                    if (exists)
                    {
                        // Use the existing location (find the exact casing)
                        finalLocation = locations.First(loc => loc.Equals(enteredLocation, StringComparison.OrdinalIgnoreCase));
                    }
                    else
                    {
                        // Location doesn't exist, ask to add
                        bool addNew = UserInterface.ConfirmAction("Add New Location?", $"The location '{enteredLocation}' does not exist. Add it?");
                        if (addNew)
                        {
                            locations.Add(enteredLocation); // Add to the main list
                            UserInterface.SetHasUnsavedChanges(true); // Mark changes
                            finalLocation = enteredLocation;

                            // Refresh the combobox source in the *current* dialog
                            sortedLocations = new List<string>(locations);
                            sortedLocations.Sort();
                            locCombo.SetSource(sortedLocations);
                            locCombo.SelectedItem = sortedLocations.IndexOf(finalLocation);
                            locCombo.Text = finalLocation; // Ensure text reflects added item
                        }
                        else
                        {
                            // User declined, stop the OK action
                            UserInterface.ShowMessage("Location Not Added", "Please select an existing location or confirm adding the new one.");
                            locCombo.Text = itemToEdit.Location ?? "Default"; // Null-coalescing
                            if (sortedLocations != null)
                            {
                                locCombo.SelectedItem = sortedLocations.IndexOf(itemToEdit.Location ?? "Default");
                            }
                            locCombo.SetFocus(); // Put focus back on combo box
                            return; // Don't close dialog
                        }
                    }
                } else if (locCombo.SelectedItem >= 0 && locCombo.SelectedItem < sortedLocations.Count) {
                    // No new text entered, use the selected item from the list if valid
                    finalLocation = sortedLocations[locCombo.SelectedItem];
                    if (!finalLocation.Equals(itemToEdit.Location, StringComparison.OrdinalIgnoreCase))
                    {
                        locationChanged = true;
                    }
                } else if (!string.IsNullOrWhiteSpace(enteredLocation) && enteredLocation.Equals(itemToEdit.Location, StringComparison.OrdinalIgnoreCase)) {
                     // Text entered matches original location
                     finalLocation = itemToEdit.Location;
                     locationChanged = false;
                } else if (locations.Count == 0) {
                     // Handle case where locations list was initially empty
                     finalLocation = "Default"; 
                     locationChanged = !finalLocation.Equals(itemToEdit.Location, StringComparison.OrdinalIgnoreCase);
                 } else {
                     // Invalid state (no text, no valid selection, not matching original)
                     UserInterface.ShowMessage("Validation Error", "Please select or enter a valid location.");
                     locCombo.SetFocus();
                     return; // Don't close dialog
                 }

                // --- Update Item Logic ---
                try {
                    bool requiresSave = false;
                    if (itemToEdit.InventoryNumber != (invNumText.Text?.ToString()?.Trim() ?? string.Empty)) { itemToEdit.InventoryNumber = invNumText.Text?.ToString()?.Trim() ?? string.Empty; requiresSave = true; }
                    if (itemToEdit.Name != (nameText.Text?.ToString()?.Trim() ?? string.Empty)) { itemToEdit.Name = nameText.Text?.ToString()?.Trim() ?? string.Empty; requiresSave = true; }
                    if (itemToEdit.Description != (descText.Text?.ToString()?.Trim() ?? string.Empty)) { itemToEdit.Description = descText.Text?.ToString()?.Trim() ?? string.Empty; requiresSave = true; }
                    if (itemToEdit.Quantity != quantity) { itemToEdit.Quantity = quantity; requiresSave = true; }
                    if (locationChanged) { itemToEdit.Location = finalLocation; requiresSave = true; }
                    
                    if (requiresSave)
                    {
                        itemToEdit.LastUpdated = DateTime.UtcNow; // Update timestamp
                        UserInterface.SetHasUnsavedChanges(true);
                        itemEdited = true;
                    } else {
                        // No actual changes made
                        itemEdited = false;
                    }

                    Terminal.Gui.Application.RequestStop(); // Close the dialog
                } catch (Exception ex) {
                    UserInterface.ShowMessage("Edit Error", $"Failed to update item: {ex.Message}");
                }
            };
            
            var cancelBtn = new Button("Cancel");
            cancelBtn.Clicked += () => { Terminal.Gui.Application.RequestStop(); };

            // Add the buttons to dialog's default button container
            dialog.AddButton(cancelBtn);
            dialog.AddButton(okBtn);

            // Ensure the first field gets focus when the dialog opens
            dialog.FocusFirst();
            invNumText.SetFocus();

            // Run the dialog
            Terminal.Gui.Application.Run(dialog);

            if (itemEdited)
            {
                InventoryTable.PopulateInventoryTable(ApplySortAndFilter()); // Refresh with sort/filter
                UserInterface.UpdateStatus($"Item ID {itemToEdit.Id} updated successfully.");
            } else {
                UserInterface.UpdateStatus("Edit item cancelled or failed.");
            }
        }

        public static void DeleteItem(InventoryItem itemToDelete)
        {
            // Confirm deletion
            bool confirmed = UserInterface.ConfirmAction("Confirm Delete", 
                $"Are you sure you want to delete:\n\nID: {itemToDelete.Id}\nName: {itemToDelete.Name}\nInv #: {itemToDelete.InventoryNumber}");

            if (confirmed)
            {
                try
                {
                    inventory.Remove(itemToDelete);
                    UserInterface.SetHasUnsavedChanges(true);
                    InventoryTable.PopulateInventoryTable(ApplySortAndFilter()); // Refresh with sort/filter
                    UserInterface.UpdateStatus($"Item ID {itemToDelete.Id} deleted successfully.");
                }
                catch (Exception ex)
                {
                    UserInterface.ShowMessage("Delete Error", $"Failed to delete item: {ex.Message}");
                    UserInterface.UpdateStatus("Item deletion failed.");
                }
            }
            else
            {
                UserInterface.UpdateStatus("Item deletion cancelled.");
            }
        }

        public static void DeleteItem()
        {
            // Get input from user about which item to delete
            var dialog = new Dialog("Delete Item By ID", 50, 7);
            
            var idLabel = new Label("Enter Item ID:") { X = 1, Y = 1 };
            var idText = new TextField("") { X = 20, Y = 1, Width = 20 };
            
            dialog.Add(idLabel, idText);

            // Add buttons
            var btnContainer = new View() { X = 0, Y = Pos.AnchorEnd(1), Width = Dim.Fill(), Height = 1 };
            
            var deleteButton = new Button("Delete") { X = Pos.Center() - 12 };
            deleteButton.Clicked += () => {
                if (int.TryParse(idText.Text?.ToString(), out int id))
                {
                    var itemToDelete = inventory.FirstOrDefault(i => i.Id == id);
                    if (itemToDelete != null)
                    {
                        Terminal.Gui.Application.RequestStop(); // Close this dialog
                        
                        // Call DeleteItem with the found item after a short delay
                        Terminal.Gui.Application.MainLoop.AddTimeout(TimeSpan.FromMilliseconds(10), (_) => {
                            DeleteItem(itemToDelete);
                            return false; // Run only once
                        });
                    }
                    else
                    {
                        UserInterface.ShowMessage("Not Found", $"Item with ID {id} not found.");
                    }
                }
                else
                {
                    UserInterface.ShowMessage("Invalid Input", "Please enter a valid Item ID (number).");
                }
            };
            
            var cancelButton = new Button("Cancel") { X = Pos.Center() + 2 };
            cancelButton.Clicked += () => { Terminal.Gui.Application.RequestStop(); };
            
            btnContainer.Add(deleteButton, cancelButton);
            dialog.Add(btnContainer);
            
            // Set focus to the ID field
            idText.SetFocus();
            
            // Run the dialog
            Terminal.Gui.Application.Run(dialog);
        }

        public static void UpdateItemQuantity()
        {
            // Implementation similar to DeleteItem but with quantity update logic
            UserInterface.ShowMessage("Feature", "This feature will be implemented soon.");
        }

        public static void UpdateItemLocation()
        {
            // Implementation similar to DeleteItem but with location update logic
            UserInterface.ShowMessage("Feature", "This feature will be implemented soon.");
        }

        public static void FindItemByName()
        {
            var dialog = new Dialog("Find Item by Name", 50, 7);
            
            var nameLabel = new Label("Enter Name:") { X = 1, Y = 1 };
            var nameText = new TextField("") { X = 20, Y = 1, Width = 25 };
            
            dialog.Add(nameLabel, nameText);

            // Add buttons
            var btnContainer = new View() { X = 0, Y = Pos.AnchorEnd(1), Width = Dim.Fill(), Height = 1 };
            
            var findButton = new Button("Find") { X = Pos.Center() - 10 };
            findButton.Clicked += () => {
                string searchText = nameText.Text?.ToString()?.Trim() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    // Close dialog before showing results
                    Terminal.Gui.Application.RequestStop();
                    
                    // Use case-insensitive regex for partial matching
                    var regex = new Regex(Regex.Escape(searchText), RegexOptions.IgnoreCase);
                    var results = inventory.Where(i => regex.IsMatch(i.Name)).ToList();
                    
                    if (results.Count > 0)
                    {
                        InventoryTable.PopulateInventoryTable(results); // Populate with ONLY found items
                        UserInterface.UpdateStatus($"Found {results.Count} item(s) matching '{searchText}'.");
                    }
                    else
                    {
                        UserInterface.ShowMessage("No Results", $"No items found with name containing '{searchText}'.");
                        UserInterface.UpdateStatus("No search results found.");
                    }
                }
                else
                {
                    UserInterface.ShowMessage("Invalid Input", "Please enter a search term.");
                }
            };
            
            var cancelButton = new Button("Cancel") { X = Pos.Center() + 2 };
            cancelButton.Clicked += () => { Terminal.Gui.Application.RequestStop(); };
            
            btnContainer.Add(findButton, cancelButton);
            dialog.Add(btnContainer);
            
            // Set focus to the name field
            nameText.SetFocus();
            
            // Run the dialog
            Terminal.Gui.Application.Run(dialog);
        }

        public static void FindItemByInvNum()
        {
            var dialog = new Dialog("Find Item by Inventory Number", 55, 7);
            
            var invNumLabel = new Label("Enter Inv. Number:") { X = 1, Y = 1 };
            var invNumText = new TextField("") { X = 20, Y = 1, Width = 30 };
            
            dialog.Add(invNumLabel, invNumText);

            // Add buttons
            var btnContainer = new View() { X = 0, Y = Pos.AnchorEnd(1), Width = Dim.Fill(), Height = 1 };
            
            var findButton = new Button("Find") { X = Pos.Center() - 10 };
            findButton.Clicked += () => {
                string searchText = invNumText.Text?.ToString()?.Trim() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    // Close dialog before showing results
                    Terminal.Gui.Application.RequestStop();
                    
                    // Use case-insensitive regex for partial matching
                    var regex = new Regex(Regex.Escape(searchText), RegexOptions.IgnoreCase);
                    var results = inventory.Where(i => regex.IsMatch(i.InventoryNumber)).ToList();
                    
                    if (results.Count > 0)
                    {
                        InventoryTable.PopulateInventoryTable(results); // Populate with ONLY found items
                        UserInterface.UpdateStatus($"Found {results.Count} item(s) matching '{searchText}'.");
                    }
                    else
                    {
                        UserInterface.ShowMessage("No Results", $"No items found with inventory number containing '{searchText}'.");
                        UserInterface.UpdateStatus("No search results found.");
                    }
                }
                else
                {
                    UserInterface.ShowMessage("Invalid Input", "Please enter a search term.");
                }
            };
            
            var cancelButton = new Button("Cancel") { X = Pos.Center() + 2 };
            cancelButton.Clicked += () => { Terminal.Gui.Application.RequestStop(); };
            
            btnContainer.Add(findButton, cancelButton);
            dialog.Add(btnContainer);
            
            // Set focus to the inventory number field
            invNumText.SetFocus();
            
            // Run the dialog
            Terminal.Gui.Application.Run(dialog);
        }
    }
} 