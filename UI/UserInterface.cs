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
    }
} 