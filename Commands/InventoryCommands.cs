using System;
using Terminal.Gui;
using SimpleInventoryApp.Services;
using SimpleInventoryApp.UI;
using SimpleInventoryApp.Operations;

namespace SimpleInventoryApp.Commands
{
    /// <summary>
    /// Command to add a new inventory item
    /// </summary>
    public class AddItemCommand : ICommand
    {
        private readonly ApplicationService _appService;
        
        /// <summary>
        /// Creates a new instance of the AddItemCommand
        /// </summary>
        public AddItemCommand(ApplicationService appService)
        {
            _appService = appService ?? throw new ArgumentNullException(nameof(appService));
        }
        
        /// <summary>
        /// Gets the name of the command
        /// </summary>
        public string Name => "Add New Item";
        
        /// <summary>
        /// Gets the key binding for the command
        /// </summary>
        public Key? KeyBinding => null;
        
        /// <summary>
        /// Checks if the command can be executed
        /// </summary>
        public bool CanExecute() => true;
        
        /// <summary>
        /// Executes the command
        /// </summary>
        public void Execute()
        {
            // Re-use existing UI and connect it to our new service
            InventoryOperations.AddItem();
        }
    }
    
    /// <summary>
    /// Command to list all inventory items
    /// </summary>
    public class ListAllItemsCommand : ICommand
    {
        private readonly ApplicationService _appService;
        
        /// <summary>
        /// Creates a new instance of the ListAllItemsCommand
        /// </summary>
        public ListAllItemsCommand(ApplicationService appService)
        {
            _appService = appService ?? throw new ArgumentNullException(nameof(appService));
        }
        
        /// <summary>
        /// Gets the name of the command
        /// </summary>
        public string Name => "List All Items";
        
        /// <summary>
        /// Gets the key binding for the command
        /// </summary>
        public Key? KeyBinding => null;
        
        /// <summary>
        /// Checks if the command can be executed
        /// </summary>
        public bool CanExecute() => true;
        
        /// <summary>
        /// Executes the command
        /// </summary>
        public void Execute()
        {
            // Re-use existing UI and connect it to our new service
            InventoryOperations.ListAllItems();
        }
    }
    
    /// <summary>
    /// Command to show sort/filter dialog
    /// </summary>
    public class SortFilterCommand : ICommand
    {
        private readonly ApplicationService _appService;
        
        /// <summary>
        /// Creates a new instance of the SortFilterCommand
        /// </summary>
        public SortFilterCommand(ApplicationService appService)
        {
            _appService = appService ?? throw new ArgumentNullException(nameof(appService));
        }
        
        /// <summary>
        /// Gets the name of the command
        /// </summary>
        public string Name => "Sort/Filter";
        
        /// <summary>
        /// Gets the key binding for the command
        /// </summary>
        public Key? KeyBinding => Key.F3;
        
        /// <summary>
        /// Checks if the command can be executed
        /// </summary>
        public bool CanExecute() => true;
        
        /// <summary>
        /// Executes the command
        /// </summary>
        public void Execute()
        {
            // Re-use existing UI and connect it to our new service
            InventoryOperations.ShowSortFilterDialog();
        }
    }
    
    /// <summary>
    /// Command to delete the selected inventory item
    /// </summary>
    public class DeleteItemCommand : ICommand
    {
        private readonly ApplicationService _appService;
        
        /// <summary>
        /// Creates a new instance of the DeleteItemCommand
        /// </summary>
        public DeleteItemCommand(ApplicationService appService)
        {
            _appService = appService ?? throw new ArgumentNullException(nameof(appService));
        }
        
        /// <summary>
        /// Gets the name of the command
        /// </summary>
        public string Name => "Delete Item";
        
        /// <summary>
        /// Gets the key binding for the command
        /// </summary>
        public Key? KeyBinding => Key.F8;
        
        /// <summary>
        /// Checks if the command can be executed
        /// </summary>
        public bool CanExecute() => true;
        
        /// <summary>
        /// Executes the command
        /// </summary>
        public void Execute()
        {
            var selectedItem = InventoryTable.GetSelectedItem();
            
            if (selectedItem != null)
            {
                // Re-use existing UI and connect it to our new service
                InventoryOperations.DeleteItem(selectedItem);
            }
            else
            {
                UserInterface.ShowMessage("No Selection", "Please select an item in the table first.");
            }
        }
    }
} 