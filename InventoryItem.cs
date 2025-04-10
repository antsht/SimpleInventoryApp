using System;

namespace SimpleInventoryApp
{
    public class InventoryItem
    {
        // Unique ID for this specific record/component in the database
        public int Id { get; set; }

        // Shared identifier for the overall asset (e.g., "ASSET-001")
        public string InventoryNumber { get; set; } = string.Empty;

        // Name of this specific component (e.g., "Laptop", "Monitor", "Keyboard")
        public string Name { get; set; } = string.Empty;

        // Optional description for this component
        public string Description { get; set; } = string.Empty;

        // How many of this specific component are at this location (often 1 for asset parts)
        public int Quantity { get; set; }

        // Where this specific component is stored
        public string Location { get; set; } = string.Empty;

        // When this specific record was last modified
        public DateTime LastUpdated { get; set; }

        // Updated ToString for clarity
        public override string ToString()
        {
            // Now includes InventoryNumber
            return $"[Inv# {InventoryNumber ?? "N/A"}] [{Id}] {Name} ({Quantity} pcs) - Loc: {Location}";
        }
    }
}