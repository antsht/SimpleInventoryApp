using System;
using Terminal.Gui;
using SimpleInventoryApp.Services;

namespace SimpleInventoryApp.Commands
{
    /// <summary>
    /// Command to save data
    /// </summary>
    public class SaveDataCommand : ICommand
    {
        private readonly ApplicationService _appService;
        
        /// <summary>
        /// Creates a new instance of the SaveDataCommand
        /// </summary>
        public SaveDataCommand(ApplicationService appService)
        {
            _appService = appService ?? throw new ArgumentNullException(nameof(appService));
        }
        
        /// <summary>
        /// Gets the name of the command
        /// </summary>
        public string Name => "Save Data";
        
        /// <summary>
        /// Gets the key binding for the command
        /// </summary>
        public Key? KeyBinding => Key.F5;
        
        /// <summary>
        /// Checks if the command can be executed
        /// </summary>
        public bool CanExecute() => true;
        
        /// <summary>
        /// Executes the command
        /// </summary>
        public void Execute()
        {
            try
            {
                Console.WriteLine("SaveDataCommand: Initiating save operation...");
                _appService.SaveAllData();
                Console.WriteLine("SaveDataCommand: Save operation completed successfully");
                UI.UserInterface.UpdateStatus("Data saved successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SaveDataCommand Error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                UI.UserInterface.ShowMessage("Save Error", $"Failed to save data: {ex.Message}");
                UI.UserInterface.UpdateStatus("Failed to save data.");
            }
        }
    }
    
    /// <summary>
    /// Command to restore data
    /// </summary>
    public class RestoreDataCommand : ICommand
    {
        private readonly ApplicationService _appService;
        
        /// <summary>
        /// Creates a new instance of the RestoreDataCommand
        /// </summary>
        public RestoreDataCommand(ApplicationService appService)
        {
            _appService = appService ?? throw new ArgumentNullException(nameof(appService));
        }
        
        /// <summary>
        /// Gets the name of the command
        /// </summary>
        public string Name => "Restore Data";
        
        /// <summary>
        /// Gets the key binding for the command
        /// </summary>
        public Key? KeyBinding => Key.F6;
        
        /// <summary>
        /// Checks if the command can be executed
        /// </summary>
        public bool CanExecute() => true;
        
        /// <summary>
        /// Executes the command
        /// </summary>
        public void Execute()
        {
            bool confirmed = UI.UserInterface.ConfirmAction(
                "Confirm Restore", 
                "Reload data from disk?\nThis will discard any unsaved changes."
            );
            
            if (confirmed)
            {
                try
                {
                    UI.UserInterface.UpdateStatus("Restoring data from disk...");
                    _appService.RestoreAllData();
                    UI.UserInterface.UpdateStatus("Data restored successfully from disk.");
                }
                catch (Exception ex)
                {
                    UI.UserInterface.ShowMessage("Restore Error", $"Failed to restore data: {ex.Message}");
                    UI.UserInterface.UpdateStatus("Failed to restore data.");
                }
            }
            else
            {
                UI.UserInterface.UpdateStatus("Restore cancelled.");
            }
        }
    }
    
    /// <summary>
    /// Command to cycle through themes
    /// </summary>
    public class CycleThemeCommand : ICommand
    {
        private readonly ApplicationService _appService;
        
        /// <summary>
        /// Creates a new instance of the CycleThemeCommand
        /// </summary>
        public CycleThemeCommand(ApplicationService appService)
        {
            _appService = appService ?? throw new ArgumentNullException(nameof(appService));
        }
        
        /// <summary>
        /// Gets the name of the command
        /// </summary>
        public string Name => "Cycle Theme";
        
        /// <summary>
        /// Gets the key binding for the command
        /// </summary>
        public Key? KeyBinding => Key.F7;
        
        /// <summary>
        /// Checks if the command can be executed
        /// </summary>
        public bool CanExecute() => true;
        
        /// <summary>
        /// Executes the command
        /// </summary>
        public void Execute()
        {
            _appService.ThemeService.CycleTheme();
            UI.UserInterface.UpdateStatus($"Theme changed to {_appService.ThemeService.CurrentTheme}.");
        }
    }
    
    /// <summary>
    /// Command to quit the application
    /// </summary>
    public class QuitCommand : ICommand
    {
        private readonly ApplicationService _appService;
        
        /// <summary>
        /// Creates a new instance of the QuitCommand
        /// </summary>
        public QuitCommand(ApplicationService appService)
        {
            _appService = appService ?? throw new ArgumentNullException(nameof(appService));
        }
        
        /// <summary>
        /// Gets the name of the command
        /// </summary>
        public string Name => "Quit";
        
        /// <summary>
        /// Gets the key binding for the command
        /// </summary>
        public Key? KeyBinding => Key.F10;
        
        /// <summary>
        /// Checks if the command can be executed
        /// </summary>
        public bool CanExecute() => true;
        
        /// <summary>
        /// Executes the command
        /// </summary>
        public void Execute()
        {
            // Try to call the Application's HandleApplicationQuit method via reflection
            try
            {
                // First try to find the Application instance
                var appType = Type.GetType("SimpleInventoryApp.Application, SimpleInventoryApp");
                if (appType == null)
                {
                    // Fallback to direct save dialog
                    Console.WriteLine("Could not find Application type - falling back to direct save dialog");
                    ShowSaveConfirmationDialog();
                    return;
                }
                
                // Try to find the current Application instance
                var appField = appType.GetField("_instance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                object? appInstance = null;
                
                if (appField != null)
                {
                    appInstance = appField.GetValue(null);
                }
                
                if (appInstance == null)
                {
                    // If we can't get the instance, use direct save dialog
                    Console.WriteLine("Could not get Application instance - falling back to direct save dialog");
                    ShowSaveConfirmationDialog();
                    return;
                }
                
                // Try to call the HandleApplicationQuit method
                var method = appType.GetMethod("HandleApplicationQuit", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (method != null)
                {
                    Console.WriteLine("Found HandleApplicationQuit method - calling it directly");
                    method.Invoke(appInstance, null);
                }
                else
                {
                    // If method doesn't exist, use direct save dialog
                    Console.WriteLine("Could not find HandleApplicationQuit method - falling back to direct save dialog");
                    ShowSaveConfirmationDialog();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in QuitCommand: {ex.Message}");
                // Fallback to direct save dialog
                ShowSaveConfirmationDialog();
            }
        }
        
        /// <summary>
        /// Shows a save confirmation dialog if there are unsaved changes
        /// </summary>
        private void ShowSaveConfirmationDialog()
        {
            Console.WriteLine("QuitCommand: Checking for unsaved changes...");
            Console.WriteLine($"AppService.HasUnsavedChanges(): {_appService.HasUnsavedChanges()}");
            Console.WriteLine($"UserInterface.GetHasUnsavedChanges(): {UI.UserInterface.GetHasUnsavedChanges()}");
            
            // Check both the AppService and UserInterface
            bool hasUnsavedChanges = _appService.HasUnsavedChanges() || UI.UserInterface.GetHasUnsavedChanges();
            
            if (hasUnsavedChanges)
            {
                int result = MessageBox.Query(
                    "Unsaved Changes", 
                    "There are unsaved changes. Save before quitting?", 
                    "Save and Quit", 
                    "Discard and Quit", 
                    "Cancel"
                );
                
                if (result == 0) // Save and Quit
                {
                    try
                    {
                        Console.WriteLine("Saving all data before quitting...");
                        _appService.SaveAllData();
                        Console.WriteLine("Data saved successfully. Exiting application.");
                        Terminal.Gui.Application.RequestStop();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error saving data: {ex.Message}");
                        UI.UserInterface.ShowMessage("Save Error", $"Failed to save data: {ex.Message}");
                    }
                }
                else if (result == 1) // Discard and Quit
                {
                    Console.WriteLine("Discarding changes and exiting application.");
                    Terminal.Gui.Application.RequestStop();
                }
                // else: Cancel, do nothing
            }
            else
            {
                Console.WriteLine("No unsaved changes. Exiting application.");
                Terminal.Gui.Application.RequestStop();
            }
        }
    }
} 