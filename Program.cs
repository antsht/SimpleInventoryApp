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
    // --- Theme Enum ---
    public enum AppTheme { Dark, Light }
    // ------------------

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
        private static FrameView? mainWin; // Corrected type to FrameView?
        private static Label? statusLabel; // Label inside the status bar
        private static TableView? inventoryTableView; // Table to display inventory

        // --- Theme Management ---
        private static AppTheme currentTheme = AppTheme.Dark; // Default
        private const string ThemeSettingFile = "theme.setting";
        private static ColorScheme? darkScheme;
        private static ColorScheme? lightScheme;
        // ----------------------

        static void Main(string[] args)
        {
            // QuestPDF Initialization
            QuestPDF.Settings.License = LicenseType.Community;

            // Load data first
            inventory = dataStorage.LoadItems();
            locations = dataStorage.LoadLocations();

            // Initialize Application
            Application.Init();

            // --- Define Color Schemes ---
            // Dark Scheme (VS Code like)
            darkScheme = new ColorScheme()
            {
                Normal = new Terminal.Gui.Attribute(Terminal.Gui.Color.Gray, Terminal.Gui.Color.Black),
                Focus = new Terminal.Gui.Attribute(Terminal.Gui.Color.Black, Terminal.Gui.Color.Gray), 
                HotNormal = new Terminal.Gui.Attribute(Terminal.Gui.Color.BrightYellow, Terminal.Gui.Color.Black),
                HotFocus = new Terminal.Gui.Attribute(Terminal.Gui.Color.BrightYellow, Terminal.Gui.Color.Gray), 
                Disabled = new Terminal.Gui.Attribute(Terminal.Gui.Color.DarkGray, Terminal.Gui.Color.Black)
            };

            // Light Scheme (Norton Commander like - Blue Background)
            lightScheme = new ColorScheme()
            {
                 Normal = new Terminal.Gui.Attribute(Terminal.Gui.Color.White, Terminal.Gui.Color.Blue),
                 Focus = new Terminal.Gui.Attribute(Terminal.Gui.Color.Black, Terminal.Gui.Color.Cyan),   
                 HotNormal = new Terminal.Gui.Attribute(Terminal.Gui.Color.BrightYellow, Terminal.Gui.Color.Blue),  // Use BrightYellow
                 HotFocus = new Terminal.Gui.Attribute(Terminal.Gui.Color.BrightYellow, Terminal.Gui.Color.Cyan), // Use BrightYellow
                 Disabled = new Terminal.Gui.Attribute(Terminal.Gui.Color.Gray, Terminal.Gui.Color.Blue) 
            };
            // --------------------------

            // --- Load and Apply Initial Theme ---
            LoadAndApplyTheme();
            // -----------------------------------

            var top = Application.Top;

            // Main Window - Occupies the space between MenuBar and StatusBar
            mainWin = new FrameView("Inventory")
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
                    new MenuItem ("_Cycle Theme", "", () => CycleTheme(), null, null, Key.F7),
                    new MenuItem ("_Quit", "", () => UserInterface.RequestQuit(), null, null, Key.F10)
                })
            });

            // Status Bar
            statusLabel = new Label("Ready") { AutoSize = false, Width = Dim.Fill(), TextAlignment = TextAlignment.Left };
            statusBar = new StatusBar(new StatusItem[] {
                // NEW ORDER:
                // Help: F1
                new StatusItem(Key.F1, "~F1~ Help", () => UserInterface.ShowHelpDialog()),
                // Sort/Filter: F3
                new StatusItem(Key.F3, "~F3~ Sort/Filter", () => InventoryOperations.ShowSortFilterDialog()),
                // Save: F5
                new StatusItem(Key.F5, "~F5~ Save", () => DataOperations.SaveData()),
                // Restore: F6
                new StatusItem(Key.F6, "~F6~ Restore", () => {
                    DataOperations.RestoreData();
                }),
                // Delete: F8 
                new StatusItem(Key.F8, "~F8~ Delete", () => {
                    var selectedItem = InventoryTable.GetSelectedItem();
                    if (selectedItem != null) {
                        InventoryOperations.DeleteItem(selectedItem);
                    } else {
                        UserInterface.ShowMessage("No Selection", "Please select an item in the table first.");
                    }
                }),
                // Cycle Theme: F7
                new StatusItem(Key.F7, "~F7~ Theme", () => CycleTheme()),
                // Quit: F10
                new StatusItem(Key.F10, "~F10~ Quit", () => UserInterface.RequestQuit())
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
                editItemAction: (item) =>
                {
                    // Call InventoryOperations.EditItem with the item to edit
                    InventoryOperations.EditItem(item);
                    // Re-initialize InventoryTable to ensure it has updated references
                    InventoryTable.Initialize(inventory);
                },
                deleteItemAction: (item) =>
                {
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

        // --- Theme Management Methods ---
        private static void LoadAndApplyTheme()
        {
            // Default to Dark if file doesn't exist or is invalid
            currentTheme = AppTheme.Dark; 
            if (File.Exists(ThemeSettingFile))
            {
                try
                {
                    string themeName = File.ReadAllText(ThemeSettingFile).Trim();
                    if (Enum.TryParse<AppTheme>(themeName, true, out AppTheme loadedTheme))
                    {
                        currentTheme = loadedTheme;
                    }
                }
                catch (Exception ex)
                {
                    // Log error or ignore - fallback to default dark theme
                    Console.Error.WriteLine($"Error loading theme setting: {ex.Message}");
                }
            }
            ApplyTheme(currentTheme);
        }

        private static void ApplyTheme(AppTheme theme)
        {   
            ColorScheme schemeToApply = (theme == AppTheme.Light) ? lightScheme! : darkScheme!; // Use null-forgiving as we define them before calling this

            Application.Top.ColorScheme = schemeToApply;
            Colors.Dialog = schemeToApply;
            Colors.Menu = schemeToApply;
            Colors.Base = schemeToApply;

            // Mark views for redraw
            Application.Top.SetNeedsDisplay(); 
        }

        private static void SaveThemeSetting(AppTheme theme)
        {
            try
            {   
                File.WriteAllText(ThemeSettingFile, theme.ToString());
            }
            catch (Exception ex)
            {
                // Log error or show message? For now, just log to console.
                Console.Error.WriteLine($"Error saving theme setting: {ex.Message}");
                UserInterface.ShowMessage("Theme Error", $"Could not save theme setting: {ex.Message}");
            }
        }
        
        private static void CycleTheme()
        {
            currentTheme = (currentTheme == AppTheme.Dark) ? AppTheme.Light : AppTheme.Dark;
            ApplyTheme(currentTheme);
            SaveThemeSetting(currentTheme);
            UserInterface.UpdateStatus($"Theme changed to {currentTheme}.");
        }
        // ------------------------------
    }
}