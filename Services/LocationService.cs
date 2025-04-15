using System;
using System.Collections.Generic;
using SimpleInventoryApp.Events;

namespace SimpleInventoryApp.Services
{
    /// <summary>
    /// Service to manage location data
    /// </summary>
    public class LocationService : ISubject
    {
        private readonly DataStorage _dataStorage;
        private readonly List<string> _locations;
        private readonly EventManager _eventManager;
        private bool _hasUnsavedChanges;

        /// <summary>
        /// Creates a new instance of the LocationService
        /// </summary>
        public LocationService(DataStorage dataStorage)
        {
            _dataStorage = dataStorage ?? throw new ArgumentNullException(nameof(dataStorage));
            _locations = _dataStorage.LoadLocations() ?? new List<string>();
            _eventManager = EventManager.Instance;
            _hasUnsavedChanges = false;
        }

        /// <summary>
        /// Gets the list of locations
        /// </summary>
        public List<string> GetLocations() => _locations;

        /// <summary>
        /// Gets whether there are unsaved changes
        /// </summary>
        public bool HasUnsavedChanges => _hasUnsavedChanges;

        /// <summary>
        /// Adds a location
        /// </summary>
        public void AddLocation(string location)
        {
            if (string.IsNullOrWhiteSpace(location))
                throw new ArgumentException("Location cannot be empty", nameof(location));

            if (!_locations.Contains(location))
            {
                _locations.Add(location);
                
                // Sort the locations for user experience
                _locations.Sort();
                
                SetUnsavedChanges(true);
                NotifyObservers(EventType.LocationsChanged, location);
            }
        }

        /// <summary>
        /// Removes a location
        /// </summary>
        public void RemoveLocation(string location)
        {
            if (string.IsNullOrWhiteSpace(location))
                throw new ArgumentException("Location cannot be empty", nameof(location));

            var removed = _locations.Remove(location);
            
            if (removed)
            {
                SetUnsavedChanges(true);
                NotifyObservers(EventType.LocationsChanged, location);
            }
        }

        /// <summary>
        /// Saves the locations to storage
        /// </summary>
        public void SaveLocations()
        {
            _dataStorage.SaveLocations(_locations);
            SetUnsavedChanges(false);
            NotifyObservers(EventType.DataChanged, null);
        }

        /// <summary>
        /// Reloads the locations from storage
        /// </summary>
        public void RestoreLocations()
        {
            var loadedLocations = _dataStorage.LoadLocations() ?? new List<string>();
            
            _locations.Clear();
            _locations.AddRange(loadedLocations);
            
            SetUnsavedChanges(false);
            
            // Ensure proper notification for UI updates
            NotifyObservers(EventType.LocationsChanged, null);
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