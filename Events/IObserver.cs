using System;

namespace SimpleInventoryApp.Events
{
    /// <summary>
    /// Defines types of events in the application
    /// </summary>
    public enum EventType
    {
        DataChanged,
        InventoryChanged,
        LocationsChanged,
        ThemeChanged,
        StatusChanged,
        UnsavedChangesChanged
    }

    /// <summary>
    /// Interface for objects that can observe events
    /// </summary>
    public interface IObserver
    {
        /// <summary>
        /// Called when an event occurs
        /// </summary>
        void Update(EventType eventType, object? data);
    }

    /// <summary>
    /// Interface for objects that can be observed
    /// </summary>
    public interface ISubject
    {
        /// <summary>
        /// Registers an observer for a specific event type
        /// </summary>
        void RegisterObserver(EventType eventType, IObserver observer);

        /// <summary>
        /// Removes an observer for a specific event type
        /// </summary>
        void RemoveObserver(EventType eventType, IObserver observer);

        /// <summary>
        /// Notifies all observers of a specific event type
        /// </summary>
        void NotifyObservers(EventType eventType, object? data);
    }
} 