using System;
using System.IO;
using Terminal.Gui;
using SimpleInventoryApp.Events;
// Explicitly qualify Attribute
using Attribute = Terminal.Gui.Attribute;

namespace SimpleInventoryApp.Services
{
    /// <summary>
    /// Service to manage application themes
    /// </summary>
    public class ThemeService : ISubject
    {
        private const string ThemeSettingFile = "theme.setting";
        private readonly string _themeFilePath; // Store the full path
        
        private readonly EventManager _eventManager;
        private AppTheme _currentTheme;
        private ColorScheme? _darkScheme;
        private ColorScheme? _lightScheme;
        private ColorScheme? _darkMenuScheme;   // Scheme for Menu/Status in Dark mode
        private ColorScheme? _lightMenuScheme;  // Scheme for Menu/Status in Light mode
        private bool _schemesInitialized = false;

        /// <summary>
        /// Creates a new instance of the ThemeService
        /// </summary>
        public ThemeService()
        {
            _eventManager = EventManager.Instance;
            _themeFilePath = Path.Combine(AppContext.BaseDirectory, ThemeSettingFile);
            // We'll initialize the theme from file, but delay scheme initialization
            LoadThemeFromFile();
        }

        /// <summary>
        /// Gets the current theme
        /// </summary>
        public AppTheme CurrentTheme => _currentTheme;

        /// <summary>
        /// Initializes the theme service. Should be called after Terminal.Gui.Application.Init()
        /// </summary>
        public void Initialize()
        {
            if (!_schemesInitialized)
            {
                InitializeSchemes();
                _schemesInitialized = true;
            }
        }

        /// <summary>
        /// Initializes the color schemes
        /// </summary>
        private void InitializeSchemes()
        {
            // --- Dark Scheme (Main Application) ---
            _darkScheme = new ColorScheme()
            {
                Normal = new Attribute(Color.Gray, Color.Black), // Standard text on black
                Focus = new Attribute(Color.Black, Color.Gray),   // Focused item: black text on gray bg
                HotNormal = new Attribute(Color.BrightYellow, Color.Black), // Hotkey text (yellow) on black bg
                HotFocus = new Attribute(Color.BrightYellow, Color.Gray),   // Focused hotkey: yellow text on gray bg
                Disabled = new Attribute(Color.DarkGray, Color.Black) // Disabled text on black bg
            };

            // --- Dark Scheme (Menu/Status Bar) ---
            // Slightly different background (e.g., DarkGray)
            _darkMenuScheme = new ColorScheme()
            {
                Normal = new Attribute(Color.Gray, Color.DarkGray), // Standard text on dark gray
                Focus = new Attribute(Color.Black, Color.Gray),   // Focused: same as main
                HotNormal = new Attribute(Color.BrightYellow, Color.DarkGray), // Hotkey text (yellow) on dark gray
                HotFocus = new Attribute(Color.BrightYellow, Color.Gray),   // Focused hotkey: same as main
                Disabled = new Attribute(Color.DarkGray, Color.DarkGray) // Disabled text on dark gray
            };


            // --- Light Scheme (Main Application - Blue Background) ---
            _lightScheme = new ColorScheme()
            {
                Normal = new Attribute(Color.White, Color.Blue),     // White text on blue bg
                Focus = new Attribute(Color.Black, Color.Cyan),     // Focused: black text on cyan bg
                HotNormal = new Attribute(Color.BrightYellow, Color.Blue), // Hotkey text (yellow) on blue bg
                HotFocus = new Attribute(Color.BrightYellow, Color.Cyan),   // Focused hotkey: yellow text on cyan bg
                Disabled = new Attribute(Color.Gray, Color.Blue)      // Disabled text on blue bg
            };
            
            // --- Light Scheme (Menu/Status Bar) ---
            // Slightly different background (e.g., BrightBlue)
            _lightMenuScheme = new ColorScheme()
            {
                Normal = new Attribute(Color.White, Color.BrightBlue), // White text on bright blue
                Focus = new Attribute(Color.Black, Color.Cyan),       // Focused: same as main
                HotNormal = new Attribute(Color.BrightYellow, Color.BrightBlue), // Hotkey text (yellow) on bright blue
                HotFocus = new Attribute(Color.BrightYellow, Color.Cyan),       // Focused hotkey: same as main
                Disabled = new Attribute(Color.Gray, Color.BrightBlue)    // Disabled text on bright blue
            };
        }

        /// <summary>
        /// Loads the theme from the settings file
        /// </summary>
        private void LoadThemeFromFile()
        {
            // Default to Dark if file doesn't exist or is invalid
            _currentTheme = AppTheme.Dark;
            
            if (File.Exists(_themeFilePath))
            {
                try
                {
                    string themeName = File.ReadAllText(_themeFilePath).Trim();
                    if (Enum.TryParse<AppTheme>(themeName, true, out AppTheme loadedTheme))
                    {
                        _currentTheme = loadedTheme;
                    }
                }
                catch (Exception ex)
                {
                    // Log error - fallback to default dark theme
                    Console.Error.WriteLine($"Error loading theme setting from {_themeFilePath}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Applies the current theme to the application's main view
        /// </summary>
        public void ApplyCurrentTheme()
        {
            // Make sure schemes are initialized
            if (!_schemesInitialized)
            {
                Initialize();
            }
            ApplyTheme(_currentTheme);
        }

        /// <summary>
        /// Applies a theme globally (mostly to Application.Top and global Colors)
        /// </summary>
        public void ApplyTheme(AppTheme theme)
        {
            _currentTheme = theme;
            // Use the MAIN scheme for Application.Top and global defaults
            ColorScheme mainSchemeToApply = (theme == AppTheme.Light) ? _lightScheme! : _darkScheme!;

            try
            {
                // Only apply to Top if it's available
                if (Terminal.Gui.Application.Top != null)
                {
                    Terminal.Gui.Application.Top.ColorScheme = mainSchemeToApply;
                    // Mark views for redraw
                    Terminal.Gui.Application.Top.SetNeedsDisplay();
                }
                
                // Set global colors - Use MAIN scheme for Dialog and Base
                // DO NOT set Colors.Menu globally here, Application.cs will handle MenuBar/StatusBar explicitly
                Colors.Dialog = mainSchemeToApply;
                Colors.Base = mainSchemeToApply; 
            }
            catch (Exception ex)
            {
                // Log error but don't crash
                Console.Error.WriteLine($"Theme error during ApplyTheme: {ex.Message}");
            }
            
            // Save the theme choice to file
            SaveThemeToFile();
            
            // Notify observers (like Application.cs) that the theme has changed
            NotifyObservers(EventType.ThemeChanged, theme);
        }

        /// <summary>
        /// Gets the ColorScheme corresponding to a specific theme.
        /// Ensures schemes are initialized before returning.
        /// </summary>
        /// <param name="theme">The theme to get the scheme for.</param>
        /// <returns>The main ColorScheme for the theme, or null if schemes are not initialized.</returns>
        public ColorScheme? GetSchemeForTheme(AppTheme theme)
        {
            if (!_schemesInitialized)
            {
                Initialize(); // Ensure schemes are ready (calls InitializeSchemes)
            }
            return theme == AppTheme.Light ? _lightScheme : _darkScheme;
        }

        /// <summary>
        /// Gets the specific ColorScheme for MenuBar/StatusBar corresponding to a specific theme.
        /// Ensures schemes are initialized before returning.
        /// </summary>
        /// <param name="theme">The theme to get the menu/status scheme for.</param>
        /// <returns>The menu/status ColorScheme for the theme, or null if schemes are not initialized.</returns>
        public ColorScheme? GetMenuStatusSchemeForTheme(AppTheme theme)
        {
            if (!_schemesInitialized)
            {
                Initialize(); // Ensure schemes are ready
            }
            return theme == AppTheme.Light ? _lightMenuScheme : _darkMenuScheme;
        }


        /// <summary>
        /// Cycles to the next theme
        /// </summary>
        public void CycleTheme()
        {
            _currentTheme = (_currentTheme == AppTheme.Dark) ? AppTheme.Light : AppTheme.Dark;
            ApplyTheme(_currentTheme); // ApplyTheme will handle saving and notifying
        }

        /// <summary>
        /// Saves the current theme choice to the settings file
        /// </summary>
        private void SaveThemeToFile()
        {
            try
            {
                File.WriteAllText(_themeFilePath, _currentTheme.ToString());
            }
            catch (Exception ex)
            {
                // Log error
                Console.Error.WriteLine($"Error saving theme setting to {_themeFilePath}: {ex.Message}");
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