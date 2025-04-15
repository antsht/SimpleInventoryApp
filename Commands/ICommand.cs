using System;

namespace SimpleInventoryApp.Commands
{
    /// <summary>
    /// Interface for the Command Pattern implementation
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// Gets the name of the command
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Gets the key binding for the command (if applicable)
        /// </summary>
        Terminal.Gui.Key? KeyBinding { get; }
        
        /// <summary>
        /// Checks if the command can be executed in the current state
        /// </summary>
        bool CanExecute();
        
        /// <summary>
        /// Executes the command
        /// </summary>
        void Execute();
    }
} 