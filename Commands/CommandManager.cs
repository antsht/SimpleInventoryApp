using System;
using System.Collections.Generic;
using Terminal.Gui;

namespace SimpleInventoryApp.Commands
{
    /// <summary>
    /// Manages commands in the application
    /// </summary>
    public class CommandManager
    {
        private readonly Dictionary<string, ICommand> _commands = new();

        /// <summary>
        /// Registers a command with the manager
        /// </summary>
        public void RegisterCommand(ICommand command)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));
            
            _commands[command.Name] = command;
        }

        /// <summary>
        /// Gets a command by name
        /// </summary>
        public ICommand? GetCommand(string name)
        {
            return _commands.TryGetValue(name, out var command) ? command : null;
        }

        /// <summary>
        /// Gets all registered commands
        /// </summary>
        public IEnumerable<ICommand> GetAllCommands()
        {
            return _commands.Values;
        }

        /// <summary>
        /// Executes a command by name
        /// </summary>
        public bool ExecuteCommand(string name)
        {
            var command = GetCommand(name);
            if (command != null && command.CanExecute())
            {
                command.Execute();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Creates a MenuItem for a command
        /// </summary>
        public MenuItem CreateMenuItem(ICommand command)
        {
            return new MenuItem(
                command.Name,
                "",
                () => { if (command.CanExecute()) command.Execute(); },
                null,
                null,
                command.KeyBinding ?? Key.Null
            );
        }
    }
} 