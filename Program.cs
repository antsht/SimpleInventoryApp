using System;
using QuestPDF.Infrastructure;

namespace SimpleInventoryApp
{
    /// <summary>
    /// Theme types
    /// </summary>
    public enum AppTheme { Dark, Light }

    /// <summary>
    /// Program entry point
    /// </summary>
    class Program
    {
        /// <summary>
        /// Main entry point
        /// </summary>
        static void Main(string[] args)
        {
            // QuestPDF Initialization
            QuestPDF.Settings.License = LicenseType.Community;

            try
            {
                // Create and run the application
                var app = new Application();
                app.Initialize();
                app.Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fatal error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }
    }
} 