using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using projectTracker.Application.Dto.Privilage;
using projectTracker.Application.Dto.Project;
using projectTracker.Application.Dto.Role;
using projectTracker.Application.Features.Role.Command;
using projectTracker.Domain.Aggregates;
using projectTracker.Domain.Entities;

namespace projectTracker.Infrastructure.Mapping
{
    public class MappingProfile :Profile
    {
        public MappingProfile()
        {
            CreateMap<CreateRoleCommand, UserRole>();
            CreateMap<RoleUpdateDto, UpdateRoleCommand>();
            CreateMap<UpdateRoleCommand, UserRole>();


            CreateMap<Permission, PermissionDto>();
            CreateMap<CreatePermissionDto, Permission>();
            CreateMap<UpdatePermissionDto, Permission>();


            CreateMap<Project, ProjectResponseDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Key, opt => opt.MapFrom(src => src.Key))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.Lead, opt => opt.MapFrom(src => src.Lead))
                .ForMember(dest => dest.Critical, opt => opt.MapFrom(src => src.Critical))
                .ForMember(dest => dest.Health, opt => opt.MapFrom(src => new ProjectHealthDto
                {
                    Level = (int)src.Health.Level,
                    Reason = src.Health.Reason,
                    Score = src.Health.Score,
                    Confidence = src.Health.Confidence
                }))
                .ForMember(dest => dest.Progress, opt => opt.MapFrom(src => new ProgressMetricsDto
                {
                    TotalTasks = src.Progress.TotalTasks,
                    CompletedTasks = src.Progress.CompletedTasks,
                    StoryPointsCompleted = src.Progress.StoryPointsCompleted,
                    StoryPointsTotal = src.Progress.StoryPointsTotal,
                    ActiveBlockers = src.Progress.ActiveBlockers,
                    RecentUpdates = src.Progress.RecentUpdates
                }))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.OverallProjectStatus))
                .ForMember(dest => dest.TargetEndDate, opt => opt.MapFrom(src => src.TargetEndDate))
                .ForMember(dest => dest.Owner, opt => opt.MapFrom(src => new OwnerDto
                {
                    Name = src.Owner.Name,
                    ContactInfo = src.Owner.ContactInfo
                }));


        }
        
    }
}
