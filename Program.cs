using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Data; // For DataTable
// using Spectre.Console; // Comment out Spectre.Console
using QuestPDF.Infrastructure;
using Terminal.Gui; // Add Terminal.Gui
using NStack; // For ustring

namespace SimpleInventoryApp
{
    class Program
    {
        // Keep DataStorage instance, it now handles both files
        static readonly DataStorage dataStorage = new DataStorage(); // Uses default filenames
        static readonly CsvManager csvManager = new CsvManager(dataStorage);
        static List<InventoryItem> inventory = new List<InventoryItem>();
        static List<string> locations = new List<string>(); // New list for locations
        // Instantiate the PDF generator
        static readonly PdfLabelGenerator pdfGenerator = new PdfLabelGenerator();
        static bool hasUnsavedChanges = false; // Track unsaved changes

        // --- Terminal.Gui Views ---
        private static MenuBar? menu;
        private static StatusBar? statusBar;
        private static Window? mainWin; // Main content window
        private static Label? statusLabel; // Label inside the status bar
        private static TableView? inventoryTableView; // Table to display inventory


        static void Main(string[] args)
        {
            // --- QuestPDF Initialization ---
            QuestPDF.Settings.License = LicenseType.Community;
            // -------------------------------

            // Load data first
            inventory = dataStorage.LoadItems();
            locations = dataStorage.LoadLocations();


            Application.Init();

            var top = Application.Top;

            // --- Main Window ---
            // Occupies the space between MenuBar and StatusBar
            mainWin = new Window("Inventory")
            {
                X = 0,
                Y = 1, // Below MenuBar
                Width = Dim.Fill(),
                Height = Dim.Fill(1) // Fill remaining space minus 1 for StatusBar
            };

            // --- Menu Bar ---
            menu = new MenuBar(new MenuBarItem[] {
                new MenuBarItem ("_Items", new MenuItem [] {
                    new MenuItem ("_List All", "", () => ListAllItems(), null, null, (Key)0),
                    new MenuItem ("_Add New...", "", () => AddItem(), null, null, (Key)0),
                    new MenuItem ("_Update Quantity...", "", () => UpdateItemQuantity(), null, null, (Key)0),
                    new MenuItem ("_Update Location...", "", () => UpdateItemLocation(), null, null, (Key)0),
                    new MenuItem ("Delete Record...", "", () => DeleteItem(), null, null, (Key)0),
                    new MenuItem ("Find by _Name...", "", () => FindItemByName(), null, null, (Key)0),
                    new MenuItem ("Find by Inv _Num...", "", () => FindItemByInvNum(), null, null, (Key)0)
                }),
                new MenuBarItem ("_Dictionary", new MenuItem [] {
                     new MenuBarItem ("_Locations", new MenuItem [] {
                        new MenuItem ("_List", "", () => ListLocations(), null, null, (Key)0),
                        new MenuItem ("_Add New...", "", () => AddLocation(), null, null, (Key)0),
                        // Potential: Delete Location?
                    })
                }),
                 new MenuBarItem ("_Service", new MenuItem [] {
                     new MenuBarItem ("_CSV", new MenuItem [] {
                        new MenuItem ("_Export...", "", () => ExportToCsv(), null, null, (Key)0),
                        new MenuItem ("_Import...", "", () => ImportFromCsv(), null, null, (Key)0),
                    })
                }),
                new MenuBarItem ("_Reports", new MenuItem [] {
                     new MenuBarItem ("Generate _Labels PDF", new MenuItem [] {
                        new MenuItem ("_All Items", "", () => GenerateLabelsForAllItems(), null, null, (Key)0),
                        new MenuItem ("By _Location...", "", () => GenerateLabelsByLocation(), null, null, (Key)0),
                        new MenuItem ("By Inv _Number...", "", () => GenerateLabelsByInventoryNumber(), null, null, (Key)0),
                    })
                }),
                new MenuBarItem ("_File", new MenuItem [] {
                    new MenuItem ("_Quit", "", () => RequestQuit(), null, null, Key.Q | Key.CtrlMask)
                })
            });

             // --- Status Bar ---
            statusLabel = new Label("Ready") { AutoSize = false, Width = Dim.Fill(), TextAlignment = TextAlignment.Left };
            statusBar = new StatusBar(new StatusItem[] {
                // Shortcut to quit
                new StatusItem(Key.Q | Key.CtrlMask, "~^Q~ Quit", () => RequestQuit()),
                // Shortcut for Save
                new StatusItem(Key.S | Key.CtrlMask, "~^S~ Save", () => SaveData()),
                // Shortcut for Restore
                new StatusItem(Key.R | Key.CtrlMask, "~^R~ Restore", () => {
                    // Use timeout wrapper as Restore shows confirmation dialog
                    Application.MainLoop.AddTimeout(TimeSpan.FromMilliseconds(10), (_) => {
                          RestoreData();
                          return false; // Run only once
                     });
                }),
                // Shortcut for deleting selected item
                new StatusItem(Key.D | Key.CtrlMask, "~^D~ Delete", () => {
                    // Use timeout wrapper like in KeyPress handler to safely call DeleteItem
                    Application.MainLoop.AddTimeout(TimeSpan.FromMilliseconds(10), (_) => {
                          DeleteItem(); // Call the overload that deletes the selected item
                          return false; // Run only once
                     });
                })
                // Status Label will be added separately below
            });
            statusBar.Add(statusLabel); // Add the label view directly to the status bar

            // --- Add Views to Toplevel ---
            top.Add(menu, mainWin, statusBar);

            // --- Initial Setup ---
            // Let's display the inventory list initially
            SetupInventoryTableView();
            if (inventoryTableView != null)
            {
                 mainWin.Add(inventoryTableView); // Add table view to the main window
            }
            ListAllItems(); // Load data into the table

            // --- Run Application ---
            Application.Run();
            Application.Shutdown();

            // Final save on exit (optional, could rely on saves after each action)
            // dataStorage.SaveItems(inventory);
            // dataStorage.SaveLocations(locations);
             Console.WriteLine("Inventory saved. Goodbye!"); // Simple console message on exit
        }

        static void UpdateStatus(string message)
        {
            if (statusLabel != null)
            {
                string fullMessage = message;
                if (hasUnsavedChanges)
                {
                    fullMessage += " [* unsaved changes]";
                }
                statusLabel.Text = fullMessage;
                Application.DoEvents(); // Ensure status update is processed
            }
        }

        static void ShowMessage(string title, string message)
        {
             MessageBox.Query(title, message, "Ok");
        }

        static bool ConfirmAction(string title, string message)
        {
             // Ensure width fits message roughly
             int width = Math.Max(title.Length, message.Split('\n').Max(s => s.Length)) + 10;
             int height = message.Split('\n').Length + 5;
             return MessageBox.Query(width, height, title, message, "Yes", "No") == 0; // 0 is the index of "Yes"
        }

        // --- Table View Setup and Population ---
        static void SetupInventoryTableView()
        {
            if (inventoryTableView != null)
            {
                 inventoryTableView.Dispose(); // Dispose if exists
            }

            inventoryTableView = new TableView()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(1), // Leave space for scrollbar
                Height = Dim.Fill(), // Changed from Dim.Fill(1) to respect parent border
                FullRowSelect = true, // Select the whole row
            };

            // Define columns matching InventoryItem properties
            var dataTable = new DataTable();
            dataTable.Columns.Add("Inv Num", typeof(string));
            dataTable.Columns.Add("ID", typeof(int));
            dataTable.Columns.Add("Name", typeof(string));
            dataTable.Columns.Add("Description", typeof(string));
            dataTable.Columns.Add("Qty", typeof(int));
            dataTable.Columns.Add("Location", typeof(string));
            dataTable.Columns.Add("Last Updated", typeof(string)); // Keep as string for display
            inventoryTableView.Table = dataTable; // Assign the empty table structure initially

            // Event handling (optional for now)
            inventoryTableView.CellActivated += (args) => {
                 // Example: Show details of the selected item
                 var table = args.Table;
                 if(table != null && args.Row >= 0 && args.Row < table.Rows.Count) {
                    try {
                         // Get data directly from the DataTable to avoid issues with list ordering mismatches
                         var row = table.Rows[args.Row];
                         // ShowMessage("Item Details", ... ) // Removed old ShowMessage call

                         // Find the actual InventoryItem object based on ID
                         if (int.TryParse(row["ID"]?.ToString(), out int itemId)) {
                            var itemToEdit = inventory.FirstOrDefault(item => item.Id == itemId);
                            if (itemToEdit != null) {
                                EditItem(itemToEdit); // Call the new EditItem method
                            } else {
                                 ShowMessage("Error", $"Could not find item with ID {itemId} in the main list.");
                            }
                         } else {
                             ShowMessage("Error", "Could not determine Item ID from the selected row.");
                         }

                     } catch (Exception ex) {
                         ShowMessage("Error", $"Failed to get item details or initiate edit: {ex.Message}");
                     }
                 }
             };

            // --- Delete Key Handling ---
            inventoryTableView.KeyPress += (args) => {
                if (args.KeyEvent.Key == Key.Delete || args.KeyEvent.Key == (Key.D | Key.CtrlMask))
                {
                    args.Handled = true; // Mark event as handled

                    var table = inventoryTableView.Table;
                    int selectedRowIndex = inventoryTableView.SelectedRow;

                    if (table != null && selectedRowIndex >= 0 && selectedRowIndex < table.Rows.Count)
                    {
                        try
                        {
                             var row = table.Rows[selectedRowIndex];
                             if (int.TryParse(row["ID"]?.ToString(), out int itemId))
                             {
                                 var itemToDelete = inventory.FirstOrDefault(item => item.Id == itemId);
                                 if (itemToDelete != null) // Check if item was found
                                 {
                                     // Need to ensure UI updates properly after modal dialog (DeleteItem shows confirmation)
                                     Application.MainLoop.AddTimeout(TimeSpan.FromMilliseconds(10), (_) => {
                                          DeleteItem(itemToDelete); // Call delete logic after a short delay
                                          return false; // Run only once
                                     });
                                 } else {
                                     ShowMessage("Error", $"Could not find item with ID {itemId} in the main list for deletion.");
                                 }
                             } else {
                                 ShowMessage("Error", "Could not determine Item ID from the selected row for deletion.");
                             }
                        } catch (Exception ex) {
                             ShowMessage("Error", $"Error processing delete key press: {ex.Message}");
                        }
                    } else {
                        UpdateStatus("No item selected to delete."); // Or ShowMessage
                    }
                }
            };
        }

        static void PopulateInventoryTable(List<InventoryItem>? items = null)
        {
             if (inventoryTableView == null || inventoryTableView.Table == null) return;

             var currentTable = inventoryTableView.Table;
             currentTable.Rows.Clear(); // Clear existing rows

             var itemsToList = items ?? inventory;
             // Order items for display
             itemsToList = itemsToList.OrderBy(i => i.InventoryNumber).ThenBy(i => i.Name).ToList();

             foreach (var item in itemsToList)
             {
                 currentTable.Rows.Add(
                     item.InventoryNumber,
                     item.Id,
                     item.Name,
                     item.Description,
                     item.Quantity,
                     item.Location,
                     item.LastUpdated.ToString("yyyy-MM-dd HH:mm")
                 );
             }

             inventoryTableView.Update(); // Update the view to reflect data changes
             UpdateStatus($"Listed {currentTable.Rows.Count} items.");
        }


        // --- Menu Action Implementations ---

        static void ListAllItems()
        {
            if (inventoryTableView == null) return;
            PopulateInventoryTable(inventory);
            UpdateStatus("Showing all items.");
        }

        static void AddItem()
        {
             // Check if locations exist
            if (!locations.Any())
            {
                ShowMessage("Error", "No locations available. Please add locations first via Dictionary -> Locations -> Add New.");
                return;
            }

             // --- Create Add Item Dialog ---
             var dialog = new Dialog("Add New Item/Component", 60, 18); // Title, Width, Height

             int labelWidth = 15; // Fixed width for label column
             int inputPadding = 1;
             int currentY = 1;

             // --- Columns ---
             var labelColumn = new View() {
                 X = 1,
                 Y = 1,
                 Width = labelWidth,
                 Height = Dim.Fill(2) // Leave space for buttons
             };
             var inputColumn = new View() {
                 X = Pos.Right(labelColumn) + inputPadding,
                 Y = 1,
                 Width = Dim.Fill(2),
                 Height = Dim.Fill(2) // Leave space for buttons
             };

             // --- Add elements to columns ---
             // Labels (added to labelColumn)
             var invNumLabel = new Label(0, currentY, "Inv Num (*):");
             var nameLabel = new Label(0, currentY + 2, "Name (*):");
             var descLabel = new Label(0, currentY + 4, "Desc:");
             var qtyLabel = new Label(0, currentY + 6, "Quantity (*):");
             var locLabel = new Label(0, currentY + 8, "Location (*):");
             labelColumn.Add(invNumLabel, nameLabel, descLabel, qtyLabel, locLabel);

             // Input Fields (added to inputColumn, X=0 relative to inputColumn)
             var invNumText = new TextField("") { X = 0, Y = invNumLabel.Y, Width = Dim.Fill() };
             var nameText = new TextField("")   { X = 0, Y = nameLabel.Y, Width = Dim.Fill() };
             var descText = new TextField("")   { X = 0, Y = descLabel.Y, Width = Dim.Fill() };
             var qtyText = new TextField("1")    { X = 0, Y = qtyLabel.Y, Width = 10 }; // Smaller width for quantity
             var locCombo = new ComboBox() {
                 X = 0,
                 Y = locLabel.Y,
                 Width = Dim.Fill(),
                 Height = 4,
             };
             var sortedLocations = locations.OrderBy(l => l).ToList();
             locCombo.SetSource(sortedLocations);
             // if (sortedLocations.Any()) locCombo.SelectedItem = 0; // Don't select first item by default
             locCombo.SelectedItem = -1; // Set to -1 for no initial selection
             inputColumn.Add(invNumText, nameText, descText, qtyText, locCombo);

            // Add columns to the dialog
            dialog.Add(labelColumn, inputColumn);

             bool itemAdded = false;
             var okButton = new Button("OK", is_default: true);

            // --- Enter Key Navigation --- 
            // void MoveFocus(View current, View next) {
            //     if (current.HasFocus) {
            //         next.SetFocus();
            //     }
            // }

            invNumText.KeyPress += (args) => {
                 if (args.KeyEvent.Key == Key.Enter) {
                     args.Handled = true;
                     nameText.SetFocus();
                 }
             };
             nameText.KeyPress += (args) => {
                 if (args.KeyEvent.Key == Key.Enter) {
                     args.Handled = true;
                     descText.SetFocus();
                 }
             };
            descText.KeyPress += (args) => {
                 if (args.KeyEvent.Key == Key.Enter) {
                     args.Handled = true;
                     qtyText.SetFocus();
                 }
             };
             qtyText.KeyPress += (args) => {
                 if (args.KeyEvent.Key == Key.Enter) {
                      // Validate quantity before moving focus (optional but good UX)
                      if (!int.TryParse(qtyText.Text?.ToString(), out int quantity) || quantity < 0) {
                           MessageBox.ErrorQuery("Validation Error", "Quantity must be a non-negative number.", "Ok");
                           qtyText.SetFocus();
                           args.Handled = true; // Prevent moving focus if invalid
                      } else {
                           args.Handled = true;
                           locCombo.SetFocus();
                      }
                 }
             };
             // Optional: Handle Enter on ComboBox to move to OK button?
             locCombo.Enter += (args) => {
                if (!locCombo.IsShow) {
                    locCombo.Expand();
                }
             };

             okButton.Clicked += () => {
                 // --- Validation ---
                 if (string.IsNullOrWhiteSpace(invNumText.Text?.ToString())) {
                     MessageBox.ErrorQuery("Validation Error", "Inventory Number cannot be empty.", "Ok");
                     invNumText.SetFocus(); return;
                 }
                 if (string.IsNullOrWhiteSpace(nameText.Text?.ToString())) {
                     MessageBox.ErrorQuery("Validation Error", "Name cannot be empty.", "Ok");
                     nameText.SetFocus(); return;
                 }
                 if (!int.TryParse(qtyText.Text?.ToString(), out int quantity) || quantity < 0) {
                     MessageBox.ErrorQuery("Validation Error", "Quantity must be a non-negative number.", "Ok");
                     qtyText.SetFocus(); return;
                 }
                 if (locCombo.SelectedItem < 0 || locCombo.SelectedItem >= sortedLocations.Count) { // Check if a location is selected
                      MessageBox.ErrorQuery("Validation Error", "Please select a valid location.", "Ok");
                     locCombo.SetFocus(); return;
                 }

                 // --- Add Item Logic ---
            var newItem = new InventoryItem
            {
                Id = dataStorage.GetNextId(inventory),
                    InventoryNumber = invNumText.Text.ToString()!.Trim(),
                    Name = nameText.Text.ToString()!.Trim(),
                    Description = descText.Text?.ToString()?.Trim() ?? string.Empty,
                Quantity = quantity,
                    Location = sortedLocations[locCombo.SelectedItem], // Get selected location text
                LastUpdated = DateTime.UtcNow
            };

            inventory.Add(newItem);
                try {
                     // dataStorage.SaveItems(inventory); // REMOVE Save immediately
                     hasUnsavedChanges = true; // Set flag
                     itemAdded = true;
                     Application.RequestStop(); // Close the dialog
                } catch (Exception ex) {
                     MessageBox.ErrorQuery("Save Error", $"Failed to save item: {ex.Message}", "Ok");
                }
             };

             var cancelButton = new Button("Cancel");
             cancelButton.Clicked += () => {
                 Application.RequestStop(); // Close the dialog
             };

             dialog.AddButton(okButton);
             dialog.AddButton(cancelButton);

             // Set focus using the Loaded event handler
             dialog.Loaded += () => {
                 invNumText.SetFocus();
             };

             Application.Run(dialog); // Run the dialog modally

            if (itemAdded)
            {
                 PopulateInventoryTable(); // Refresh the main table
                 UpdateStatus($"Item added successfully."); // UpdateStatus will add indicator if needed
            } else {
                 UpdateStatus("Add item cancelled or failed."); // UpdateStatus will add indicator if needed
            }
        }

        static void EditItem(InventoryItem itemToEdit)
        {
            if (itemToEdit == null)
            {
                ShowMessage("Error", "Cannot edit. Invalid item provided.");
                return;
            }
            // Check if locations exist for the dropdown
            if (!locations.Any())
            {
                ShowMessage("Error", "No locations defined. Cannot edit item location properly. Please add locations first.");
                // Consider allowing editing other fields even if locations are missing?
                // For now, prevent editing if locations are missing, as location is required.
                return;
            }

            var dialog = new Dialog($"Edit Item (ID: {itemToEdit.Id})", 60, 18);

            int labelWidth = 15;
            int inputPadding = 1;
            int currentY = 1;

            // --- Columns ---
            var labelColumn = new View() { X = 1, Y = 1, Width = labelWidth, Height = Dim.Fill(2) };
            var inputColumn = new View() { X = Pos.Right(labelColumn) + inputPadding, Y = 1, Width = Dim.Fill(2), Height = Dim.Fill(2) };

            // --- Labels ---
            var invNumLabel = new Label(0, currentY, "Inv Num (*):");
            var nameLabel = new Label(0, currentY + 2, "Name (*):");
            var descLabel = new Label(0, currentY + 4, "Desc:");
            var qtyLabel = new Label(0, currentY + 6, "Quantity (*):");
            var locLabel = new Label(0, currentY + 8, "Location (*):");
            labelColumn.Add(invNumLabel, nameLabel, descLabel, qtyLabel, locLabel);

            // --- Input Fields (Populated with existing data) ---
            var invNumText = new TextField(itemToEdit.InventoryNumber) { X = 0, Y = invNumLabel.Y, Width = Dim.Fill() };
            var nameText = new TextField(itemToEdit.Name) { X = 0, Y = nameLabel.Y, Width = Dim.Fill() };
            var descText = new TextField(itemToEdit.Description) { X = 0, Y = descLabel.Y, Width = Dim.Fill() };
            var qtyText = new TextField(itemToEdit.Quantity.ToString()) { X = 0, Y = qtyLabel.Y, Width = 10 };
            var locCombo = new ComboBox() {
                X = 0,
                Y = locLabel.Y,
                Width = Dim.Fill(),
                Height = 4,
            };
            var sortedLocations = locations.OrderBy(l => l).ToList();
            locCombo.SetSource(sortedLocations);
            // Set the initial selection in the ComboBox
            int initialLocationIndex = sortedLocations.IndexOf(itemToEdit.Location);
            if (initialLocationIndex >= 0) {
                locCombo.SelectedItem = initialLocationIndex;
            }
            else if (sortedLocations.Any()) {
                // If the item's location doesn't exist anymore (shouldn't happen ideally),
                // select the first available location.
                locCombo.SelectedItem = 0;
                ShowMessage("Warning", "Item's original location not found. Please re-select a location.");
            }
            inputColumn.Add(invNumText, nameText, descText, qtyText, locCombo);

            dialog.Add(labelColumn, inputColumn);

            bool itemEdited = false;
            var okButton = new Button("OK", is_default: true);

            // --- Enter Key Navigation (Same as AddItem) ---
            invNumText.KeyPress += (args) => { if (args.KeyEvent.Key == Key.Enter) { args.Handled = true; nameText.SetFocus(); } };
            nameText.KeyPress += (args) => { if (args.KeyEvent.Key == Key.Enter) { args.Handled = true; descText.SetFocus(); } };
            descText.KeyPress += (args) => { if (args.KeyEvent.Key == Key.Enter) { args.Handled = true; qtyText.SetFocus(); } };
            qtyText.KeyPress += (args) => {
                if (args.KeyEvent.Key == Key.Enter) {
                    if (!int.TryParse(qtyText.Text?.ToString(), out int q) || q < 0) {
                        MessageBox.ErrorQuery("Validation Error", "Quantity must be a non-negative number.", "Ok");
                        qtyText.SetFocus(); args.Handled = true;
                    } else {
                        args.Handled = true; locCombo.SetFocus();
                    }
                }
            };

            // --- Auto expand dropdown on focus ---
            locCombo.Enter += (args) => {
               if (!locCombo.IsShow) {
                   locCombo.Expand();
               }
            };

            okButton.Clicked += () => {
                // --- Validation ---
                 if (string.IsNullOrWhiteSpace(invNumText.Text?.ToString())) {
                     MessageBox.ErrorQuery("Validation Error", "Inventory Number cannot be empty.", "Ok");
                     invNumText.SetFocus(); return;
                 }
                 if (string.IsNullOrWhiteSpace(nameText.Text?.ToString())) {
                     MessageBox.ErrorQuery("Validation Error", "Name cannot be empty.", "Ok");
                     nameText.SetFocus(); return;
                 }
                 if (!int.TryParse(qtyText.Text?.ToString(), out int quantity) || quantity < 0) {
                     MessageBox.ErrorQuery("Validation Error", "Quantity must be a non-negative number.", "Ok");
                     qtyText.SetFocus(); return;
                 }
                 if (locCombo.SelectedItem < 0 || locCombo.SelectedItem >= sortedLocations.Count) {
                      MessageBox.ErrorQuery("Validation Error", "Please select a valid location.", "Ok");
                     locCombo.SetFocus(); return;
                 }

                 // --- Update Item Logic ---
                 try {
                    itemToEdit.InventoryNumber = invNumText.Text.ToString()!.Trim();
                    itemToEdit.Name = nameText.Text.ToString()!.Trim();
                    itemToEdit.Description = descText.Text?.ToString()?.Trim() ?? string.Empty;
                    itemToEdit.Quantity = quantity;
                    itemToEdit.Location = sortedLocations[locCombo.SelectedItem];
                    itemToEdit.LastUpdated = DateTime.UtcNow; // Update timestamp

                     // dataStorage.SaveItems(inventory); // REMOVE Save the updated list
                     hasUnsavedChanges = true; // Set flag
                     itemEdited = true;
                     Application.RequestStop(); // Close the dialog
                } catch (Exception ex) {
                     MessageBox.ErrorQuery("Save Error", $"Failed to save changes: {ex.Message}", "Ok");
                }
            };

            var cancelButton = new Button("Cancel");
            cancelButton.Clicked += () => { Application.RequestStop(); };

            dialog.AddButton(okButton);
            dialog.AddButton(cancelButton);

            // Set initial focus
            dialog.Loaded += () => { invNumText.SetFocus(); };

            Application.Run(dialog);

            if (itemEdited)
            {
                 PopulateInventoryTable(); // Refresh the main table
                 UpdateStatus($"Item ID {itemToEdit.Id} updated successfully."); // UpdateStatus will add indicator
            } else {
                 UpdateStatus("Edit item cancelled or failed."); // UpdateStatus will add indicator
            }
        }

        static void UpdateItemQuantity()
        {
            UpdateStatus("Update Quantity: Not Implemented Yet");
            ShowMessage("Info", "Functionality to update item quantity is not yet implemented.");
            // TODO: Implement using a dialog similar to AddItem or by selecting from the table
            // 1. Get Item ID (prompt or selection from TableView)
            // 2. Show current quantity in a dialog
            // 3. Prompt for new quantity (TextField)
            // 4. Validate
            // 5. Update inventory list
            // 6. Save
            // 7. Refresh table
        }

        static void UpdateItemLocation()
        {
            UpdateStatus("Update Location: Not Implemented Yet");
             ShowMessage("Info", "Functionality to update item location is not yet implemented.");
            // TODO: Implement similar to UpdateItemQuantity
             // 1. Get Item ID (prompt or selection from TableView)
             // 2. Show current location
             // 3. Show ComboBox with available locations in a dialog
             // 4. Update inventory list
             // 5. Save
             // 6. Refresh table
        }

        // New DeleteItem methods
        static void DeleteItem(InventoryItem itemToDelete)
        {
            if (itemToDelete == null)
            {
                ShowMessage("Error", "No item selected or provided to delete.");
                return;
            }

            bool confirmed = ConfirmAction("Confirm Deletion", $"Are you sure you want to delete item:\nInv#: {itemToDelete.InventoryNumber}\nName: {itemToDelete.Name}\nID: {itemToDelete.Id}");

            if (confirmed)
            {
                try
                {
                    inventory.Remove(itemToDelete);
                    // dataStorage.SaveItems(inventory); // REMOVE Save the changes
                    hasUnsavedChanges = true; // Set flag
                    PopulateInventoryTable(); // Refresh the table
                    UpdateStatus($"Item ID {itemToDelete.Id} deleted successfully."); // UpdateStatus will add indicator
                }
                catch (Exception ex)
                {
                     MessageBox.ErrorQuery("Delete Error", $"Failed to delete item: {ex.Message}", "Ok");
                     UpdateStatus("Item deletion failed."); // UpdateStatus will add indicator
                }
            }
            else
            {
                UpdateStatus("Item deletion cancelled."); // UpdateStatus will add indicator
            }
        }

        // Overload for menu item click
        static void DeleteItem()
        {
            if (inventoryTableView == null || inventoryTableView.SelectedRow < 0)
            {
                 ShowMessage("Delete Item", "Please select an item in the table first to delete.");
                 return;
            }

            try
            {
                var table = inventoryTableView.Table;
                int selectedRowIndex = inventoryTableView.SelectedRow;

                if (table != null && selectedRowIndex >= 0 && selectedRowIndex < table.Rows.Count)
                {
                    var row = table.Rows[selectedRowIndex];
                    if (int.TryParse(row["ID"]?.ToString(), out int itemId))
                    {
                        var itemToDelete = inventory.FirstOrDefault(item => item.Id == itemId);
                        if (itemToDelete != null)
                        {
                            DeleteItem(itemToDelete); // Call the main delete logic
                        } else {
                             ShowMessage("Error", $"Could not find item with ID {itemId} in the main list.");
                        }
                    }
                    else
                    {
                        ShowMessage("Error", "Could not determine Item ID from the selected row.");
                    }
                }
                 else {
                      ShowMessage("Delete Item", "Invalid selection in the table.");
                 }
            }
            catch (Exception ex)
            {
                ShowMessage("Error", $"Error initiating delete: {ex.Message}");
            }
        }

         static void FindItemByName()
         {
             var dialog = new Dialog("Find by Name", 40, 7);
             int labelWidth = 15;
             int inputPadding = 1;

             var labelColumn = new View() { X = 1, Y = 1, Width = labelWidth, Height = 1 };
             var inputColumn = new View() { X = Pos.Right(labelColumn) + inputPadding, Y = 1, Width = Dim.Fill(2), Height = 1 };

             var nameLabel = new Label(0, 0, "Name contains:");
             var nameText = new TextField("") { X = 0, Y = 0, Width = Dim.Fill() };

             labelColumn.Add(nameLabel);
             inputColumn.Add(nameText);
             dialog.Add(labelColumn, inputColumn);

             string? searchTerm = null;
             var findButton = new Button("Find", is_default: true);
             findButton.Clicked += () => {
                 var input = nameText.Text?.ToString()?.Trim();
                 if (!string.IsNullOrWhiteSpace(input))
                 {
                     searchTerm = input;
                     Application.RequestStop();
                 } else {
                     MessageBox.ErrorQuery("Input Required", "Please enter text to search for.", "Ok");
                     nameText.SetFocus();
                 }
             };
             var cancelButton = new Button("Cancel");
             cancelButton.Clicked += () => { Application.RequestStop(); };

             dialog.AddButton(findButton);
             dialog.AddButton(cancelButton);
             dialog.Loaded += () => nameText.SetFocus();

             Application.Run(dialog);

             if (searchTerm != null)
             {
                 var results = inventory.Where(i => i.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)).ToList();
                 PopulateInventoryTable(results);
                 UpdateStatus($"Found {results.Count} items matching '{searchTerm}'.");
             } else {
                  UpdateStatus("Find by name cancelled.");
             }
         }

         static void FindItemByInvNum()
         {
             var dialog = new Dialog("Find by Inv Num", 40, 7);
             int labelWidth = 10; // Adjusted width
             int inputPadding = 1;

             var labelColumn = new View() { X = 1, Y = 1, Width = labelWidth, Height = 1 };
             var inputColumn = new View() { X = Pos.Right(labelColumn) + inputPadding, Y = 1, Width = Dim.Fill(2), Height = 1 };

             var invNumLabel = new Label(0, 0, "Inv Num:");
             var invNumText = new TextField("") { X = 0, Y = 0, Width = Dim.Fill() };

             labelColumn.Add(invNumLabel);
             inputColumn.Add(invNumText);
             dialog.Add(labelColumn, inputColumn);

             string? searchTerm = null;
             var findButton = new Button("Find", is_default: true);
             findButton.Clicked += () => {
                 var input = invNumText.Text?.ToString()?.Trim();
                 if (!string.IsNullOrWhiteSpace(input))
                 {
                     searchTerm = input;
                     Application.RequestStop();
                 } else {
                     MessageBox.ErrorQuery("Input Required", "Please enter an Inventory Number.", "Ok");
                     invNumText.SetFocus();
                 }
             };
             var cancelButton = new Button("Cancel");
             cancelButton.Clicked += () => { Application.RequestStop(); };

             dialog.AddButton(findButton);
             dialog.AddButton(cancelButton);
             dialog.Loaded += () => invNumText.SetFocus();

             Application.Run(dialog);

             if (searchTerm != null)
             {
                 var results = inventory.Where(i => i.InventoryNumber.Equals(searchTerm, StringComparison.OrdinalIgnoreCase)).ToList();
                 PopulateInventoryTable(results);
                 UpdateStatus($"Found {results.Count} items with Inv# '{searchTerm}'.");
             } else {
                 UpdateStatus("Find by inventory number cancelled.");
             }
         }


        static void ListLocations()
        {
             // Display locations in a ListView within a dialog
             var dialog = new Dialog("Locations List", 40, 15);
             var sortedLocations = locations.OrderBy(l => l).ToList();
             var listView = new ListView(sortedLocations) {
                 X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill(1) // Fill space except button
             };
             var closeButton = new Button("Close", is_default: true);
             closeButton.Clicked += () => Application.RequestStop();

             dialog.Add(listView);
             dialog.AddButton(closeButton);
             dialog.Ready += () => listView.SetFocus(); // Focus the list on open

             Application.Run(dialog);
             UpdateStatus("Displayed locations list.");
        }

        static void AddLocation()
        {
            var dialog = new Dialog("Add New Location", 40, 7);
            int labelWidth = 15;
            int inputPadding = 1;

            var labelColumn = new View() { X = 1, Y = 1, Width = labelWidth, Height = 1 };
            var inputColumn = new View() { X = Pos.Right(labelColumn) + inputPadding, Y = 1, Width = Dim.Fill(2), Height = 1 };

            var nameLabel = new Label(0, 0, "Location Name:");
            var nameText = new TextField("") { X = 0, Y = 0, Width = Dim.Fill() };

            labelColumn.Add(nameLabel);
            inputColumn.Add(nameText);
            dialog.Add(labelColumn, inputColumn);

            bool locationAdded = false;
            var okButton = new Button("OK", is_default: true);
            okButton.Clicked += () => {
                var newLoc = nameText.Text?.ToString()?.Trim();
                 if (string.IsNullOrWhiteSpace(newLoc))
                 {
                     MessageBox.ErrorQuery("Validation Error", "Location name cannot be empty.", "Ok");
                     nameText.SetFocus(); return;
                 }
                 if (locations.Contains(newLoc, StringComparer.OrdinalIgnoreCase))
                 {
                      MessageBox.ErrorQuery("Validation Error", $"Location '{newLoc}' already exists.", "Ok");
                      nameText.SelectAll(); nameText.SetFocus(); return;
                 }

                 locations.Add(newLoc);
                 locations.Sort(); // Keep the list sorted
                try {
                     // dataStorage.SaveLocations(locations); // REMOVE Save immediately
                     hasUnsavedChanges = true; // Set flag
                     locationAdded = true;
                     Application.RequestStop();
                } catch (Exception ex) {
                     MessageBox.ErrorQuery("Save Error", $"Failed to save location: {ex.Message}", "Ok");
                }
            };

             var cancelButton = new Button("Cancel");
             cancelButton.Clicked += () => {
                 Application.RequestStop();
             };

             dialog.AddButton(okButton);
             dialog.AddButton(cancelButton);
             dialog.Loaded += () => nameText.SetFocus();

             Application.Run(dialog);

             if(locationAdded) {
                 UpdateStatus($"Location added successfully."); // UpdateStatus will add indicator
             } else {
                 UpdateStatus("Add location cancelled or failed."); // UpdateStatus will add indicator
             }
        }


        static void ExportToCsv()
        {
             var saveDialog = new SaveDialog("Export CSV", "Enter filename for CSV export");
             // saveDialog.DirectoryPath = ...; // Set initial directory if desired
             saveDialog.FilePath = (ustring)$"InventoryExport_{DateTime.Now:yyyyMMdd_HHmmss}.csv"; // Default name

             Application.Run(saveDialog);

             if (!saveDialog.Canceled && saveDialog.FilePath != null)
             {
                 string filePath = saveDialog.FilePath.ToString()!;
                 try
                 {
                     UpdateStatus("Exporting to CSV...");
                     csvManager.ExportToCsv(filePath);
                     ShowMessage("Export Successful", $"Inventory exported to '{Path.GetFileName(filePath)}'.");
                     UpdateStatus("Exported inventory to CSV.");
            }
            catch (Exception ex)
            {
                      ShowMessage("Export Error", $"Failed to export inventory: {ex.Message}");
                      UpdateStatus("CSV export failed.");
                 }
             }
             else
             {
                 UpdateStatus("CSV export cancelled.");
            }
        }

        static void ImportFromCsv()
        {
             var openDialog = new OpenDialog("Import CSV", "Select CSV file to import") { AllowsMultipleSelection = false };
            // openDialog.AllowedFileTypes = new[] { ".csv" }; // API might differ or not exist

             Application.Run(openDialog);

             if (!openDialog.Canceled && openDialog.FilePaths.Count > 0)
             {
                 string filePath = openDialog.FilePaths[0];
                 UpdateStatus("Importing from CSV...");
                 try
                 {
                     // CsvManager.ImportFromCsv handles loading, merging/assigning IDs, saving items and locations
                     csvManager.ImportFromCsv(filePath);

                     // Reload data from storage to reflect changes made by CsvManager
                     inventory = dataStorage.LoadItems();
                     locations = dataStorage.LoadLocations();
                     hasUnsavedChanges = false; // Data on disk IS current after import

                     // Refresh the UI
                     PopulateInventoryTable();

                     ShowMessage("Import Complete", $"Import process finished from '{Path.GetFileName(filePath)}'. Check table for results.");
                     UpdateStatus("Import process complete."); // UpdateStatus handles the flag display

                 }
                 catch (Exception ex)
                 {
                      ShowMessage("Import Error", $"Failed to import from '{Path.GetFileName(filePath)}': {ex.Message}");
                      UpdateStatus("CSV import failed.");
                 }
             }
             else
             {
                  UpdateStatus("CSV import cancelled.");
             }
        }

        static void SaveData()
        {
            try
            {
                UpdateStatus("Saving data...");
                dataStorage.SaveItems(inventory);
                dataStorage.SaveLocations(locations);
                hasUnsavedChanges = false; // Reset flag
                UpdateStatus("Data saved successfully.");
                // Optional: Show a brief confirmation message?
                // ShowMessage("Save", "Data saved successfully.");
            }
            catch (Exception ex)
            {
                MessageBox.ErrorQuery("Save Error", $"Failed to save data: {ex.Message}", "Ok");
                UpdateStatus("Save data failed.");
            }
        }

        static void RestoreData()
        {
            bool confirmed = ConfirmAction("Confirm Restore", "Reload data from disk?\nThis will discard any unsaved changes.");

            if (confirmed)
            {
                try
                {
                    UpdateStatus("Restoring data from disk...");
                    inventory = dataStorage.LoadItems();
                    locations = dataStorage.LoadLocations();
                    hasUnsavedChanges = false; // Reset flag
                    PopulateInventoryTable(); // Refresh the view with reloaded data
                    UpdateStatus("Data restored successfully from disk.");
                }
                catch (Exception ex)
                {
                     MessageBox.ErrorQuery("Restore Error", $"Failed to restore data: {ex.Message}", "Ok");
                     UpdateStatus("Restore data failed.");
                }
            }
            else
            {
                UpdateStatus("Restore data cancelled."); // UpdateStatus handles flag
            }
        }

        // --- Report Generation ---
         static void GenerateLabelsForAllItems()
         {
             UpdateStatus("Generating PDF for all items...");
             try {
                 pdfGenerator.GenerateAllItemsReport(inventory);
                 ShowMessage("PDF Generation", "Generated PDF for all items. Check application folder.");
                 UpdateStatus("PDF generation for all items complete.");
             } catch (Exception ex) {
                 ShowMessage("PDF Error", $"Failed to generate PDF: {ex.Message}");
                 UpdateStatus("PDF generation failed.");
             }
         }

         static void GenerateLabelsByLocation()
         {
             if (!locations.Any())
             {
                 ShowMessage("Error", "No locations defined. Cannot generate report by location.");
                    return;
                }

             var dialog = new Dialog("Select Location", 40, 15); // Increased height
             var listLabel = new Label("Select Location:") { X = 1, Y = 1 };
             var sortedLocations = locations.OrderBy(l => l).ToList();
             var locationListView = new ListView(sortedLocations) {
                 X = 1, Y = Pos.Bottom(listLabel),
                 Width = Dim.Fill(2), Height = Dim.Fill(2) // Fill available space
             };

             string? selectedLocation = null;
             var okButton = new Button("OK", is_default: true);
             okButton.Clicked += () => {
                 if (locationListView.SelectedItem >= 0 && locationListView.SelectedItem < sortedLocations.Count)
                 {
                     selectedLocation = sortedLocations[locationListView.SelectedItem];
                     Application.RequestStop();
                 } else {
                     MessageBox.ErrorQuery("Selection Required", "Please select a location from the list.", "Ok");
                 }
             };
             var cancelButton = new Button("Cancel");
             cancelButton.Clicked += () => {
                 Application.RequestStop();
             };

             dialog.Add(listLabel, locationListView);
             dialog.AddButton(okButton);
             dialog.AddButton(cancelButton);
             dialog.Loaded += () => locationListView.SetFocus();

             Application.Run(dialog);

             if (!string.IsNullOrEmpty(selectedLocation))
             {
                  UpdateStatus($"Generating PDF labels for location '{selectedLocation}'...");
                 try {
                      pdfGenerator.GenerateLocationReport(inventory, selectedLocation);
                      ShowMessage("PDF Generation", $"Generated PDF for location '{selectedLocation}'. Check application folder.");
                      UpdateStatus($"PDF generation for location '{selectedLocation}' complete.");
                  } catch (Exception ex) {
                      ShowMessage("PDF Error", $"Failed to generate PDF: {ex.Message}");
                      UpdateStatus("PDF generation failed.");
                  }
             } else {
                 UpdateStatus("PDF generation by location cancelled.");
             }
         }

         static void GenerateLabelsByInventoryNumber()
         {
              if (!inventory.Any())
              {
                  ShowMessage("Error", "Inventory is empty. Cannot generate report by inventory number.");
                  return;
              }

              var dialog = new Dialog("Enter Inventory Number", 40, 7);
              int labelWidth = 18; // Adjusted width
              int inputPadding = 1;

              var labelColumn = new View() { X = 1, Y = 1, Width = labelWidth, Height = 1 };
              var inputColumn = new View() { X = Pos.Right(labelColumn) + inputPadding, Y = 1, Width = Dim.Fill(2), Height = 1 };

              var invNumLabel = new Label(0, 0, "Inventory Number:");
              var invNumText = new TextField("") { X = 0, Y = 0, Width = Dim.Fill() };

              labelColumn.Add(invNumLabel);
              inputColumn.Add(invNumText);
              dialog.Add(labelColumn, inputColumn);

              string? selectedInvNum = null;
              var okButton = new Button("OK", is_default: true);
              okButton.Clicked += () => {
                  var input = invNumText.Text?.ToString()?.Trim();
                  if (!string.IsNullOrWhiteSpace(input))
                  {
                      selectedInvNum = input;
                      Application.RequestStop();
                  } else {
                       MessageBox.ErrorQuery("Input Required", "Please enter an Inventory Number.", "Ok");
                       invNumText.SetFocus();
                  }
              };
               var cancelButton = new Button("Cancel");
               cancelButton.Clicked += () => {
                   Application.RequestStop();
               };

               dialog.AddButton(okButton);
               dialog.AddButton(cancelButton);
               dialog.Loaded += () => invNumText.SetFocus();

               Application.Run(dialog);

               if (!string.IsNullOrEmpty(selectedInvNum))
               {
                    UpdateStatus($"Generating PDF labels for Inv# '{selectedInvNum}'...");
                    try {
                         pdfGenerator.GenerateInventoryNumberReport(inventory, selectedInvNum);
                         ShowMessage("PDF Generation", $"Generated PDF for Inv# '{selectedInvNum}'. Check application folder.");
                         UpdateStatus($"PDF generation for Inv# '{selectedInvNum}' complete.");
                     } catch (Exception ex) {
                         ShowMessage("PDF Error", $"Failed to generate PDF: {ex.Message}");
                         UpdateStatus("PDF generation failed.");
                     }
               } else {
                   UpdateStatus("PDF generation by inventory number cancelled.");
               }
         }

        // --- Add this new method to the Program class ---
        static void RequestQuit()
        {
            if (hasUnsavedChanges)
            {
                int result = MessageBox.Query("Unsaved Changes", "There are unsaved changes. Save before quitting?", "Save and Quit", "Discard and Quit", "Cancel");
                if (result == 0) // Save and Quit
                {
                    SaveData();
                    if (!hasUnsavedChanges) // Check if save was successful (flag is reset)
                    {
                         Application.RequestStop();
                    }
                    // else: Save failed, don't quit
                }
                else if (result == 1) // Discard and Quit
                {
                    Application.RequestStop();
                }
                // else if result == 2 (Cancel) or -1 (dialog closed), do nothing
            }
            else
            {
                Application.RequestStop(); // No unsaved changes, quit directly
            }
        }

        #region Spectre Console Methods (Commented Out)
        /*
        // Commented out or removed Spectre.Console specific methods like:
        // static void ShowItemsMenu(ref bool dataChanged) { ... }
        // static void ShowDictionaryMenu(ref bool dataChanged) { ... }
        // static void ShowLocationsMenu(ref bool dataChanged) { ... }
        // static void ShowServiceMenu(ref bool dataChanged) { ... }
        // static void ShowCsvMenu(ref bool dataChanged) { ... }
        // static void ShowReportsMenu() { ... }
        // static void Pause() { ... }
        // Original AddItem, ListItems, etc. using Spectre are implicitly replaced by Terminal.Gui versions
        */
        #endregion


    }
}