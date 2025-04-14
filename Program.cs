using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Data;
using QuestPDF.Infrastructure;
using Terminal.Gui;
using NStack;
using SimpleInventoryApp.UI;
using SimpleInventoryApp.Operations;

namespace SimpleInventoryApp
{
    class Program
    {
        // Data storage
        static readonly DataStorage dataStorage = new DataStorage(); // Uses default filenames
        static readonly CsvManager csvManager = new CsvManager(dataStorage);
        static List<InventoryItem> inventory = new List<InventoryItem>();
        static List<string> locations = new List<string>(); // New list for locations
        // PDF generator
        static readonly PdfLabelGenerator pdfGenerator = new PdfLabelGenerator();

        // Terminal.Gui Views
        private static MenuBar? menu;
        private static StatusBar? statusBar;
        private static Window? mainWin; // Main content window
        private static Label? statusLabel; // Label inside the status bar
        private static TableView? inventoryTableView; // Table to display inventory

        static void Main(string[] args)
        {
            // QuestPDF Initialization
            QuestPDF.Settings.License = LicenseType.Community;

            // Load data first
            inventory = dataStorage.LoadItems();
            locations = dataStorage.LoadLocations();

            // Initialize Application
            Application.Init();

            var top = Application.Top;

            // Main Window - Occupies the space between MenuBar and StatusBar
            mainWin = new Window("Inventory")
            {
                X = 0,
                Y = 1, // Below MenuBar
                Width = Dim.Fill(),
                Height = Dim.Fill(1) // Fill remaining space minus 1 for StatusBar
            };

            // Menu Bar
            menu = new MenuBar(new MenuBarItem[] {
                new MenuBarItem ("_Items", new MenuItem [] {
                    new MenuItem ("_List All", "", () => InventoryOperations.ListAllItems(), null, null, (Key)0),
                    new MenuItem ("_Sort / Filter...", "", () => InventoryOperations.ShowSortFilterDialog(), null, null, Key.F5),
                    new MenuItem ("_Add New...", "", () => InventoryOperations.AddItem(), null, null, (Key)0),
                    new MenuItem ("_Update Quantity...", "", () => InventoryOperations.UpdateItemQuantity(), null, null, (Key)0),
                    new MenuItem ("_Update Location...", "", () => InventoryOperations.UpdateItemLocation(), null, null, (Key)0),
                    new MenuItem ("_Delete Record...", "", () => InventoryOperations.DeleteItem(), null, null, (Key)0),
                    new MenuItem ("Find by _Name...", "", () => InventoryOperations.FindItemByName(), null, null, (Key)0),
                    new MenuItem ("Find by Inv _Num...", "", () => InventoryOperations.FindItemByInvNum(), null, null, (Key)0)
                }),
                new MenuBarItem ("_Dictionary", new MenuItem [] {
                     new MenuBarItem ("_Locations", new MenuItem [] {
                        new MenuItem ("_List", "", () => LocationOperations.ListLocations(), null, null, (Key)0),
                        new MenuItem ("_Add New...", "", () => LocationOperations.AddLocation(), null, null, (Key)0)
                    })
                }),
                 new MenuBarItem ("_Service", new MenuItem [] {
                     new MenuBarItem ("_CSV", new MenuItem [] {
                        new MenuItem ("_Export...", "", () => DataOperations.ExportToCsv(), null, null, (Key)0),
                        new MenuItem ("_Import...", "", () => DataOperations.ImportFromCsv(), null, null, (Key)0),
                    })
                }),
                new MenuBarItem ("_Reports", new MenuItem [] {
                     new MenuBarItem ("Generate _Labels PDF", new MenuItem [] {
                        new MenuItem ("_All Items", "", () => ReportOperations.GenerateLabelsForAllItems(), null, null, (Key)0),
                        new MenuItem ("By _Location...", "", () => ReportOperations.GenerateLabelsByLocation(), null, null, (Key)0),
                        new MenuItem ("By Inv _Number...", "", () => ReportOperations.GenerateLabelsByInventoryNumber(), null, null, (Key)0),
                    })
                }),
                new MenuBarItem ("_File", new MenuItem [] {
                    new MenuItem ("_Quit", "", () => UserInterface.RequestQuit(), null, null, Key.Q | Key.CtrlMask)
                })
            });

            // Status Bar
            statusLabel = new Label("Ready") { AutoSize = false, Width = Dim.Fill(), TextAlignment = TextAlignment.Left };
            statusBar = new StatusBar(new StatusItem[] {
                // Shortcut to quit
                new StatusItem(Key.Q | Key.CtrlMask, "~^Q~ Quit", () => UserInterface.RequestQuit()),
                // Shortcut for Save
                new StatusItem(Key.S | Key.CtrlMask, "~^S~ Save", () => DataOperations.SaveData()),
                // Shortcut for Restore
                new StatusItem(Key.R | Key.CtrlMask, "~^R~ Restore", () => {
                    Application.MainLoop.AddTimeout(TimeSpan.FromMilliseconds(10), (_) => {
                          DataOperations.RestoreData();
                          return false; 
                     });
                }),
                // Shortcut for deleting selected item (NOTE: Currently non-functional from status bar)
                new StatusItem(Key.D | Key.CtrlMask, "~^D~ Delete", () => {
                    // Deleting via status bar shortcut is tricky as we don't have direct access 
                    // to the selected item here. The Delete key press is handled in InventoryTable.cs.
                    UserInterface.ShowMessage("Delete Shortcut", "Use the Delete key on the selected item in the table.");
                    // Application.MainLoop.AddTimeout(TimeSpan.FromMilliseconds(10), (_) => {
                          // var selectedItem = InventoryOperations.GetSelectedItem(); // Incorrect
                          // if (selectedItem != null) {
                          //    InventoryOperations.DeleteItem(selectedItem);
                          // } else {
                          //    UserInterface.ShowMessage("Delete", "No item selected.");
                          // }
                          // return false; 
                     // });
                }),
                // Shortcut for Sort/Filter
                new StatusItem(Key.F5, "~F5~ Sort/Filter", () => InventoryOperations.ShowSortFilterDialog())
            });
            statusBar.Add(statusLabel); // Add the label view directly to the status bar

            // Add Views to Toplevel
            top.Add(menu, mainWin, statusBar);

            // Initialize UI components
            UserInterface.Initialize(statusLabel);

            // Initialize Operations with a reference to inventory and locations
            InventoryOperations.Initialize(inventory, locations, dataStorage);
            LocationOperations.Initialize(locations, dataStorage);
            DataOperations.Initialize(inventory, locations, dataStorage, csvManager);
            ReportOperations.Initialize(inventory, locations, pdfGenerator);

            // Setup the Inventory Table with a reference to the same inventory collection
            InventoryTable.Initialize(inventory);
            inventoryTableView = InventoryTable.SetupInventoryTableView(
                editItemAction: (item) => {
                    // Call InventoryOperations.EditItem with the item to edit
                    InventoryOperations.EditItem(item);
                    // Re-initialize InventoryTable to ensure it has updated references
                    InventoryTable.Initialize(inventory);
                }, 
                deleteItemAction: (item) => {
                    // Call InventoryOperations.DeleteItem with the item to delete  
                    InventoryOperations.DeleteItem(item);
                    // Re-initialize InventoryTable to ensure it has updated references
                    InventoryTable.Initialize(inventory);
                }
            );
            mainWin.Add(inventoryTableView);

            // Show initial inventory
            InventoryOperations.ListAllItems();

            // Run Application
            Application.Run();
            Application.Shutdown();

            Console.WriteLine("Inventory saved. Goodbye!"); // Simple console message on exit
        }

        // Public methods needed from other classes
        public static void SaveData()
        {
            DataOperations.SaveData();
        }
    }
} 