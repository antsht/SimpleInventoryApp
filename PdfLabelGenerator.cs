using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Spectre.Console; // For console output

namespace SimpleInventoryApp
{
 public class PdfLabelGenerator
    {
        // Basic label dimensions and grid layout (adjust as needed)
        private const float LabelWidth = 2.0f; // inches
        private const float LabelHeight = 1.0f; // inches
        private const int ColumnsPerPage = 3;
        private const int RowsPerPage = 5; // Used primarily for calculating vertical spacing if needed, less direct role in Row/Column layout
        private const float PageMargin = 0.5f; // inches

        // --- Constants for Conversion ---
        private const float InchesToPoints = 72f;

        public void GenerateLabels(List<InventoryItem> items, string filename, string title)
        {
            if (items == null || !items.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No items provided to generate labels.[/]");
                return;
            }

            try
            {
                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Margin(PageMargin, Unit.Inch);
                        page.Size(PageSizes.Letter); // Standard letter size paper
                        page.DefaultTextStyle(x => x.FontSize(10)); // Default font size for labels

                        page.Header()
                            .AlignCenter()
                            .PaddingBottom(5, Unit.Point) // Add some space below header
                            .Text(title)
                            .SemiBold().FontSize(14);

                        page.Content()
                            .PaddingVertical(10, Unit.Point)
                            .Column(mainColumn => // Use a main column to stack the rows of labels
                            {
                                // --- Calculate Spacing ---
                                // Available width/height within margins (in points)
                                float availableWidth = (PageSizes.Letter.Width / InchesToPoints - 2 * PageMargin) * InchesToPoints;
                                // Estimate available height considering header/footer/margins
                                float pageContentHeight = (PageSizes.Letter.Height / InchesToPoints - 2 * PageMargin) * InchesToPoints;
                                float approxHeaderFooterHeight = 60; // Estimate header/footer/padding height in points
                                float availableHeight = pageContentHeight - approxHeaderFooterHeight;

                                // Calculate total width/height occupied by labels themselves (in points)
                                float totalLabelsWidth = ColumnsPerPage * LabelWidth * InchesToPoints;
                                // float totalLabelsHeight = RowsPerPage * LabelHeight * InchesToPoints; // Used mainly for vertical gap calculation

                                // Calculate gaps (ensure non-negative)
                                float horizontalGap = (ColumnsPerPage > 1)
                                    ? Math.Max(0, (availableWidth - totalLabelsWidth) / (ColumnsPerPage - 1))
                                    : 0;
                                float verticalGap = (RowsPerPage > 1)
                                     // Use calculated available height or just a fixed sensible gap
                                     // Method 1: Using calculated available height (can be imprecise)
                                     //? Math.Max(0, (availableHeight - totalLabelsHeight) / (RowsPerPage - 1))
                                     // Method 2: Using a fixed sensible gap (often more reliable)
                                     ? 10 // Points, adjust as needed for your label paper stock
                                     : 0;

                                // Apply vertical spacing between the rows of labels
                                mainColumn.Spacing(verticalGap, Unit.Point);

                                // --- Create Rows and Labels ---
                                int itemCount = items.Count;
                                for (int i = 0; i < itemCount; i += ColumnsPerPage)
                                {
                                    // Get the items for the current row
                                    var itemsInRow = items.Skip(i).Take(ColumnsPerPage).ToList();

                                    // Add a Row element to the main Column
                                    mainColumn.Item().Row(row =>
                                    {
                                        // Apply horizontal spacing between labels in this row
                                        row.Spacing(horizontalGap, Unit.Point);

                                        // Add each label as a Column item within the Row
                                        foreach (var item in itemsInRow)
                                        {
                                            row.RelativeItem() // Use RelativeItem for flexible distribution if needed, or Item() for fixed size
                                               .Width(LabelWidth, Unit.Inch)
                                               .Height(LabelHeight, Unit.Inch)
                                               .Border(1, Unit.Point).BorderColor(Colors.Grey.Medium)
                                               .Padding(5, Unit.Point) // Padding inside the label frame
                                               .Column(labelContentCol => // Content within the label arranged vertically
                                               {
                                                   labelContentCol.Spacing(2); // Spacing between lines inside the label

                                                   labelContentCol.Item().Text($"Inv#: {item.InventoryNumber}")
                                                                .SemiBold().FontSize(11); // Slightly larger font for Inv#

                                                   labelContentCol.Item().Text(item.Name)
                                                                .FontSize(10); // Standard font for name

                                                   // Example: Add location if it exists
                                                   if (!string.IsNullOrWhiteSpace(item.Location))
                                                   {
                                                      labelContentCol.Item().Text($"Loc: {item.Location}").Italic().FontSize(8);
                                                   }
                                               });
                                        }

                                        // Add placeholder items if the row is not full to maintain spacing
                                        int placeholdersNeeded = ColumnsPerPage - itemsInRow.Count;
                                        for (int j = 0; j < placeholdersNeeded; j++)
                                        {
                                            // Add an empty item to take up space and keep alignment
                                            row.RelativeItem().Width(LabelWidth, Unit.Inch).Height(LabelHeight, Unit.Inch);
                                            // Or use row.ConstantItem if RelativeItem causes issues
                                            // row.ConstantItem(LabelWidth * InchesToPoints).Height(LabelHeight, Unit.Inch);
                                        }
                                    });
                                }
                            });

                        page.Footer()
                            .AlignCenter()
                            .Text(x =>
                            {
                                x.Span("Page ");
                                x.CurrentPageNumber();
                                x.Span(" of ");
                                x.TotalPages();
                            });
                    });
                })
                .GeneratePdf(filename); // Generate the PDF file

                AnsiConsole.MarkupLine($"[green]Successfully generated labels PDF: '{Markup.Escape(filename)}'[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error generating PDF: {Markup.Escape(ex.Message)}[/]");
                AnsiConsole.WriteException(ex, ExceptionFormats.ShortenPaths); // More detailed exception output
            }
        }

        // --- Report Generation Methods ---

        public void GenerateAllItemsReport(List<InventoryItem> inventory)
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string filename = $"InventoryLabels_All_{timestamp}.pdf";
            string title = "Inventory Labels - All Items";
            GenerateLabels(inventory.OrderBy(i => i.InventoryNumber).ThenBy(i => i.Name).ToList(), filename, title);
        }

        public void GenerateLocationReport(List<InventoryItem> inventory, string location)
        {
            var itemsForLocation = inventory.Where(i => i.Location.Equals(location, StringComparison.OrdinalIgnoreCase)).ToList();
            if (!itemsForLocation.Any())
            {
                 AnsiConsole.MarkupLine($"[yellow]No items found for location: '{Markup.Escape(location)}'[/]");
                 return;
            }

            string safeLocation = string.Join("_", location.Split(Path.GetInvalidFileNameChars())); // Sanitize filename
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string filename = $"InventoryLabels_Location_{safeLocation}_{timestamp}.pdf";
            string title = $"Inventory Labels - Location: {location}";
            GenerateLabels(itemsForLocation.OrderBy(i => i.InventoryNumber).ThenBy(i => i.Name).ToList(), filename, title);
        }

        public void GenerateInventoryNumberReport(List<InventoryItem> inventory, string invNum)
        {
             var itemsForInvNum = inventory.Where(i => i.InventoryNumber.Equals(invNum, StringComparison.OrdinalIgnoreCase)).ToList();
             if (!itemsForInvNum.Any())
             {
                  AnsiConsole.MarkupLine($"[yellow]No items found for Inventory Number: '{Markup.Escape(invNum)}'[/]");
                  return;
             }

            string safeInvNum = string.Join("_", invNum.Split(Path.GetInvalidFileNameChars())); // Sanitize filename
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string filename = $"InventoryLabels_InvNum_{safeInvNum}_{timestamp}.pdf";
            string title = $"Inventory Labels - Inventory #: {invNum}";
            GenerateLabels(itemsForInvNum.OrderBy(i => i.Name).ToList(), filename, title); // Order by name within the same Inv#
        }
    }
} 