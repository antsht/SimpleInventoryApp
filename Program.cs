using System;
using System.Collections.Generic;
using System.Linq;
using Spectre.Console;
using QuestPDF.Infrastructure; // <-- Add QuestPDF using

namespace SimpleInventoryApp
{
    class Program
    {
        // Keep DataStorage instance, it now handles both files
        static readonly DataStorage dataStorage = new DataStorage(); // Uses default filenames
        static readonly CsvManager csvManager = new CsvManager(dataStorage);
        static List<InventoryItem> inventory = new List<InventoryItem>();
        static List<string> locations = new List<string>(); // New list for locations
        // Instantiate the PDF generator
        static readonly PdfLabelGenerator pdfGenerator = new PdfLabelGenerator();

        static void Main(string[] args)
        {
            // --- QuestPDF Initialization ---
            QuestPDF.Settings.License = LicenseType.Community;
            // -------------------------------

            // Load both data sets
            inventory = dataStorage.LoadItems();
            locations = dataStorage.LoadLocations();

            AnsiConsole.Clear(); // Start with a clear screen
            AnsiConsole.Write(
                new FigletText("Inventory Mgr")
                .Centered()
                .Color(Spectre.Console.Color.Blue));
            AnsiConsole.WriteLine();

            bool dataChanged = false; // Flag to track if saving is needed
            bool keepRunning = true;

            while (keepRunning)
            {
                AnsiConsole.Clear(); // Clear screen before showing top menu
                AnsiConsole.Write(new Rule("[yellow]Main Menu[/]").RuleStyle("blue").LeftJustified());

                var topChoice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Select a category:")
                        .PageSize(5)
                        .AddChoices(new[] {
                            "Items", "Dictionary", "Service", "Reports", "[red]Exit[/]"
                        }));

                dataChanged = false; // Reset flag for each top-level action

                switch (topChoice)
                {
                    case "Items":
                        ShowItemsMenu(ref dataChanged);
                        break;
                    case "Dictionary":
                        ShowDictionaryMenu(ref dataChanged);
                        break;
                    case "Service":
                        ShowServiceMenu(ref dataChanged);
                        break;
                    case "Reports":
                        ShowReportsMenu();
                        break;
                    case "[red]Exit[/]":
                        keepRunning = false;
                        break;
                }

                // Save changes if any modifying action was taken within submenus
                if (dataChanged)
                {
                    dataStorage.SaveItems(inventory);
                    dataStorage.SaveLocations(locations); // Save locations too!
                    AnsiConsole.MarkupLine("[green]Data saved successfully.[/]");
                    Pause(); // Pause only after saving
                }
                else if (keepRunning) // Pause after non-modifying actions unless exiting
                {
                     // Don't pause if a sub-menu was exited immediately without action
                     // (Requires more complex state tracking, or just accept the pause)
                     // Simple approach: Always pause if not exiting and no data changed.
                     // Consider if Pause() is needed after just viewing menus.
                     // For now, let's pause unless Exiting.
                     Pause();
                }
            }

            // Final save on exit (optional, could rely on saves after each action)
            // dataStorage.SaveItems(inventory);
            // dataStorage.SaveLocations(locations);
            AnsiConsole.MarkupLine("[green]Inventory saved. Goodbye![/]");
        }

        // --- Sub-Menu Methods ---

        static void ShowItemsMenu(ref bool dataChanged)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new Rule("[green]Items Menu[/]").RuleStyle("green").LeftJustified());

            var itemChoices = new List<string> {
                "List All Items",
                "Add New Item/Component",
                "Update Item Quantity",
                "Update Item Location",
                "Delete Item Record",
                "Find Item by Name",
                "Find Item by Inventory Number",
                 "[yellow]< Back[/]" // Option to go back
            };

            // Disable adding/updating location if no locations defined
            if (!locations.Any())
            {
                itemChoices.Remove("Add New Item/Component");
                itemChoices.Remove("Update Item Location");
                itemChoices.Insert(1, "[grey]Add New Item/Component (Add locations first)[/]"); // Show disabled option
                itemChoices.Insert(3, "[grey]Update Item Location (Add locations first)[/]"); // Show disabled option
            }


            var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Item Actions:")
                        .PageSize(10)
                        .AddChoices(itemChoices)
                        );

            switch (choice)
            {
                case "List All Items":
                    ListItems();
                    break;
                case "Add New Item/Component":
                    if (locations.Any()) { AddItem(); dataChanged = true; }
                    else { AnsiConsole.MarkupLine("[yellow]Please add at least one location first.[/]"); }
                    break;
                case "Update Item Quantity":
                    UpdateItemQuantity(); dataChanged = true;
                    break;
                case "Update Item Location":
                     if (locations.Any()) { UpdateItemLocation(); dataChanged = true; }
                    else { AnsiConsole.MarkupLine("[yellow]Please add at least one location first.[/]"); }
                    break;
                case "Delete Item Record":
                    DeleteItem(); dataChanged = true;
                    break;
                case "Find Item by Name":
                    FindItemByName();
                    break;
                case "Find Item by Inventory Number":
                    FindItemByInventoryNumber();
                    break;
                case string s when s.StartsWith("[grey]"): // Handle disabled options
                     AnsiConsole.MarkupLine("[yellow]This option is disabled. Please add locations first.[/]");
                     break;
                case "[yellow]< Back[/]":
                    return; // Return to main menu
                default:
                     AnsiConsole.MarkupLine($"[grey]Option '{choice}' selected.[/]");
                     break;
            }
             // Don't pause here, pause is handled in the main loop after returning
        }

        static void ShowDictionaryMenu(ref bool dataChanged)
        {
             AnsiConsole.Clear();
             AnsiConsole.Write(new Rule("[blue]Dictionary Menu[/]").RuleStyle("blue").LeftJustified());

             var dictChoice = AnsiConsole.Prompt(
                 new SelectionPrompt<string>()
                     .Title("Dictionary Actions:")
                     .PageSize(5)
                     .AddChoices(new[] { "Locations", "[yellow]< Back[/]" })
             );

             switch (dictChoice)
             {
                 case "Locations":
                     ShowLocationsMenu(ref dataChanged);
                     break;
                 case "[yellow]< Back[/]":
                     return; // Return to main menu
             }
             // Pause handled in main loop
        }

        static void ShowLocationsMenu(ref bool dataChanged)
        {
             AnsiConsole.Clear();
             AnsiConsole.Write(new Rule("[cyan]Locations Menu[/]").RuleStyle("cyan").LeftJustified());

            var locChoice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Location Actions:")
                    .PageSize(5)
                    .AddChoices(new[] { "List Locations", "Add New Location", "[yellow]< Back[/]" })
            );

            switch (locChoice)
            {
                case "List Locations":
                    ListLocations();
                    break;
                case "Add New Location":
                    AddLocation();
                    dataChanged = true;
                    break;
                case "[yellow]< Back[/]":
                     // Instead of returning to Main, return to Dictionary menu
                     // This requires restructuring the call stack or passing more state.
                     // Simplest is returning to Main for now.
                    return; // Return to the caller (Dictionary menu, which then returns to Main)
            }
            // Pause handled in main loop
        }

         static void ShowServiceMenu(ref bool dataChanged)
         {
             AnsiConsole.Clear();
             AnsiConsole.Write(new Rule("[magenta]Service Menu[/]").RuleStyle("magenta").LeftJustified());

             var serviceChoice = AnsiConsole.Prompt(
                 new SelectionPrompt<string>()
                     .Title("Service Actions:")
                     .PageSize(5)
                     .AddChoices(new[] { "CSV", "[yellow]< Back[/]" })
             );

              switch (serviceChoice)
              {
                  case "CSV":
                      ShowCsvMenu(ref dataChanged);
                      break;
                  case "[yellow]< Back[/]":
                      return; // Return to main menu
              }
              // Pause handled in main loop
         }

         static void ShowCsvMenu(ref bool dataChanged)
         {
              AnsiConsole.Clear();
              AnsiConsole.Write(new Rule("[yellow]CSV Import/Export[/]").RuleStyle("yellow").LeftJustified());

              var csvChoice = AnsiConsole.Prompt(
                  new SelectionPrompt<string>()
                      .Title("CSV Actions:")
                      .PageSize(5)
                      .AddChoices(new[] { "Export to CSV", "Import from CSV", "[yellow]< Back[/]" })
              );

              switch (csvChoice)
              {
                  case "Export to CSV":
                      ExportToCsv();
                      // Export doesn't change data flag
                      break;
                  case "Import from CSV":
                      ImportFromCsv();
                      dataChanged = true; // Import changes data
                      break;
                 case "[yellow]< Back[/]":
                      // Return to the caller (Service menu, which then returns to Main)
                     return;
              }
               // Pause handled in main loop
         }

         static void ShowReportsMenu()
         {
             AnsiConsole.Clear();
             AnsiConsole.Write(new Rule("[green]Reports Menu[/]").RuleStyle("green").LeftJustified());

             var reportChoice = AnsiConsole.Prompt(
                 new SelectionPrompt<string>()
                     .Title("Generate Labels PDF:")
                     .PageSize(5)
                     .AddChoices(new[] {
                         "All Items",
                         "By Location",
                         "By Inventory Number",
                         "[yellow]< Back[/]"
                     })
             );

             switch (reportChoice)
             {
                 case "All Items":
                     GenerateLabelsForAllItems();
                     break;
                 case "By Location":
                     GenerateLabelsByLocation();
                     break;
                 case "By Inventory Number":
                     GenerateLabelsByInventoryNumber();
                     break;
                 case "[yellow]< Back[/]":
                     return; // Return to main menu
             }
             // Pause handled in main loop
         }

        // Placeholder methods for PDF generation
        static void GenerateLabelsForAllItems()
        {
            AnsiConsole.MarkupLine("[cyan]Generating PDF labels for all items...[/]");
            pdfGenerator.GenerateAllItemsReport(inventory);
            // Pause handled by main loop
        }

        static void GenerateLabelsByLocation()
        {
            if (!locations.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No locations defined. Cannot generate report by location.[/]");
                return;
            }

            AnsiConsole.MarkupLine("[cyan]Select a location to generate labels for:[/]");
            string selectedLocation = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select [green]location[/]:")
                    .PageSize(10)
                    .MoreChoicesText("[grey](Move up and down to reveal more locations)[/]")
                    .AddChoices(locations.OrderBy(l => l))
                );

            AnsiConsole.MarkupLine($"[cyan]Generating PDF labels for location '{Markup.Escape(selectedLocation)}'...[/]");
            pdfGenerator.GenerateLocationReport(inventory, selectedLocation);
            // Pause handled by main loop
        }

        static void GenerateLabelsByInventoryNumber()
        {
            if (!inventory.Any())
            {
                AnsiConsole.MarkupLine("[yellow]Inventory is empty. Cannot generate report by inventory number.[/]");
                return;
            }

            AnsiConsole.MarkupLine("[cyan]Enter the Inventory Number to generate labels for:[/]");
            string invNum = AnsiConsole.Prompt(
                new TextPrompt<string>("Enter [green]Inventory Number[/]:")
                    .PromptStyle("cyan")
                    .Validate(input => !string.IsNullOrWhiteSpace(input), "[red]Inventory Number cannot be empty.[/]")
                );

            AnsiConsole.MarkupLine($"[cyan]Generating PDF labels for Inventory # '{Markup.Escape(invNum)}'...[/]");
            pdfGenerator.GenerateInventoryNumberReport(inventory, invNum);
            // Pause handled by main loop
        }

        // --- Item Methods (ListItems, UpdateItemQuantity, DeleteItem, Find*) ---
        // ListItems, DeleteItem, Find* - No changes needed
        // UpdateItemQuantity - No changes needed

        static void ListItems(List<InventoryItem>? itemsToList = null) // No changes needed
        {
            AnsiConsole.Write(new Rule("[green]Inventory List[/]").RuleStyle("green").Centered());
            var items = itemsToList ?? inventory;
            if (!items.Any()) { AnsiConsole.MarkupLine("[yellow]No items to display.[/]"); return; }
            var table = new Table().Border(TableBorder.Rounded);
            table.AddColumn("[b]Inv Num[/]").AddColumn("[b]ID[/]").AddColumn("[b]Name[/]").AddColumn("[b]Description[/]").AddColumn("[b]Qty[/]").AddColumn("[b]Location[/]").AddColumn("[b]Last Updated[/]");
            foreach (var item in items.OrderBy(i => i.InventoryNumber).ThenBy(i => i.Name))
            {
                table.AddRow(Markup.Escape(item.InventoryNumber), item.Id.ToString(), Markup.Escape(item.Name), Markup.Escape(item.Description), item.Quantity.ToString(), Markup.Escape(item.Location), item.LastUpdated.ToString("yyyy-MM-dd HH:mm"));
            }
            AnsiConsole.Write(table);
        }

        static void AddItem()
        {
            AnsiConsole.Write(new Rule("[green]Add New Item/Component[/]").RuleStyle("green").Centered());

            // Check if locations exist (should be guaranteed by menu logic, but belt-and-suspenders)
            if (!locations.Any())
            {
                AnsiConsole.MarkupLine("[red]Error: No locations available. Please add locations first.[/]");
                // No Pause here, handled by main loop
                return;
            }

            string inventoryNumber = AnsiConsole.Prompt(
                new TextPrompt<string>("Enter [green]Inventory Number[/]:")
                    .PromptStyle("cyan").Validate(invNum => !string.IsNullOrWhiteSpace(invNum), "[red]Inventory Number cannot be empty.[/]"));

            var existingItems = inventory.Where(i => i.InventoryNumber.Equals(inventoryNumber, StringComparison.OrdinalIgnoreCase)).ToList();
            if (existingItems.Any())
            {
                AnsiConsole.MarkupLine($"[yellow]Inv# '{Markup.Escape(inventoryNumber)}' components:[/]");
                ListItems(existingItems); AnsiConsole.WriteLine();
            }
            else
            {
                AnsiConsole.MarkupLine($"[yellow]First item for Inv# '{Markup.Escape(inventoryNumber)}'.[/]");
            }

            string name = AnsiConsole.Prompt(
                new TextPrompt<string>("Enter component [green]name[/]:")
                    .PromptStyle("cyan").Validate(n => !string.IsNullOrWhiteSpace(n), "[red]Name cannot be empty.[/]"));

            string description = AnsiConsole.Ask<string>("Enter component [green]description[/] (optional):", string.Empty);

            int quantity = AnsiConsole.Prompt(
                new TextPrompt<int>("Enter item [green]quantity[/]:")
                    .PromptStyle("cyan").DefaultValue(1).ValidationErrorMessage("[red]Invalid number >= 0.[/]").Validate(q => q >= 0));

            // --- *** Location Selection *** ---
            string selectedLocation = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select item [green]location[/]:")
                    .PageSize(10)
                    .MoreChoicesText("[grey](Move up and down to reveal more locations)[/]")
                    .AddChoices(locations.OrderBy(l => l)) // Use the loaded locations list, sorted
                );
            // --- *** End Location Selection *** ---

            var newItem = new InventoryItem
            {
                Id = dataStorage.GetNextId(inventory),
                InventoryNumber = inventoryNumber.Trim(),
                Name = name.Trim(),
                Description = description.Trim(),
                Quantity = quantity,
                Location = selectedLocation, // Assign the selected location
                LastUpdated = DateTime.UtcNow
            };

            inventory.Add(newItem);
            AnsiConsole.MarkupLine($"[bold green]Component '{Markup.Escape(newItem.Name)}' added for Inv# {Markup.Escape(newItem.InventoryNumber)} at '{Markup.Escape(selectedLocation)}' (ID: {newItem.Id}).[/]");
            // No Pause here, handled by main loop
        }

        static void UpdateItemQuantity() // No changes needed
        {
            AnsiConsole.Write(new Rule("[blue]Update Item Quantity[/]").RuleStyle("blue").Centered());
            if (!inventory.Any()) { AnsiConsole.MarkupLine("[yellow]Inventory empty.[/]"); return; }
            int idToUpdate = AnsiConsole.Prompt(new TextPrompt<int>("Enter [green]Record ID[/] to update:").PromptStyle("cyan").ValidationErrorMessage("[red]Invalid ID.[/]"));
            var itemToUpdate = inventory.FirstOrDefault(item => item.Id == idToUpdate);
            if (itemToUpdate == null) { AnsiConsole.MarkupLine($"[red]Error: ID {idToUpdate} not found.[/]"); return; }
            AnsiConsole.MarkupLine($"Updating: [yellow]Inv# {Markup.Escape(itemToUpdate.InventoryNumber)} / {Markup.Escape(itemToUpdate.Name)}[/] (Current qty: {itemToUpdate.Quantity})");
            int newQuantity = AnsiConsole.Prompt(new TextPrompt<int>($"Enter [green]new quantity[/] for Record ID {itemToUpdate.Id}:").PromptStyle("cyan").ValidationErrorMessage("[red]Invalid number >= 0.[/]").Validate(q => q >= 0));
            itemToUpdate.Quantity = newQuantity; itemToUpdate.LastUpdated = DateTime.UtcNow;
            AnsiConsole.MarkupLine($"[bold green]Quantity for item {itemToUpdate.Id} updated to {newQuantity}.[/]");
            // No Pause here, handled by main loop
        }

        // --- New Method: Update Item Location ---
        static void UpdateItemLocation()
        {
            AnsiConsole.Write(new Rule("[blue]Update Item Location[/]").RuleStyle("blue").Centered());

            if (!inventory.Any())
            {
                AnsiConsole.MarkupLine("[yellow]Inventory is empty. Nothing to update.[/]");
                return;
            }
            if (!locations.Any()) // Redundant check, but safe
            {
                AnsiConsole.MarkupLine("[yellow]No locations defined. Cannot update location.[/]");
                return;
            }

            // Use the unique Record ID for targeting the update
            int idToUpdate = AnsiConsole.Prompt(
                 new TextPrompt<int>("Enter the unique [green]Record ID[/] of the item record to update:")
                    .PromptStyle("cyan").ValidationErrorMessage("[red]Invalid ID.[/]"));

            var itemToUpdate = inventory.FirstOrDefault(item => item.Id == idToUpdate);

            if (itemToUpdate == null)
            {
                AnsiConsole.MarkupLine($"[red]Error: Item record with ID {idToUpdate} not found.[/]");
                return;
            }

            AnsiConsole.MarkupLine($"Updating location for: [yellow]Inv# {Markup.Escape(itemToUpdate.InventoryNumber)} / {Markup.Escape(itemToUpdate.Name)}[/]");
            AnsiConsole.MarkupLine($"Current location: [yellow]{Markup.Escape(itemToUpdate.Location)}[/]");

            // --- Location Selection ---
            string newLocation = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select the [green]new location[/]:")
                    .PageSize(10)
                    .MoreChoicesText("[grey](Scroll for more locations)[/]")
                    .AddChoices(locations.OrderBy(l => l)) // Use the loaded locations
                );
            // --- End Location Selection ---

            itemToUpdate.Location = newLocation;
            itemToUpdate.LastUpdated = DateTime.UtcNow;

            AnsiConsole.MarkupLine($"[bold green]Location for item record {itemToUpdate.Id} (Inv# {Markup.Escape(itemToUpdate.InventoryNumber)}) updated to '{Markup.Escape(newLocation)}'.[/]");
            // No Pause here, handled by main loop
        }


        static void DeleteItem() // No changes needed
        {
            AnsiConsole.Write(new Rule("[red]Delete Item Record[/]").RuleStyle("red").Centered());
            if (!inventory.Any()) { AnsiConsole.MarkupLine("[yellow]Inventory empty.[/]"); return; }
            int idToDelete = AnsiConsole.Prompt(new TextPrompt<int>("Enter [green]Record ID[/] to delete:").PromptStyle("cyan").ValidationErrorMessage("[red]Invalid ID.[/]"));
            var itemToDelete = inventory.FirstOrDefault(item => item.Id == idToDelete);
            if (itemToDelete == null) { AnsiConsole.MarkupLine($"[red]Error: ID {idToDelete} not found.[/]"); return; }
            AnsiConsole.MarkupLine($"Selected: [yellow]{Markup.Escape(itemToDelete.ToString())}[/]");
            if (AnsiConsole.Confirm($"[red]Delete[/] this specific item record?"))
            {
                inventory.Remove(itemToDelete); AnsiConsole.MarkupLine($"[bold green]Item record {idToDelete} deleted.[/]");
            }
            else { AnsiConsole.MarkupLine("Deletion cancelled."); }
            // No Pause here, handled by main loop
        }

        static void FindItemByName() // No changes needed
        {
            AnsiConsole.Write(new Rule("[cyan]Find Item by Name[/]").RuleStyle("cyan").Centered());
            if (!inventory.Any()) { AnsiConsole.MarkupLine("[yellow]Inventory empty.[/]"); return; }
            string searchTerm = AnsiConsole.Ask<string>("Enter component [green]name[/] (or part) to search:").Trim();
            if (string.IsNullOrWhiteSpace(searchTerm)) { AnsiConsole.MarkupLine("[yellow]Search term empty.[/]"); return; }
            var foundItems = inventory.Where(item => item.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)).ToList();
            if (!foundItems.Any()) { AnsiConsole.MarkupLine($"[yellow]No items found matching '{Markup.Escape(searchTerm)}'.[/]"); }
            else { AnsiConsole.MarkupLine($"[green]Found {foundItems.Count} matching '{Markup.Escape(searchTerm)}':[/]"); ListItems(foundItems); }
            // No Pause here, handled by main loop
        }

        static void FindItemByInventoryNumber() // No changes needed
        {
            AnsiConsole.Write(new Rule("[cyan]Find Item by Inventory Number[/]").RuleStyle("cyan").Centered());
            if (!inventory.Any()) { AnsiConsole.MarkupLine("[yellow]Inventory empty.[/]"); return; }
            string searchTerm = AnsiConsole.Ask<string>("Enter [green]Inventory Number[/] to search:").Trim();
            if (string.IsNullOrWhiteSpace(searchTerm)) { AnsiConsole.MarkupLine("[yellow]Search term empty.[/]"); return; }
            var foundItems = inventory.Where(item => item.InventoryNumber.Equals(searchTerm, StringComparison.OrdinalIgnoreCase)).ToList();
            if (!foundItems.Any()) { AnsiConsole.MarkupLine($"[yellow]No items found with Inv# '{Markup.Escape(searchTerm)}'.[/]"); }
            else { AnsiConsole.MarkupLine($"[green]Found {foundItems.Count} for Inv# '{Markup.Escape(searchTerm)}':[/]"); ListItems(foundItems); }
            // No Pause here, handled by main loop
        }


        // --- New Location Management Methods ---

        static void ListLocations()
        {
            AnsiConsole.Write(new Rule("[blue]Office Locations[/]").RuleStyle("blue").Centered());

            if (!locations.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No locations have been defined yet.[/]");
            }
            else
            {
                AnsiConsole.MarkupLine("Defined locations:");
                // Sort locations for display consistency
                foreach (var location in locations.OrderBy(l => l))
                {
                    AnsiConsole.MarkupLine($" - [cyan]{Markup.Escape(location)}[/]");
                }
            }
            // No Pause here, handled by main loop
        }

        static void AddLocation()
        {
            AnsiConsole.Write(new Rule("[blue]Add New Location[/]").RuleStyle("blue").Centered());

            string newLocation = AnsiConsole.Prompt(
               new TextPrompt<string>("Enter the name for the [green]new location[/]:")
                   .PromptStyle("cyan")
                   .Validate(loc => // First validation: not empty
                   {
                       return !string.IsNullOrWhiteSpace(loc)
                           ? ValidationResult.Success()
                           : ValidationResult.Error("[red]Location name cannot be empty.[/]");
                   })
                   .Validate(loc => // Second validation: check for duplicates
                   {
                       // 'loc' here is the input value from the prompt
                       bool exists = locations.Contains(loc.Trim(), StringComparer.OrdinalIgnoreCase);
                       if (!exists)
                       {
                           return ValidationResult.Success();
                       }
                       else
                       {
                           // Now 'loc' is accessible to be used in the error message
                           return ValidationResult.Error($"[red]Location '[yellow]{Markup.Escape(loc.Trim())}[/]' already exists.[/]");
                       }
                   }) // End of second Validate
               ); // End of Prompt

            // Trim before adding to ensure consistency
            string trimmedLocation = newLocation.Trim();
            locations.Add(trimmedLocation);
            // Optionally re-sort the list after adding
            // locations.Sort(StringComparer.OrdinalIgnoreCase);
            AnsiConsole.MarkupLine($"[bold green]Location '[yellow]{Markup.Escape(trimmedLocation)}[/]' added successfully.[/]");
            // No Pause here, handled by main loop
        }

        // --- Helper ---
        static void Pause() // No changes needed
        {
            AnsiConsole.MarkupLine("\n[grey]Press Enter to continue...[/]");
            Console.ReadLine();
        }

        static void ExportToCsv()
        {
            AnsiConsole.Write(new Rule("[blue]Export to CSV[/]").RuleStyle("blue").Centered());
            
            if (!inventory.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No items to export.[/]");
                return;
            }

            try
            {
                string fileName = AnsiConsole.Ask<string>("Enter the [green]CSV file name[/] to export to:", "inventory_export.csv");
                csvManager.ExportToCsv(fileName);
                AnsiConsole.MarkupLine($"[green]Successfully exported inventory to {Markup.Escape(fileName)}[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error exporting to CSV: {Markup.Escape(ex.Message)}[/]");
            }
        }

        static void ImportFromCsv()
        {
            AnsiConsole.Write(new Rule("[blue]Import from CSV[/]").RuleStyle("blue").Centered());

            try
            {
                string fileName = AnsiConsole.Ask<string>("Enter the [green]CSV file name[/] to import from:", "inventory_import.csv");
                
                if (!File.Exists(fileName))
                {
                    AnsiConsole.MarkupLine($"[red]File '{Markup.Escape(fileName)}' not found.[/]");
                    return;
                }

                if (AnsiConsole.Confirm("This will replace all existing inventory items. Continue?"))
                {
                    csvManager.ImportFromCsv(fileName);
                    inventory = dataStorage.LoadItems(); // Reload inventory after import
                    AnsiConsole.MarkupLine($"[green]Successfully imported inventory from {Markup.Escape(fileName)}[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine("[yellow]Import cancelled.[/]");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error importing from CSV: {Markup.Escape(ex.Message)}[/]");
            }
        }
    }
}