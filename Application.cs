using System;
using System.Collections.Generic;
using Terminal.Gui;
using SimpleInventoryApp.Commands;
using SimpleInventoryApp.Events;
using SimpleInventoryApp.Services;
using SimpleInventoryApp.UI;
using SimpleInventoryApp.Operations;

namespace SimpleInventoryApp
{
    /// <summary>
    /// Main application class
    /// </summary>
    public class Application : IObserver
    {
        private readonly ApplicationService _appService;
        private readonly CommandManager _commandManager;
        
        // Terminal.Gui Views
        private MenuBar? _menu;
        private StatusBar? _statusBar;
        private FrameView? _mainWindow;
        private TableView? _inventoryTableView;
        
        /// <summary>
        /// Creates a new instance of the Application
        /// </summary>
        public Application()
        {
            _appService = ApplicationService.Instance;
            _commandManager = _appService.CommandManager;
            
            // Register as observer
            _appService.InventoryService.RegisterObserver(EventType.InventoryChanged, this);
            _appService.LocationService.RegisterObserver(EventType.LocationsChanged, this);
            _appService.ThemeService.RegisterObserver(EventType.ThemeChanged, this);
        }
        
        /// <summary>
        /// Initializes the application
        /// </summary>
        public void Initialize()
        {
            // Register commands
            RegisterCommands();
        }
        
        /// <summary>
        /// Runs the application
        /// </summary>
        public void Run()
        {
            try
            {
                // Initialize Terminal.Gui
                Terminal.Gui.Application.Init();
                
                // Initialize theme service after Terminal.Gui is initialized
                _appService.ThemeService.Initialize();

                // Apply the theme
                _appService.ThemeService.ApplyCurrentTheme();

                // Set up the UI
                SetupUI();
                
                // Initialize operations with the existing static classes
                RegisterExistingOperations();
                
                // Show the initial inventory
                Operations.InventoryOperations.ListAllItems();
                
                // Run the application
                Terminal.Gui.Application.Run();
                Terminal.Gui.Application.Shutdown();
                
                Console.WriteLine("Inventory saved. Goodbye!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Sets up the UI
        /// </summary>
        private void SetupUI()
        {
            var top = Terminal.Gui.Application.Top;
            
            // Main Window - Occupies the space between MenuBar and StatusBar
            _mainWindow = new FrameView("Inventory")
            {
                X = 0,
                Y = 1, // Below MenuBar
                Width = Dim.Fill(),
                Height = Dim.Fill(1) // Fill remaining space minus 1 for StatusBar
            };
            
            // Set up the menu
            _menu = CreateMenuBar();
            
            // Set up the status bar
            var statusLabel = new Label("Ready") { AutoSize = false, Width = Dim.Fill(), TextAlignment = TextAlignment.Left };
            _statusBar = CreateStatusBar(statusLabel);
            
            // Add Views to Toplevel
            top.Add(_menu, _mainWindow, _statusBar);
            
            // Initialize UI components
            UserInterface.Initialize(statusLabel);
            
            // Setup the Inventory Table
            SetupInventoryTable();
        }
        
        /// <summary>
        /// Creates the menu bar
        /// </summary>
        private MenuBar CreateMenuBar()
        {
            var itemsMenu = new MenuBarItem("_Items", new MenuItem[]
            {
                _commandManager.CreateMenuItem(_commandManager.GetCommand("List All Items") ?? throw new InvalidOperationException("Command not found")),
                _commandManager.CreateMenuItem(_commandManager.GetCommand("Sort/Filter") ?? throw new InvalidOperationException("Command not found")),
                _commandManager.CreateMenuItem(_commandManager.GetCommand("Add New Item") ?? throw new InvalidOperationException("Command not found")),
                _commandManager.CreateMenuItem(_commandManager.GetCommand("Copy Item") ?? throw new InvalidOperationException("Command not found"))
            });
            
            var dictionaryMenu = new MenuBarItem("_Dictionary", new MenuItem[]
            {
                new MenuBarItem("_Locations", new MenuItem[]
                {
                    new MenuItem("_List", "", () => Operations.LocationOperations.ListLocations(), null, null, Key.Null),
                    new MenuItem("_Add New...", "", () => Operations.LocationOperations.AddLocation(), null, null, Key.Null)
                })
            });
            
            var serviceMenu = new MenuBarItem("_Service", new MenuItem[]
            {
                new MenuBarItem("_CSV", new MenuItem[]
                {
                    new MenuItem("_Export...", "", () => Operations.DataOperations.ExportToCsv(), null, null, Key.Null),
                    new MenuItem("_Import...", "", () => Operations.DataOperations.ImportFromCsv(), null, null, Key.Null)
                })
            });
            
            var reportsMenu = new MenuBarItem("_Reports", new MenuItem[]
            {
                new MenuBarItem("Generate _Labels PDF", new MenuItem[]
                {
                    new MenuItem("_All Items", "", () => Operations.ReportOperations.GenerateLabelsForAllItems(), null, null, Key.Null),
                    new MenuItem("By _Location...", "", () => Operations.ReportOperations.GenerateLabelsByLocation(), null, null, Key.Null),
                    new MenuItem("By Inv _Number...", "", () => Operations.ReportOperations.GenerateLabelsByInventoryNumber(), null, null, Key.Null)
                })
            });
            
            var fileMenu = new MenuBarItem("_File", new MenuItem[]
            {
                _commandManager.CreateMenuItem(_commandManager.GetCommand("Cycle Theme") ?? throw new InvalidOperationException("Command not found")),
                _commandManager.CreateMenuItem(_commandManager.GetCommand("Quit") ?? throw new InvalidOperationException("Command not found"))
            });
            
            return new MenuBar(new MenuBarItem[] { itemsMenu, dictionaryMenu, serviceMenu, reportsMenu, fileMenu });
        }
        
        /// <summary>
        /// Creates the status bar
        /// </summary>
        private StatusBar CreateStatusBar(Label statusLabel)
        {
            // Set up the status bar items
            var statusBar = new StatusBar(new StatusItem[]
            {
                new StatusItem(Key.F1, "~F1~ Help", () => UserInterface.ShowHelpDialog()),
                new StatusItem(
                    (_commandManager.GetCommand("Add New Item") as AddItemCommand)?.KeyBinding ?? Key.F2,
                    "~F2~ Add",
                    () => _commandManager.ExecuteCommand("Add New Item")
                ),
                new StatusItem(Key.F3, "~F3~ Sort/Filter", () => Operations.InventoryOperations.ShowSortFilterDialog()),
                new StatusItem(
                    (_commandManager.GetCommand("Copy Item") as CopyItemCommand)?.KeyBinding ?? Key.F4,
                    "~F4~ Copy",
                    () => _commandManager.ExecuteCommand("Copy Item")
                ),
                new StatusItem(
                    (_commandManager.GetCommand("Save Data") as SaveDataCommand)?.KeyBinding ?? Key.F5,
                    "~F5~ Save",
                    () => _commandManager.ExecuteCommand("Save Data")
                ),
                new StatusItem(
                    (_commandManager.GetCommand("Restore Data") as RestoreDataCommand)?.KeyBinding ?? Key.F6,
                    "~F6~ Restore",
                    () => Operations.DataOperations.RestoreData()
                ),
                new StatusItem(
                    (_commandManager.GetCommand("Delete Item") as DeleteItemCommand)?.KeyBinding ?? Key.F8,
                    "~F8~ Delete",
                    () => _commandManager.ExecuteCommand("Delete Item")
                ),
                new StatusItem(
                    (_commandManager.GetCommand("Cycle Theme") as CycleThemeCommand)?.KeyBinding ?? Key.F7,
                    "~F7~ Theme",
                    () => _commandManager.ExecuteCommand("Cycle Theme")
                ),
                new StatusItem(
                    (_commandManager.GetCommand("Quit") as QuitCommand)?.KeyBinding ?? Key.F10,
                    "~F10~ Quit",
                    () => _commandManager.ExecuteCommand("Quit")
                )
            });
            
            // Add the status label
            statusBar.Add(statusLabel);
            
            return statusBar;
        }
        
        /// <summary>
        /// Sets up the inventory table
        /// </summary>
        private void SetupInventoryTable()
        {
            InventoryTable.Initialize(_appService.InventoryService.GetInventory());
            
            _inventoryTableView = InventoryTable.SetupInventoryTableView(
                editItemAction: (item) =>
                {
                    // Call InventoryOperations.EditItem with the item to edit
                    Operations.InventoryOperations.EditItem(item);
                    // Re-initialize InventoryTable to ensure it has updated references
                    InventoryTable.Initialize(_appService.InventoryService.GetInventory());
                },
                deleteItemAction: (item) =>
                {
                    // Call InventoryOperations.DeleteItem with the item to delete  
                    Operations.InventoryOperations.DeleteItem(item);
                    // Re-initialize InventoryTable to ensure it has updated references
                    InventoryTable.Initialize(_appService.InventoryService.GetInventory());
                }
            );
            
            if (_mainWindow != null)
            {
                _mainWindow.Add(_inventoryTableView);
            }
        }
        
        /// <summary>
        /// Registers commands with the command manager
        /// </summary>
        private void RegisterCommands()
        {
            _commandManager.RegisterCommand(new SaveDataCommand(_appService));
            _commandManager.RegisterCommand(new RestoreDataCommand(_appService));
            _commandManager.RegisterCommand(new CycleThemeCommand(_appService));
            _commandManager.RegisterCommand(new QuitCommand(_appService));
            _commandManager.RegisterCommand(new AddItemCommand(_appService));
            _commandManager.RegisterCommand(new ListAllItemsCommand(_appService));
            _commandManager.RegisterCommand(new SortFilterCommand(_appService));
            _commandManager.RegisterCommand(new DeleteItemCommand(_appService));
            _commandManager.RegisterCommand(new CopyItemCommand(_appService));
        }
        
        /// <summary>
        /// Registers existing operations with the services
        /// </summary>
        private void RegisterExistingOperations()
        {
            // Re-use existing operations for now, without casting
            Operations.InventoryOperations.Initialize(
                _appService.InventoryService.GetInventory(),
                _appService.LocationService.GetLocations(),
                _appService.DataStorage
            );
            
            Operations.LocationOperations.Initialize(
                _appService.LocationService.GetLocations(),
                _appService.DataStorage
            );
            
            Operations.DataOperations.Initialize(
                _appService.InventoryService.GetInventory(),
                _appService.LocationService.GetLocations(),
                _appService.DataStorage,
                _appService.CsvManager
            );
            
            Operations.ReportOperations.Initialize(
                _appService.InventoryService.GetInventory(),
                _appService.LocationService.GetLocations(),
                _appService.PdfGenerator
            );
        }
        
        /// <summary>
        /// Handles events from the services
        /// </summary>
        public void Update(EventType eventType, object? data)
        {
            switch (eventType)
            {
                case EventType.InventoryChanged:
                    // Refresh the inventory table
                    InventoryTable.PopulateInventoryTable(Operations.InventoryOperations.ApplySortAndFilter());
                    break;
                    
                case EventType.LocationsChanged:
                    // Nothing to do here yet
                    break;
                    
                case EventType.ThemeChanged:
                    // Apply the theme directly to MenuBar and StatusBar using the specific schemes
                    if (data is AppTheme theme)
                    {
                        // Get the specific scheme for Menu/Status Bar
                        var menuStatusScheme = _appService.ThemeService.GetMenuStatusSchemeForTheme(theme);
                        if (menuStatusScheme != null)
                        {
                            if (_menu != null)
                            {
                                _menu.ColorScheme = menuStatusScheme;
                                _menu.SetNeedsDisplay();
                            }
                            if (_statusBar != null)
                            {
                                _statusBar.ColorScheme = menuStatusScheme;
                                _statusBar.SetNeedsDisplay();
                            }
                            // Force a general refresh to ensure everything redraws correctly
                            Terminal.Gui.Application.Refresh(); 
                        }
                        // Note: The main content area uses the scheme set on Application.Top
                        // which is handled by ThemeService.ApplyTheme itself.
                    }
                    break;
            }
        }
    }
} 