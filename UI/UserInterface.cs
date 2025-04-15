using System;
using Terminal.Gui;
using NStack;

namespace SimpleInventoryApp.UI
{
    public static class UserInterface
    {
        private static Label? statusLabel;
        private static bool hasUnsavedChanges = false;

        public static void Initialize(Label statusBarLabel)
        {
            statusLabel = statusBarLabel;
        }

        public static void SetHasUnsavedChanges(bool value)
        {
            hasUnsavedChanges = value;
        }

        public static bool GetHasUnsavedChanges()
        {
            return hasUnsavedChanges;
        }

        public static void UpdateStatus(string message)
        {
            if (statusLabel != null)
            {
                string fullMessage = message;
                if (hasUnsavedChanges)
                {
                    fullMessage += " [* unsaved changes]";
                }
                statusLabel.Text = fullMessage;
                Application.DoEvents(); // Ensure status update is processed
            }
        }

        public static void ShowMessage(string title, string message)
        {
            MessageBox.Query(title, message, "Ok");
        }

        public static bool ConfirmAction(string title, string message)
        {
            // Ensure width fits message roughly
            int width = Math.Max(title.Length, message.Split('\n').Max(s => s.Length)) + 10;
            int height = message.Split('\n').Length + 5;
            return MessageBox.Query(width, height, title, message, "Yes", "No") == 0; // 0 is the index of "Yes"
        }

        public static void RequestQuit()
        {
            if (hasUnsavedChanges)
            {
                int result = MessageBox.Query("Unsaved Changes", "There are unsaved changes. Save before quitting?", "Save and Quit", "Discard and Quit", "Cancel");
                if (result == 0) // Save and Quit
                {
                    // We need to call SaveData but it's in Program.cs now
                    // We'll have to refactor this later
                    Program.SaveData();
                    if (!hasUnsavedChanges) // Check if save was successful (flag is reset)
                    {
                        Application.RequestStop();
                    }
                    // else: Save failed, don't quit
                }
                else if (result == 1) // Discard and Quit
                {
                    Application.RequestStop();
                }
                // else if result == 2 (Cancel) or -1 (dialog closed), do nothing
            }
            else
            {
                Application.RequestStop(); // No unsaved changes, quit directly
            }
        }

        // --- New Method for Help Dialog ---
        public static void ShowHelpDialog()
        {
            var dialog = new Dialog("Help - Simple Inventory App", 60, 20); // Adjust size as needed

            string programName = "Simple Inventory App";
            string description = "A basic console application for managing inventory items and locations.";
            string hotkeys = 
                "Hotkeys:\n" +
                "------------------------------\n" +
                "  F1  - Show this Help\n" +
                "  F3  - Sort / Filter Inventory\n" +
                "  F5  - Save Data\n" +
                "  F6  - Restore Data\n" +
                "  F8  - Delete Selected Item (Use Delete Key in Table)\n" +
                "  F10 - Quit Application\n" +
                "\n" +
                "Table Navigation:\n" +
                "  Arrows - Move Selection\n" +
                "  Enter / Double Click - Edit Item\n" +
                "  Delete Key - Delete Selected Item";

            var infoLabel = new TextView() { // Use TextView for multi-line
                X = 1, Y = 1,
                Width = Dim.Fill(2),
                Height = Dim.Fill(4),
                ReadOnly = true,
                Text = $"{programName}\n\n{description}\n\n{hotkeys}"
            };
            
            var closeButton = new Button("Close");
            closeButton.Clicked += () => { Application.RequestStop(); };

            dialog.Add(infoLabel);
            dialog.AddButton(closeButton);

            closeButton.SetFocus(); // Explicitly set focus to the button
            Application.Run(dialog);
        }
    }
} 