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
        private const int RowsPerPage = 5;
        private const float PageMargin = 0.5f; // inches

        public void GenerateLabels(List<InventoryItem> items, string filename, string title)
        {
            if (!items.Any())
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
                        page.DefaultTextStyle(x => x.FontSize(12)); // Default font size

                        page.Header()
                            .AlignCenter()
                            .Text(title)
                            .SemiBold().FontSize(14);

                        page.Content()
                            .PaddingVertical(10, Unit.Point)
                            .Grid(grid =>
                            {
                                // Define grid columns based on label width and page margins
                                grid.Columns(ColumnsPerPage);
                                // Calculate horizontal gap to distribute labels evenly within margins
                                float totalLabelWidth = ColumnsPerPage * LabelWidth;
                                float availableWidth = PageSizes.Letter.Width - (2 * PageMargin * 72); // Convert inches to points (1 inch = 72 points)
                                float horizontalGap = (availableWidth - (totalLabelWidth * 72)) / (ColumnsPerPage > 1 ? ColumnsPerPage - 1 : 1);
                                grid.HorizontalSpacing(horizontalGap > 0 ? horizontalGap : 0, Unit.Point);

                                // Calculate vertical gap
                                float totalLabelHeight = RowsPerPage * LabelHeight;
                                float availableHeight = PageSizes.Letter.Height - (2 * PageMargin * 72) - 50; // Approx height for header/footer/margins
                                float verticalGap = (availableHeight - (totalLabelHeight * 72)) / (RowsPerPage > 1 ? RowsPerPage - 1 : 1);
                                grid.VerticalSpacing(verticalGap > 0 ? verticalGap: 0, Unit.Point);


                                foreach (var item in items)
                                {
                                    grid.Item().Width(LabelWidth, Unit.Inch).Height(LabelHeight, Unit.Inch)
                                       .Border(1, Unit.Point).BorderColor(Colors.Grey.Medium)
                                       .Padding(5, Unit.Point) // Padding inside the label frame
                                       .Column(column =>
                                       {
                                           column.Spacing(2); // Spacing between lines inside the label

                                           column.Item().Text($"Inv#: {item.InventoryNumber}")
                                                 .SemiBold().FontSize(11); // Larger font for Inv#

                                           column.Item().Text(item.Name)
                                                 .FontSize(10); // Standard font for name

                                            // Add more details if needed, e.g., item.Location
                                            // column.Item().Text($"Loc: {item.Location}").Italic().FontSize(8);
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
                // Consider more detailed logging or error handling here
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