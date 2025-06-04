using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace projectTracker.Domain.Entities
{
   
    public class MenuItem
    {
        public int Id { get; set; }
        public string? Name { get; set; }          // Display text (e.g., "Dashboard")
        public string? Url { get; set; }           // Route (e.g., "/dashboard")
        public string? Icon { get; set; }          // Icon class (e.g., "fa-home")
        public string? RequiredPrivilege { get; set; } // e.g., "Dashboard-View"
        public int? ParentId { get; set; }         // Null for root items
        public int Order { get; set; }             // Sorting order
        public bool IsActive { get; set; } = true; // Soft delete

        [ForeignKey("ParentId")]
        public virtual MenuItem? Parent { get; set; }
        public virtual ICollection<MenuItem> Children { get; set; } = new List<MenuItem>();
    }
}
