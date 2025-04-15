using System;
using SimpleInventoryApp.Commands;
using SimpleInventoryApp.Events;

namespace SimpleInventoryApp.Services
{
    /// <summary>
    /// Central service manager for the application
    /// </summary>
    public class ApplicationService
    {
        private static ApplicationService? _instance;
        private static readonly object _lockObject = new object();
        
        private readonly DataStorage _dataStorage;
        private readonly CsvManager _csvManager;
        private readonly PdfLabelGenerator _pdfGenerator;
        
        /// <summary>
        /// Gets the singleton instance of the ApplicationService
        /// </summary>
        public static ApplicationService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lockObject)
                    {
                        _instance ??= new ApplicationService();
                    }
                }
                return _instance;
            }
        }
        
        /// <summary>
        /// Gets the inventory service
        /// </summary>
        public InventoryService InventoryService { get; }
        
        /// <summary>
        /// Gets the location service
        /// </summary>
        public LocationService LocationService { get; }
        
        /// <summary>
        /// Gets the theme service
        /// </summary>
        public ThemeService ThemeService { get; }
        
        /// <summary>
        /// Gets the command manager
        /// </summary>
        public CommandManager CommandManager { get; }
        
        /// <summary>
        /// Gets the data storage
        /// </summary>
        public DataStorage DataStorage => _dataStorage;
        
        /// <summary>
        /// Gets the CSV manager
        /// </summary>
        public CsvManager CsvManager => _csvManager;
        
        /// <summary>
        /// Gets the PDF generator
        /// </summary>
        public PdfLabelGenerator PdfGenerator => _pdfGenerator;
        
        /// <summary>
        /// Creates a new instance of the ApplicationService
        /// </summary>
        private ApplicationService()
        {
            _dataStorage = new DataStorage();
            _csvManager = new CsvManager(_dataStorage);
            _pdfGenerator = new PdfLabelGenerator();
            
            // Create services
            InventoryService = new InventoryService(_dataStorage);
            LocationService = new LocationService(_dataStorage);
            ThemeService = new ThemeService();
            CommandManager = new CommandManager();
            
            // Register services as observers
            RegisterServices();
        }
        
        /// <summary>
        /// Registers services as observers
        /// </summary>
        private void RegisterServices()
        {
            // This will be implemented as we develop the UI components
            // that need to observe events from the services
        }
        
        /// <summary>
        /// Saves all data
        /// </summary>
        public void SaveAllData()
        {
            InventoryService.SaveInventory();
            LocationService.SaveLocations();
        }
        
        /// <summary>
        /// Restores all data
        /// </summary>
        public void RestoreAllData()
        {
            InventoryService.RestoreInventory();
            LocationService.RestoreLocations();
        }
        
        /// <summary>
        /// Checks if there are any unsaved changes
        /// </summary>
        public bool HasUnsavedChanges()
        {
            return InventoryService.HasUnsavedChanges || LocationService.HasUnsavedChanges;
        }
    }
} 