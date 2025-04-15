using System;
using System.Collections.Generic;

namespace SimpleInventoryApp.Events
{
    /// <summary>
    /// Central manager for handling application events
    /// </summary>
    public class EventManager : ISubject
    {
        private readonly Dictionary<EventType, List<IObserver>> _observers = new();

        /// <summary>
        /// Singleton instance of the EventManager
        /// </summary>
        public static EventManager Instance { get; } = new EventManager();

        // Private constructor for singleton
        private EventManager()
        {
            // Initialize with empty lists for each event type
            foreach (EventType eventType in Enum.GetValues(typeof(EventType)))
            {
                _observers[eventType] = new List<IObserver>();
            }
        }

        /// <summary>
        /// Registers an observer for a specific event type
        /// </summary>
        public void RegisterObserver(EventType eventType, IObserver observer)
        {
            if (!_observers[eventType].Contains(observer))
            {
                _observers[eventType].Add(observer);
            }
        }

        /// <summary>
        /// Removes an observer for a specific event type
        /// </summary>
        public void RemoveObserver(EventType eventType, IObserver observer)
        {
            _observers[eventType].Remove(observer);
        }

        /// <summary>
        /// Notifies all observers of a specific event type
        /// </summary>
        public void NotifyObservers(EventType eventType, object? data)
        {
            foreach (var observer in _observers[eventType])
            {
                observer.Update(eventType, data);
            }
        }
    }
} 