using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using projectTracker.Application.Dto;
using projectTracker.Application.Interfaces;

namespace projectTracker.Application.Dto
{
    public class JiraSearchResult
    {
        [JsonPropertyName("startAt")]
        public int StartAt { get; set; }

        [JsonPropertyName("maxResults")]
        public int MaxResults { get; set; }

        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("issues")]
        public List<JiraIssue> Issues { get; set; } = new List<JiraIssue>();
    }

    public class JiraIssue
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("key")]
        public string Key { get; set; } = string.Empty;

        [JsonPropertyName("fields")]
        public JiraIssueFieldsDto Fields { get; set; } = new JiraIssueFieldsDto();
    }

    public class JiraIssueFieldsDto
    {
        [JsonPropertyName("summary")]
        public string Summary { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public JsonElement? Description { get; set; } // Use JsonElement for rich text, parse in domain

        [JsonPropertyName("status")]
        public JiraStatusDto? Status { get; set; }

        [JsonPropertyName("assignee")]
        public JiraUserResponse? Assignee { get; set; }

        [JsonPropertyName("created")]
        public DateTime Created { get; set; }

        [JsonPropertyName("updated")]
        public DateTime Updated { get; set; }

        [JsonPropertyName("duedate")]
        public DateTime? DueDate { get; set; }

        // Custom field for Story Points (Jira ID 10035, typically)
        [JsonPropertyName("customfield_10038")]
        public decimal? StoryPoints { get; set; }

        [JsonPropertyName("issuetype")]
        public JiraIssueTypeDto? IssueType { get; set; }

        [JsonPropertyName("parent")]
        public JiraIssueParentDto? Parent { get; set; } // For sub-tasks

        // For tasks linked to epics (Jira's specific linking mechanism for epics)
        // This is often a custom field or a specific endpoint depending on Jira version.
        // For /search, Jira often provides a special 'epic' field if the task is linked.
        [JsonPropertyName("customfield_10014")]
        public string? Epic { get; set; }

        [JsonPropertyName("timetracking")]
        public JiraTimeTrackingDto? TimeTracking { get; set; }

        [JsonPropertyName("labels")]
        public List<string>? Labels { get; set; }

        [JsonPropertyName("priority")]
        public JiraPriorityDto? Priority { get; set; }

        // This is how Jira typically returns current sprint information for an issue.
        // It's often an array of objects even if only one is active.
        [JsonPropertyName("customfield_10020")]
        public List<JiraSprintReferenceDto>? Sprints { get; set; }
    }

    public class JiraStatusDto
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("statusCategory")]
        public JiraStatusCategoryDto? StatusCategory { get; set; }
    }


    public class JiraStatusCategoryDto
    {
        [JsonPropertyName("key")] // e.g., "new", "indeterminate", "done"
        public string Key { get; set; } = string.Empty;
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    public class JiraAssigneeDto
    {
        public string AccountId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
    }


    public class JiraIssueTypeDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    public class JiraIssueParentDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
        [JsonPropertyName("key")]
        public string Key { get; set; } = string.Empty;
        [JsonPropertyName("fields")]
        public JiraIssueParentFieldsDto? Fields { get; set; }
    }

    public class JiraIssueParentFieldsDto
    {
        [JsonPropertyName("summary")]
        public string Summary { get; set; } = string.Empty;
        [JsonPropertyName("issuetype")]
        public JiraIssueTypeDto? IssueType { get; set; }
    }

    // Represents an Epic issue (often has a name field itself, or link to summary)
    public class JiraEpicDto
    {
        [JsonPropertyName("id")]

        public int Id { get; set; } = 0;
        [JsonPropertyName("key")]
        public string Key { get; set; } = string.Empty;
        // Depending on Jira config, you might get the epic name here or need to fetch it separately
        [JsonPropertyName("name")]
         public string? Name { get; set; }
        [JsonPropertyName("Summary")]
        public string? Summary { get; set; }
    }

    public class JiraTimeTrackingDto
    {
        [JsonPropertyName("originalEstimateSeconds")]
        public int? OriginalEstimateSeconds { get; set; }

        [JsonPropertyName("remainingEstimateSeconds")]
        public int? RemainingEstimateSeconds { get; set; }

        [JsonPropertyName("timeSpentSeconds")]
        public int? TimeSpentSeconds { get; set; }
    }

    public class JiraPriorityDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    // --- Jira Agile API DTOs ---

    // For /rest/agile/1.0/board
    public class JiraBoardsResponse
    {
        [JsonPropertyName("maxResults")]
        public int MaxResults { get; set; }
        [JsonPropertyName("startAt")]
        public int StartAt { get; set; }
        [JsonPropertyName("total")]
        public int Total { get; set; }
        [JsonPropertyName("values")]
        public List<JiraBoardDto> Values { get; set; } = new List<JiraBoardDto>();
    }

    public class JiraBoardDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("self")]
        public string Self { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("type")] // e.g., "scrum", "kanban"
        public string Type { get; set; } = string.Empty;

        // REMOVE THIS LINE: [JsonPropertyName("projectKey")] // <--- This was the problem
        // REMOVE THIS LINE: public string ProjectKey { get; set; } = string.Empty;

        [JsonPropertyName("location")] // ADD THIS LINE (or uncomment if you had it)
        public JiraBoardLocationDto? Location { get; set; } // Make it nullable as location might not always be present
    }

    public class JiraBoardLocationDto
    {
        [JsonPropertyName("projectId")]
        public long ProjectId { get; set; } // Jira Project ID, often a long

        [JsonPropertyName("projectKey")]
        public string ProjectKey { get; set; } = string.Empty; // This is the crucial field

        [JsonPropertyName("projectName")]
        public string ProjectName { get; set; } = string.Empty;

        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; } = string.Empty;

        [JsonPropertyName("projectType")]
        public string ProjectType { get; set; } = string.Empty;
    }

    // For /rest/agile/1.0/board/{boardId}/sprint
    public class JiraSprintsResponse
    {
        [JsonPropertyName("maxResults")]
        public int MaxResults { get; set; }
        [JsonPropertyName("startAt")]
        public int StartAt { get; set; }
        [JsonPropertyName("total")]
        public int Total { get; set; }
        [JsonPropertyName("values")]
        public List<JiraSprintDto> Values { get; set; } = new List<JiraSprintDto>();
    }

    public class JiraSprintDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("self")]
        public string Self { get; set; } = string.Empty;

        [JsonPropertyName("state")] // e.g., 'active', 'future', 'closed'
        public string State { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("startDate")]
        public DateTime? StartDate { get; set; }

        [JsonPropertyName("endDate")]
        public DateTime? EndDate { get; set; }

        [JsonPropertyName("completeDate")]
        public DateTime? CompleteDate { get; set; }

        [JsonPropertyName("originBoardId")]
        public int? OriginBoardId { get; set; }

        [JsonPropertyName("goal")]
        public string? Goal { get; set; }
    }

    // Represents a sprint object within an issue's fields (often an array)
    public class JiraSprintReferenceDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("state")]
        public string State { get; set; } = string.Empty;

        // Full properties often returned
        [JsonPropertyName("startDate")]
        public DateTime? StartDate { get; set; }

        [JsonPropertyName("endDate")]
        public DateTime? EndDate { get; set; }

        [JsonPropertyName("completeDate")]
        public DateTime? CompleteDate { get; set; }

        [JsonPropertyName("boardId")]
        public int? BoardId { get; set; }
    }

    // --- Jira Changelog DTOs ---

    // For /issue/{issueIdOrKey}?expand=changelog
    public class JiraIssueWithChangelog : JiraIssue
    {
        [JsonPropertyName("changelog")]
        public JiraChangelog? Changelog { get; set; }
    }

    public class JiraChangelog
    {
        [JsonPropertyName("histories")]
        public List<JiraHistory> Histories { get; set; } = new List<JiraHistory>();
    }

    public class JiraHistory
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("author")]
        public JiraUserResponse? Author { get; set; } // Who made the change

        [JsonPropertyName("created")]
        public DateTime Created { get; set; }

        [JsonPropertyName("items")]
        public List<JiraHistoryItem> Items { get; set; } = new List<JiraHistoryItem>();
    }

    public class JiraHistoryItem
    {
        [JsonPropertyName("field")]
        public string Field { get; set; } = string.Empty; // e.g., "status", "assignee", "Sprint"

        [JsonPropertyName("fieldtype")]
        public string FieldType { get; set; } = string.Empty; // e.g., "jira", "custom"

        [JsonPropertyName("from")] // Raw ID of old value
        public string? From { get; set; }

        [JsonPropertyName("fromString")] // Display name of old value
        public string? FromString { get; set; }

        [JsonPropertyName("to")] // Raw ID of new value
        public string? To { get; set; }

        [JsonPropertyName("toString")] // Display name of new value
        public string? ToString { get; set; }
    }

}
