using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace projectTracker.Application.Dto.Report
{
        public class UserProjectSummaryDto
        {
            [Required]
            public string ProjectId { get; set; } = string.Empty; 

            [Required]
            public string ProjectKey { get; set; } = string.Empty; 

            [Required]
            public string ProjectName { get; set; } = string.Empty;

            public int TotalTasksAssigned { get; set; }

            public int CompletedTasks { get; set; }

            public decimal TotalStoryPointsAssigned { get; set; }

            public decimal CompletedStoryPoints { get; set; }

            public decimal TaskCompletionPercentage => TotalTasksAssigned > 0 ?
                (decimal)CompletedTasks / TotalTasksAssigned * 100 : 0;

            public decimal StoryPointCompletionPercentage => TotalStoryPointsAssigned > 0 ?
                CompletedStoryPoints / TotalStoryPointsAssigned * 100 : 0;
        }
    
}
