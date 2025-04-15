using System;
using System.Collections.Generic;
using SimpleInventoryApp.Events;

namespace SimpleInventoryApp.Services
{
    /// <summary>
    /// Service to manage inventory data
    /// </summary>
    public class InventoryService : ISubject
    {
        private readonly DataStorage _dataStorage;
        private readonly List<InventoryItem> _inventory;
        private readonly EventManager _eventManager;
        private bool _hasUnsavedChanges;

        /// <summary>
        /// Creates a new instance of the InventoryService
        /// </summary>
        public InventoryService(DataStorage dataStorage)
        {
            _dataStorage = dataStorage ?? throw new ArgumentNullException(nameof(dataStorage));
            _inventory = _dataStorage.LoadItems() ?? new List<InventoryItem>();
            _eventManager = EventManager.Instance;
            _hasUnsavedChanges = false;
        }

        /// <summary>
        /// Gets the list of inventory items
        /// </summary>
        public List<InventoryItem> GetInventory() => _inventory;

        /// <summary>
        /// Gets whether there are unsaved changes
        /// </summary>
        public bool HasUnsavedChanges => _hasUnsavedChanges;

        /// <summary>
        /// Adds an item to the inventory
        /// </summary>
        public void AddItem(InventoryItem item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            // Assign a new ID if needed
            if (item.Id <= 0)
            {
                item.Id = _dataStorage.GetNextId(_inventory);
            }
            
            item.LastUpdated = DateTime.Now;
            _inventory.Add(item);
            
            SetUnsavedChanges(true);
            NotifyObservers(EventType.InventoryChanged, item);
        }

        /// <summary>
        /// Updates an existing item in the inventory
        /// </summary>
        public void UpdateItem(InventoryItem item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            var existingItem = _inventory.Find(i => i.Id == item.Id);
            if (existingItem == null)
                throw new InvalidOperationException($"Item with ID {item.Id} not found");

            // Update the existing item's properties
            existingItem.InventoryNumber = item.InventoryNumber;
            existingItem.Name = item.Name;
            existingItem.Description = item.Description;
            existingItem.Quantity = item.Quantity;
            existingItem.Location = item.Location;
            existingItem.LastUpdated = DateTime.Now;

            SetUnsavedChanges(true);
            NotifyObservers(EventType.InventoryChanged, existingItem);
        }

        /// <summary>
        /// Removes an item from the inventory
        /// </summary>
        public void DeleteItem(InventoryItem item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            
            var removed = _inventory.RemoveAll(i => i.Id == item.Id) > 0;
            
            if (removed)
            {
                SetUnsavedChanges(true);
                NotifyObservers(EventType.InventoryChanged, item);
            }
        }

        /// <summary>
        /// Saves the inventory to storage
        /// </summary>
        public void SaveInventory()
        {
            _dataStorage.SaveItems(_inventory);
            SetUnsavedChanges(false);
            NotifyObservers(EventType.DataChanged, null);
        }

        /// <summary>
        /// Reloads the inventory from storage
        /// </summary>
        public void RestoreInventory()
        {
            var loadedItems = _dataStorage.LoadItems() ?? new List<InventoryItem>();
            
            _inventory.Clear();
            _inventory.AddRange(loadedItems);
            
            SetUnsavedChanges(false);
            
            // Ensure UI components are properly updated
            NotifyObservers(EventType.InventoryChanged, null);
            
            // Force a UI refresh of the inventory table
            UI.InventoryTable.Initialize(_inventory);
            UI.InventoryTable.PopulateInventoryTable();
        }

        /// <summary>
        /// Sets the unsaved changes flag
        /// </summary>
        private void SetUnsavedChanges(bool value)
        {
            if (_hasUnsavedChanges != value)
            {
                _hasUnsavedChanges = value;
                NotifyObservers(EventType.UnsavedChangesChanged, _hasUnsavedChanges);
            }
        }

        /// <summary>
        /// Registers an observer for a specific event type
        /// </summary>
        public void RegisterObserver(EventType eventType, IObserver observer)
        {
            _eventManager.RegisterObserver(eventType, observer);
        }

        /// <summary>
        /// Removes an observer for a specific event type
        /// </summary>
        public void RemoveObserver(EventType eventType, IObserver observer)
        {
            _eventManager.RemoveObserver(eventType, observer);
        }

        /// <summary>
        /// Notifies all observers of a specific event type
        /// </summary>
        public void NotifyObservers(EventType eventType, object? data)
        {
            _eventManager.NotifyObservers(eventType, data);
        }
    }
} 