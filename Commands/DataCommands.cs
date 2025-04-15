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
                _appService.SaveAllData();
                UI.UserInterface.UpdateStatus("Data saved successfully.");
            }
            catch (Exception ex)
            {
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
            if (_appService.HasUnsavedChanges())
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
                        _appService.SaveAllData();
                        Terminal.Gui.Application.RequestStop();
                    }
                    catch (Exception ex)
                    {
                        UI.UserInterface.ShowMessage("Save Error", $"Failed to save data: {ex.Message}");
                    }
                }
                else if (result == 1) // Discard and Quit
                {
                    Terminal.Gui.Application.RequestStop();
                }
                // else: Cancel, do nothing
            }
            else
            {
                Terminal.Gui.Application.RequestStop();
            }
        }
    }
} 