using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Terminal.Gui;
using NStack;

namespace SimpleInventoryApp.UI
{
    public static class InventoryTable
    {
        private static TableView? inventoryTableView;
        private static List<InventoryItem> inventory = new List<InventoryItem>();
        
        public static void Initialize(List<InventoryItem> inventoryItems)
        {
            inventory = inventoryItems;
        }
        
        public static TableView SetupInventoryTableView(Action<InventoryItem> editItemAction, Action<InventoryItem> deleteItemAction)
        {
            if (inventoryTableView != null)
            {
                inventoryTableView.Dispose(); // Dispose if exists
            }

            inventoryTableView = new TableView()
            {
                X = 1,
                Y = 0,
                Width = Dim.Fill(1), // Keep space for scrollbar/right border
                Height = Dim.Fill(), // Fill parent's client area
                FullRowSelect = true, // Select the whole row
                // Ensure the TableView's own border is defined
                Border = new Border()
                {
                    BorderStyle = BorderStyle.Rounded,
                    BorderBrush = Color.Gray
                }
            };

            // Define table structure
            var dataTable = new DataTable();
            dataTable.Columns.Add("Location", typeof(string));
            dataTable.Columns.Add("Inv Num", typeof(string));
            dataTable.Columns.Add("Name", typeof(string));
            dataTable.Columns.Add("Description", typeof(string));
            dataTable.Columns.Add("Qty", typeof(int));
            dataTable.Columns.Add("Last Updated", typeof(string)); 
            dataTable.Columns.Add("ID", typeof(int));
            inventoryTableView.Table = dataTable;

            // --- Configure TableStyle --- 
            var tableStyle = new TableView.TableStyle
            {
                 ShowHorizontalHeaderOverline = true, 
                 ShowVerticalCellLines = true, 
                 ExpandLastColumn = false, 
                 SmoothHorizontalScrolling = true,
                 ColumnStyles = new Dictionary<DataColumn, TableView.ColumnStyle> {
                     [dataTable.Columns["Location"]!] = new TableView.ColumnStyle { MinWidth = 15, MaxWidth = 30, MinAcceptableWidth = 15  }, 
                     [dataTable.Columns["Inv Num"]!] = new TableView.ColumnStyle { MinWidth = 10, MaxWidth = 20 }, 
                     [dataTable.Columns["Name"]!] = new TableView.ColumnStyle { MinWidth = 10, MaxWidth = 80, MinAcceptableWidth = 30 }, 
                     [dataTable.Columns["Description"]!] = new TableView.ColumnStyle { MinWidth = 10, MaxWidth = 30}, 
                     [dataTable.Columns["Qty"]!] = new TableView.ColumnStyle { MinWidth = 3, MaxWidth = 5 },
                     [dataTable.Columns["Last Updated"]!] = new TableView.ColumnStyle { MinWidth = 14, MaxWidth = 16 },
                     [dataTable.Columns["ID"]!] = new TableView.ColumnStyle { MinWidth = 4, MaxWidth = 6 },
                 }
            };
            inventoryTableView.Style = tableStyle;
            // ---------------------------

            // Event handling for cell activation (double-click)
            inventoryTableView.CellActivated += (args) => {
                var table = args.Table;
                if(table != null && args.Row >= 0 && args.Row < table.Rows.Count) {
                    try {
                        // Get data directly from the DataTable
                        var row = table.Rows[args.Row];
                        
                        // Add debug message to see what we're getting
                        string rowData = $"Row selected: ";
                        foreach (DataColumn col in table.Columns)
                        {
                            rowData += $"{col.ColumnName}={row[col]}; ";
                        }
                        UserInterface.UpdateStatus(rowData);
                        
                        // Find the actual InventoryItem object based on ID
                        if (row["ID"] != null && int.TryParse(row["ID"].ToString(), out int itemId)) {
                            var itemToEdit = inventory.FirstOrDefault(item => item.Id == itemId);
                            if (itemToEdit != null) {
                                // Add debug message to confirm item found
                                UserInterface.UpdateStatus($"Found item: ID={itemToEdit.Id}, Name={itemToEdit.Name}");
                                editItemAction(itemToEdit); // Call the edit action
                            } else {
                                UserInterface.ShowMessage("Error", $"Could not find item with ID {itemId} in the main list.");
                            }
                        } else {
                            UserInterface.ShowMessage("Error", "Could not determine Item ID from the selected row.");
                        }
                    } catch (Exception ex) {
                        UserInterface.ShowMessage("Error", $"Failed to get item details or initiate edit: {ex.Message}");
                    }
                }
            };

            // Delete Key Handling
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
                                    // Need to ensure UI updates properly after modal dialog
                                    Terminal.Gui.Application.MainLoop.AddTimeout(TimeSpan.FromMilliseconds(10), (_) => {
                                        deleteItemAction(itemToDelete); // Call delete logic after a short delay
                                        return false; // Run only once
                                    });
                                } else {
                                    UserInterface.ShowMessage("Error", $"Could not find item with ID {itemId} in the main list for deletion.");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            UserInterface.ShowMessage("Error", $"Failed to delete item: {ex.Message}");
                        }
                    }
                }
            };
            return inventoryTableView;
        }

        public static void PopulateInventoryTable(List<InventoryItem>? items = null)
        {
            if (inventoryTableView == null || inventoryTableView.Table == null) return;

            var dataTable = inventoryTableView.Table;
            dataTable.Clear(); // Clear existing rows

            // Use provided items or fall back to the full inventory
            var displayItems = items ?? inventory;

            // Add rows for each inventory item
            foreach (var item in displayItems)
            {
                var row = dataTable.NewRow();
                row["Location"] = item.Location;
                row["Inv Num"] = item.InventoryNumber;
                row["Name"] = item.Name;
                row["Description"] = item.Description; 
                row["Qty"] = item.Quantity;
                row["Last Updated"] = item.LastUpdated.ToLocalTime().ToString("g"); // Format as short date and time
                row["ID"] = item.Id;
                dataTable.Rows.Add(row);
            }

            // Refresh the table view
            inventoryTableView.SetNeedsDisplay();
        }

        // --- New method to get the selected item --- 
        public static InventoryItem? GetSelectedItem()
        {
            if (inventoryTableView == null || inventoryTableView.Table == null || inventoryTableView.SelectedRow < 0)
            {
                return null; // No table or no selection
            }

            int selectedRowIndex = inventoryTableView.SelectedRow;
            if (selectedRowIndex >= inventoryTableView.Table.Rows.Count)
            {
                 return null; // Selection out of bounds (e.g., after delete/filter)
            }
            
            try
            {
                var table = inventoryTableView.Table;
                var row = table.Rows[selectedRowIndex];
                if (int.TryParse(row["ID"]?.ToString(), out int itemId))
                {
                    // Find the item in the main inventory list by ID
                    return inventory.FirstOrDefault(item => item.Id == itemId);
                }
            }
            catch (Exception ex) // Index errors, etc.
            {
                // Log error? For now, just return null
                Console.Error.WriteLine($"Error getting selected item: {ex.Message}"); 
                return null;
            }

            return null; // ID parsing failed or other issue
        }
        // ----------------------------------------
    }
} 