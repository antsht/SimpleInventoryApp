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
        public Key? KeyBinding => Key.F2;
        
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

    /// <summary>
    /// Command to copy the selected inventory item
    /// </summary>
    public class CopyItemCommand : ICommand
    {
        private readonly ApplicationService _appService;
        
        /// <summary>
        /// Creates a new instance of the CopyItemCommand
        /// </summary>
        public CopyItemCommand(ApplicationService appService)
        {
            _appService = appService ?? throw new ArgumentNullException(nameof(appService));
        }
        
        /// <summary>
        /// Gets the name of the command
        /// </summary>
        public string Name => "Copy Item";
        
        /// <summary>
        /// Gets the key binding for the command
        /// </summary>
        public Key? KeyBinding => Key.F4;
        
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
                try
                {
                    // Create a copy of the selected item
                    var copiedItem = new InventoryItem
                    {
                        // ID will be set by the storage service
                        InventoryNumber = selectedItem.InventoryNumber,
                        Name = selectedItem.Name,
                        Description = selectedItem.Description + " (Copy)",
                        Quantity = selectedItem.Quantity,
                        Location = selectedItem.Location,
                        LastUpdated = DateTime.Now
                    };
                    
                    // Add the copied item to inventory
                    _appService.InventoryService.AddItem(copiedItem);
                    
                    // Update the status
                    UserInterface.UpdateStatus($"Item copied. Now editing the copy.");
                    
                    // Open the edit dialog for the newly created item
                    Operations.InventoryOperations.EditItem(copiedItem);
                    
                    // Refresh the inventory table
                    InventoryTable.Initialize(_appService.InventoryService.GetInventory());
                    InventoryTable.PopulateInventoryTable();
                }
                catch (Exception ex)
                {
                    UserInterface.ShowMessage("Copy Error", $"Failed to copy item: {ex.Message}");
                }
            }
            else
            {
                UserInterface.ShowMessage("No Selection", "Please select an item in the table first.");
            }
        }
    }
} 