using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using projectTracker.Domain.Enums;

namespace projectTracker.Application.Dto.Project.MileStone
{
    public class MilestoneDto {
           public string id { get; set; }
            public string Name { get; set; }
           public  string Description { get; set; }
           public DateTime DueDate { get; set; }
           public   MilestoneStatus status { get; set; }
           public string ProjectId { get; set; }

    }
    
}
