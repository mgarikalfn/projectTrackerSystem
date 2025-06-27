using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using projectTracker.Domain.Enums;

namespace projectTracker.Application.Dto.Project.MileStone
{
    public class UpdateMilestoneDto
    {
        public string Name { get; set; } = default!;
        public DateTime DueDate { get; set; }
        public MilestoneStatus Status { get; set; }
        public string? Description { get; set; }
    }
}
