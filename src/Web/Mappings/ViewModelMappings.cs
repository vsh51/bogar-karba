using Application.DTOs;
using Application.DTOs.Checklist;
using Application.DTOs.User;
using Application.UseCases.GetPublishedChecklist;
using Web.Models.Admin;
using Web.Models.Author;
using Web.Models.Checklist;

namespace Web.Mappings;

public static class ViewModelMappings
{
    public static AdminChecklistViewModel ToAdminViewModel(this ChecklistSummaryDto dto)
    {
        return new AdminChecklistViewModel
        {
            Id = dto.Id,
            Title = dto.Title,
            Description = dto.Description,
            UserId = dto.UserId,
            UserName = dto.UserName,
            Status = dto.Status
        };
    }

    public static AdminUserViewModel ToAdminUserViewModel(this UserSummaryDto dto)
    {
        return new AdminUserViewModel
        {
            Id = dto.Id,
            UserName = dto.UserName,
            Email = dto.Email,
            FullName = $"{dto.Name} {dto.Surname}",
            Status = dto.Status
        };
    }

    public static AuthorChecklistViewModel ToAuthorViewModel(this ChecklistSummaryDto dto)
    {
        return new AuthorChecklistViewModel
        {
            Id = dto.Id,
            Title = dto.Title,
            Description = dto.Description,
            Status = dto.Status,
            IsPublic = dto.IsPublic,
            Deadline = dto.Deadline,
            IsOutdated = dto.IsOutdated,
            DeadlineRemaining = dto.DeadlineRemaining
        };
    }

    public static ChecklistViewModel ToChecklistViewModel(this GetPublishedChecklistResult result)
    {
        return new ChecklistViewModel
        {
            Id = result.Id,
            Title = result.Title,
            Description = result.Description,
            Deadline = result.Deadline,
            IsPublic = result.IsPublic,
            IsOwner = result.IsOwner,
            IsOutdated = result.IsOutdated,
            DeadlineRemaining = result.DeadlineRemaining,
            Sections = result.Sections
                .OrderBy(s => s.Position)
                .Select(section => new ChecklistSectionViewModel
                {
                    Id = section.Id,
                    Name = section.Name,
                    Position = section.Position,
                    Items = section.Items
                        .OrderBy(i => i.Position)
                        .Select(item => new ChecklistItemViewModel
                        {
                            Id = item.Id,
                            Content = item.Content,
                            Link = item.Link
                        })
                        .ToList()
                })
                .ToList()
        };
    }

    public static DashboardViewModel ToDashboardViewModel(this SystemStatsDto result)
    {
        return new DashboardViewModel
        {
            TotalChecklists = result.TotalChecklists,
            TotalUsers = result.TotalUsers,
            PublishedChecklists = result.PublishedChecklists,
            DraftChecklists = result.DraftChecklists,
            ArchivedChecklists = result.ArchivedChecklists
        };
    }
}
