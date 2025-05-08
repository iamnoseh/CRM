using System.Net;
using Domain.DTOs.MentorGroup;
using Domain.Entities;
using Domain.Filters;
using Domain.Responses;
using Infrastructure.Data;
using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class MentorGroupService(DataContext context) : IMentorGroupService
{
    #region CreateMentorGroupAsync
    public async Task<Response<string>> CreateMentorGroupAsync(CreateMentorGroupDto request)
    {
        try
        {
            // Проверка существования ментора
            var mentor = await context.Mentors.FirstOrDefaultAsync(m => m.Id == request.MentorId && !m.IsDeleted);
            if (mentor == null)
                return new Response<string>(HttpStatusCode.NotFound, "Mentor not found");

            // Проверка существования группы
            var group = await context.Groups.FirstOrDefaultAsync(g => g.Id == request.GroupId && !g.IsDeleted);
            if (group == null)
                return new Response<string>(HttpStatusCode.NotFound, "Group not found");

            // Проверка, не назначен ли уже ментор в эту группу
            var existingMentorGroup = await context.MentorGroups
                .FirstOrDefaultAsync(mg => mg.MentorId == request.MentorId &&
                                          mg.GroupId == request.GroupId &&
                                          !mg.IsDeleted);

            if (existingMentorGroup != null)
                return new Response<string>(HttpStatusCode.BadRequest, "Mentor is already assigned to this group");

            // Создание новой записи MentorGroup
            var mentorGroup = new MentorGroup
            {
                MentorId = request.MentorId,
                GroupId = request.GroupId,
                IsActive = request.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await context.MentorGroups.AddAsync(mentorGroup);
            var result = await context.SaveChangesAsync();

            return result > 0
                ? new Response<string>(HttpStatusCode.Created, "Mentor assigned to group successfully")
                : new Response<string>(HttpStatusCode.InternalServerError, "Failed to assign mentor to group");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }
    #endregion

    #region UpdateMentorGroupAsync
    public async Task<Response<string>> UpdateMentorGroupAsync(int id, UpdateMentorGroupDto request)
    {
        try
        {
            // Находим запись MentorGroup по ID
            var mentorGroup = await context.MentorGroups
                .FirstOrDefaultAsync(mg => mg.Id == id && !mg.IsDeleted);

            if (mentorGroup == null)
                return new Response<string>(HttpStatusCode.NotFound, "Mentor group assignment not found");

            // Проверяем ментора, если ID ментора был изменен
            if (request.MentorId.HasValue && request.MentorId.Value != mentorGroup.MentorId)
            {
                var mentor = await context.Mentors
                    .FirstOrDefaultAsync(m => m.Id == request.MentorId.Value && !m.IsDeleted);

                if (mentor == null)
                    return new Response<string>(HttpStatusCode.NotFound, "Mentor not found");

                mentorGroup.MentorId = request.MentorId.Value;
            }

            // Проверяем группу, если ID группы был изменен
            if (request.GroupId.HasValue && request.GroupId.Value != mentorGroup.GroupId)
            {
                var group = await context.Groups
                    .FirstOrDefaultAsync(g => g.Id == request.GroupId.Value && !g.IsDeleted);

                if (group == null)
                    return new Response<string>(HttpStatusCode.NotFound, "Group not found");

                mentorGroup.GroupId = request.GroupId.Value;
            }

            // Проверяем, не назначен ли уже ментор в эту группу (если оба ID изменены)
            if (request.MentorId.HasValue && request.GroupId.HasValue)
            {
                var existingMentorGroup = await context.MentorGroups
                    .FirstOrDefaultAsync(mg => mg.MentorId == request.MentorId.Value &&
                                             mg.GroupId == request.GroupId.Value &&
                                             mg.Id != id &&
                                             !mg.IsDeleted);

                if (existingMentorGroup != null)
                    return new Response<string>(HttpStatusCode.BadRequest, "Mentor is already assigned to this group");
            }

            // Обновляем статус активности, если он был изменен
            if (request.IsActive.HasValue)
                mentorGroup.IsActive = request.IsActive.Value;

            mentorGroup.UpdatedAt = DateTime.UtcNow;

            context.MentorGroups.Update(mentorGroup);
            var result = await context.SaveChangesAsync();

            return result > 0
                ? new Response<string>(HttpStatusCode.OK, "Mentor group assignment updated successfully")
                : new Response<string>(HttpStatusCode.InternalServerError, "Failed to update mentor group assignment");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }
    #endregion

    #region DeleteMentorGroupAsync
    public async Task<Response<string>> DeleteMentorGroupAsync(int id)
    {
        try
        {
            // Находим запись MentorGroup по ID
            var mentorGroup = await context.MentorGroups
                .FirstOrDefaultAsync(mg => mg.Id == id && !mg.IsDeleted);

            if (mentorGroup == null)
                return new Response<string>(HttpStatusCode.NotFound, "Mentor group assignment not found");

            // Выполняем мягкое удаление
            mentorGroup.IsDeleted = true;
            mentorGroup.UpdatedAt = DateTime.UtcNow;

            context.MentorGroups.Update(mentorGroup);
            var result = await context.SaveChangesAsync();

            return result > 0
                ? new Response<string>(HttpStatusCode.OK, "Mentor removed from group successfully")
                : new Response<string>(HttpStatusCode.InternalServerError, "Failed to remove mentor from group");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }
    #endregion

    #region GetMentorGroupByIdAsync
    public async Task<Response<GetMentorGroupDto>> GetMentorGroupByIdAsync(int id)
    {
        try
        {
            // Находим запись MentorGroup по ID со связанными данными
            var mentorGroup = await context.MentorGroups
                .Include(mg => mg.Mentor)
                .Include(mg => mg.Group)
                .FirstOrDefaultAsync(mg => mg.Id == id && !mg.IsDeleted);

            if (mentorGroup == null)
                return new Response<GetMentorGroupDto>(HttpStatusCode.NotFound, "Mentor group assignment not found");

            // Преобразуем в DTO
            var dto = new GetMentorGroupDto
            {
                Id = mentorGroup.Id,
                GroupId = mentorGroup.GroupId,
                GroupName = mentorGroup.Group?.Name,
                MentorId = mentorGroup.MentorId,
                MentorName = mentorGroup.Mentor?.FullName,
                CreatedAt = mentorGroup.CreatedAt,
                UpdatedAt = mentorGroup.UpdatedAt,
                IsActive = mentorGroup.IsActive ?? true
            };

            return new Response<GetMentorGroupDto>(dto);
        }
        catch (Exception ex)
        {
            return new Response<GetMentorGroupDto>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }
    #endregion

    #region GetAllMentorGroupsAsync
    public async Task<Response<List<GetMentorGroupDto>>> GetAllMentorGroupsAsync()
    {
        try
        {
            var mentorGroups = await context.MentorGroups
                .Include(mg => mg.Mentor)
                .Include(mg => mg.Group)
                .Where(mg => !mg.IsDeleted)
                .Select(mg => new GetMentorGroupDto
                {
                    Id = mg.Id,
                    GroupId = mg.GroupId,
                    GroupName = mg.Group.Name,
                    MentorId = mg.MentorId,
                    MentorName = mg.Mentor.FullName,
                    CreatedAt = mg.CreatedAt,
                    UpdatedAt = mg.UpdatedAt,
                    IsActive = mg.IsActive ?? true
                })
                .ToListAsync();

            if (!mentorGroups.Any())
                return new Response<List<GetMentorGroupDto>>(HttpStatusCode.NotFound, "No mentor group assignments found");

            return new Response<List<GetMentorGroupDto>>(mentorGroups);
        }
        catch (Exception ex)
        {
            return new Response<List<GetMentorGroupDto>>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }
    #endregion

    #region GetMentorGroupsPaginated
    public async Task<PaginationResponse<List<GetMentorGroupDto>>> GetMentorGroupsPaginated(BaseFilter filter)
    {
        try
        {
            // Базовый запрос
            var query = context.MentorGroups
                .Include(mg => mg.Mentor)
                .Include(mg => mg.Group)
                .Where(mg => !mg.IsDeleted)
                .AsQueryable();

            // Получаем общее количество записей для пагинации
            var totalCount = await query.CountAsync();

            // Применяем пагинацию
            var mentorGroups = await query
                .OrderByDescending(mg => mg.CreatedAt)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(mg => new GetMentorGroupDto
                {
                    Id = mg.Id,
                    GroupId = mg.GroupId,
                    GroupName = mg.Group.Name,
                    MentorId = mg.MentorId,
                    MentorName = mg.Mentor.FullName,
                    CreatedAt = mg.CreatedAt,
                    UpdatedAt = mg.UpdatedAt,
                    IsActive = mg.IsActive ?? true
                })
                .ToListAsync();

            return new PaginationResponse<List<GetMentorGroupDto>>(
                mentorGroups,
                filter.PageNumber,
                filter.PageSize,
                totalCount
            );
        }
        catch (Exception ex)
        {
            return new PaginationResponse<List<GetMentorGroupDto>>(
                HttpStatusCode.InternalServerError,
                ex.Message
            );
        }
    }
    #endregion

    #region GetMentorGroupsByMentorAsync
    public async Task<Response<List<GetMentorGroupDto>>> GetMentorGroupsByMentorAsync(int mentorId)
    {
        try
        {
            // Проверяем существование ментора
            var mentor = await context.Mentors
                .FirstOrDefaultAsync(m => m.Id == mentorId && !m.IsDeleted);

            if (mentor == null)
                return new Response<List<GetMentorGroupDto>>(HttpStatusCode.NotFound, "Mentor not found");

            // Получаем группы ментора
            var mentorGroups = await context.MentorGroups
                .Include(mg => mg.Group)
                .Where(mg => mg.MentorId == mentorId && !mg.IsDeleted)
                .Select(mg => new GetMentorGroupDto
                {
                    Id = mg.Id,
                    GroupId = mg.GroupId,
                    GroupName = mg.Group.Name,
                    MentorId = mg.MentorId,
                    MentorName = mentor.FullName,
                    CreatedAt = mg.CreatedAt,
                    UpdatedAt = mg.UpdatedAt,
                    IsActive = mg.IsActive ?? true
                })
                .ToListAsync();

            if (!mentorGroups.Any())
                return new Response<List<GetMentorGroupDto>>(HttpStatusCode.NotFound, "Mentor is not assigned to any groups");

            return new Response<List<GetMentorGroupDto>>(mentorGroups);
        }
        catch (Exception ex)
        {
            return new Response<List<GetMentorGroupDto>>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }
    #endregion

    #region GetMentorGroupsByGroupAsync
    public async Task<Response<List<GetMentorGroupDto>>> GetMentorGroupsByGroupAsync(int groupId)
    {
        try
        {
            // Проверяем существование группы
            var group = await context.Groups
                .FirstOrDefaultAsync(g => g.Id == groupId && !g.IsDeleted);

            if (group == null)
                return new Response<List<GetMentorGroupDto>>(HttpStatusCode.NotFound, "Group not found");

            // Получаем менторов группы
            var mentorGroups = await context.MentorGroups
                .Include(mg => mg.Mentor)
                .Where(mg => mg.GroupId == groupId && !mg.IsDeleted)
                .Select(mg => new GetMentorGroupDto
                {
                    Id = mg.Id,
                    GroupId = mg.GroupId,
                    GroupName = group.Name,
                    MentorId = mg.MentorId,
                    MentorName = mg.Mentor.FullName,
                    CreatedAt = mg.CreatedAt,
                    UpdatedAt = mg.UpdatedAt,
                    IsActive = mg.IsActive ?? true
                })
                .ToListAsync();

            if (!mentorGroups.Any())
                return new Response<List<GetMentorGroupDto>>(HttpStatusCode.NotFound, "No mentors assigned to this group");

            return new Response<List<GetMentorGroupDto>>(mentorGroups);
        }
        catch (Exception ex)
        {
            return new Response<List<GetMentorGroupDto>>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }
    #endregion

    #region AddMultipleMentorsToGroupAsync
    public async Task<Response<string>> AddMultipleMentorsToGroupAsync(int groupId, List<int> mentorIds)
    {
        try
        {
            if (mentorIds == null || !mentorIds.Any())
                return new Response<string>(HttpStatusCode.BadRequest, "No mentors specified");

            // Проверяем существование группы
            var group = await context.Groups
                .FirstOrDefaultAsync(g => g.Id == groupId && !g.IsDeleted);

            if (group == null)
                return new Response<string>(HttpStatusCode.NotFound, "Group not found");

            // Проверяем существование всех менторов
            var existingMentors = await context.Mentors
                .Where(m => mentorIds.Contains(m.Id) && !m.IsDeleted)
                .Select(m => m.Id)
                .ToListAsync();

            var missingMentorIds = mentorIds.Except(existingMentors).ToList();
            if (missingMentorIds.Any())
                return new Response<string>(HttpStatusCode.NotFound, $"Mentors with IDs {string.Join(", ", missingMentorIds)} not found");

            // Получаем существующие связи менторов с этой группой
            var existingMentorGroups = await context.MentorGroups
                .Where(mg => mg.GroupId == groupId && mentorIds.Contains(mg.MentorId) && !mg.IsDeleted)
                .ToListAsync();

            // Определяем, каких менторов нужно добавить
            var mentorsToAdd = mentorIds
                .Except(existingMentorGroups.Select(mg => mg.MentorId))
                .ToList();

            // Создаем новые записи для менторов, которых еще нет в группе
            var newMentorGroups = mentorsToAdd.Select(mentorId => new MentorGroup
            {
                MentorId = mentorId,
                GroupId = groupId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            await context.MentorGroups.AddRangeAsync(newMentorGroups);
            var result = await context.SaveChangesAsync();

            return result > 0
                ? new Response<string>(HttpStatusCode.Created, "Mentors added to group successfully")
                : new Response<string>(HttpStatusCode.InternalServerError, "Failed to add mentors to group");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }
    #endregion

    #region RemoveMentorFromAllGroupsAsync
    public async Task<Response<string>> RemoveMentorFromAllGroupsAsync(int mentorId)
    {
        try
        {
            // Проверяем существование ментора
            var mentor = await context.Mentors
                .FirstOrDefaultAsync(m => m.Id == mentorId && !m.IsDeleted);

            if (mentor == null)
                return new Response<string>(HttpStatusCode.NotFound, "Mentor not found");

            // Получаем все группы ментора
            var mentorGroups = await context.MentorGroups
                .Where(mg => mg.MentorId == mentorId && !mg.IsDeleted)
                .ToListAsync();

            if (!mentorGroups.Any())
                return new Response<string>(HttpStatusCode.NotFound, "Mentor is not assigned to any groups");

            // Отмечаем все связи как удаленные
            foreach (var mg in mentorGroups)
            {
                mg.IsDeleted = true;
                mg.UpdatedAt = DateTime.UtcNow;
            }

            var result = await context.SaveChangesAsync();

            return result > 0
                ? new Response<string>(HttpStatusCode.OK, $"Mentor removed from {mentorGroups.Count} groups successfully")
                : new Response<string>(HttpStatusCode.InternalServerError, "Failed to remove mentor from groups");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }
    #endregion
}
