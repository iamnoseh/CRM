using System.Net;
using Domain.DTOs.StudentGroup;
using Domain.Entities;
using Domain.Filters;
using Domain.Responses;
using Infrastructure.Data;
using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class StudentGroupService(DataContext context) : IStudentGroupService
{
    #region CreateStudentGroupAsync
    public async Task<Response<string>> CreateStudentGroupAsync(CreateStudentGroup request)
    {
        try
        {
            var student = await context.Students.FirstOrDefaultAsync(s => s.Id == request.StudentId && !s.IsDeleted);
            if (student == null)
                return new Response<string>(HttpStatusCode.NotFound, "Student not found");

            var group = await context.Groups.FirstOrDefaultAsync(g => g.Id == request.GroupId && !g.IsDeleted);
            if (group == null)
                return new Response<string>(HttpStatusCode.NotFound, "Group not found");

            var existingStudentGroup = await context.StudentGroups
                .FirstOrDefaultAsync(sg => sg.StudentId == request.StudentId && 
                                          sg.GroupId == request.GroupId && 
                                          !sg.IsDeleted);
            
            if (existingStudentGroup != null)
            {
                if (existingStudentGroup.IsActive == true)
                    return new Response<string>(HttpStatusCode.BadRequest, "Student is already assigned to this group");
                
                existingStudentGroup.IsActive = true;
                existingStudentGroup.UpdatedAt = DateTime.UtcNow;
                context.StudentGroups.Update(existingStudentGroup);
                
                var updateResult = await context.SaveChangesAsync();
                return updateResult > 0
                    ? new Response<string>(HttpStatusCode.OK, "Student's group membership reactivated")
                    : new Response<string>(HttpStatusCode.InternalServerError, "Failed to reactivate student's group membership");
            }

            var studentGroup = new StudentGroup
            {
                StudentId = request.StudentId,
                GroupId = request.GroupId,
                IsActive = request.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await context.StudentGroups.AddAsync(studentGroup);
            var result = await context.SaveChangesAsync();

            return result > 0
                ? new Response<string>(HttpStatusCode.Created, "Student added to group successfully")
                : new Response<string>(HttpStatusCode.InternalServerError, "Failed to add student to group");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }
    #endregion

    #region UpdateStudentGroupAsync
    public async Task<Response<string>> UpdateStudentGroupAsync(int id, UpdateStudentGroupDto request)
    {
        try
        {
            // Находим запись StudentGroup по ID
            var studentGroup = await context.StudentGroups
                .FirstOrDefaultAsync(sg => sg.Id == id && !sg.IsDeleted);
            
            if (studentGroup == null)
                return new Response<string>(HttpStatusCode.NotFound, "Student group membership not found");

            // Проверяем студента, если ID студента был изменен
            if (request.StudentId.HasValue && request.StudentId.Value != studentGroup.StudentId)
            {
                var student = await context.Students
                    .FirstOrDefaultAsync(s => s.Id == request.StudentId.Value && !s.IsDeleted);
                
                if (student == null)
                    return new Response<string>(HttpStatusCode.NotFound, "Student not found");
                
                studentGroup.StudentId = request.StudentId.Value;
            }

            // Проверяем группу, если ID группы был изменен
            if (request.GroupId.HasValue && request.GroupId.Value != studentGroup.GroupId)
            {
                var group = await context.Groups
                    .FirstOrDefaultAsync(g => g.Id == request.GroupId.Value && !g.IsDeleted);
                
                if (group == null)
                    return new Response<string>(HttpStatusCode.NotFound, "Group not found");
                
                studentGroup.GroupId = request.GroupId.Value;
            }

            // Проверяем, не состоит ли студент уже в этой группе (если оба ID изменены)
            if (request.StudentId.HasValue && request.GroupId.HasValue)
            {
                var existingStudentGroup = await context.StudentGroups
                    .FirstOrDefaultAsync(sg => sg.StudentId == request.StudentId.Value && 
                                             sg.GroupId == request.GroupId.Value && 
                                             sg.Id != id &&
                                             !sg.IsDeleted);
                
                if (existingStudentGroup != null)
                    return new Response<string>(HttpStatusCode.BadRequest, "Student is already assigned to this group");
            }

            // Обновляем статус активности, если он был изменен
            if (request.IsActive.HasValue)
                studentGroup.IsActive = request.IsActive.Value;

            studentGroup.UpdatedAt = DateTime.UtcNow;
            
            context.StudentGroups.Update(studentGroup);
            var result = await context.SaveChangesAsync();

            return result > 0
                ? new Response<string>(HttpStatusCode.OK, "Student group membership updated successfully")
                : new Response<string>(HttpStatusCode.InternalServerError, "Failed to update student group membership");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }
    #endregion

    #region DeleteStudentGroupAsync
    public async Task<Response<string>> DeleteStudentGroupAsync(int id)
    {
        try
        {
            // Находим запись StudentGroup по ID
            var studentGroup = await context.StudentGroups
                .FirstOrDefaultAsync(sg => sg.Id == id && !sg.IsDeleted);
            
            if (studentGroup == null)
                return new Response<string>(HttpStatusCode.NotFound, "Student group membership not found");

            // Выполняем мягкое удаление
            studentGroup.IsDeleted = true;
            studentGroup.UpdatedAt = DateTime.UtcNow;
            
            context.StudentGroups.Update(studentGroup);
            var result = await context.SaveChangesAsync();

            return result > 0
                ? new Response<string>(HttpStatusCode.OK, "Student removed from group successfully")
                : new Response<string>(HttpStatusCode.InternalServerError, "Failed to remove student from group");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }
    #endregion

    #region GetStudentGroupByIdAsync
    public async Task<Response<GetStudentGroupDto>> GetStudentGroupByIdAsync(int id)
    {
        try
        {
            var studentGroup = await context.StudentGroups
                .Include(sg => sg.Student)
                .Include(sg => sg.Group)
                .FirstOrDefaultAsync(sg => sg.Id == id && !sg.IsDeleted);
            
            var dto = new GetStudentGroupDto
            {
                Id = studentGroup.Id,
                GroupId = studentGroup.GroupId,
                GroupName = studentGroup.Group?.Name,
                student = new StudentDTO()
                {
                    Id = studentGroup.Student.Id,
                    Age = studentGroup.Student.Age,
                    FullName = studentGroup.Student.FullName,
                    PhoneNumber = studentGroup.Student.PhoneNumber,
                },
                JoinedDate = studentGroup.CreatedAt,
                IsActive = studentGroup.IsActive ?? false
            };

            return new Response<GetStudentGroupDto>(dto);
        }
        catch (Exception ex)
        {
            return new Response<GetStudentGroupDto>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }
    #endregion

    #region GetAllStudentGroupsAsync
    public async Task<Response<List<GetStudentGroupDto>>> GetAllStudentGroupsAsync()
    {
        try
        {
            var studentGroups = await context.StudentGroups
                .Include(sg => sg.Student)
                .Include(sg => sg.Group)
                .Where(sg => !sg.IsDeleted)
                .Select(sg => new GetStudentGroupDto
                {
                    Id = sg.Id,
                    GroupId = sg.GroupId,
                    GroupName = sg.Group.Name,
                    student = new StudentDTO()
                    {
                        Id = sg.Student.Id,
                        Age = sg.Student.Age,
                        FullName = sg.Student.FullName,
                        PhoneNumber = sg.Student.PhoneNumber,
                    },
                    JoinedDate = sg.CreatedAt,
                    IsActive = sg.IsActive ?? false
                })
                .ToListAsync();

            if (!studentGroups.Any())
                return new Response<List<GetStudentGroupDto>>(HttpStatusCode.NotFound, "No student group memberships found");

            return new Response<List<GetStudentGroupDto>>(studentGroups);
        }
        catch (Exception ex)
        {
            return new Response<List<GetStudentGroupDto>>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }
    #endregion

    #region GetStudentGroupsPaginated
    public async Task<PaginationResponse<List<GetStudentGroupDto>>> GetStudentGroupsPaginated(StudentGroupFilter filter)
    {
        try
        {
           
            var query = context.StudentGroups
                .Include(sg => sg.Student)
                .Include(sg => sg.Group)
                .Where(sg => !sg.IsDeleted)
                .AsQueryable();

            if (!string.IsNullOrEmpty(filter.Search))
            {
                query = query.Where(sg => sg.Student.FullName.Contains(filter.Search) ||
                                        sg.Group.Name.Contains(filter.Search));
            }

            if (filter.StudentId.HasValue)
            {
                query = query.Where(sg => sg.StudentId == filter.StudentId);
            }

            if (filter.GroupId.HasValue)
            {
                query = query.Where(sg => sg.GroupId == filter.GroupId);
            }

            if (filter.IsActive.HasValue)
            {
                query = query.Where(sg => sg.IsActive == filter.IsActive);
            }

            if (filter.JoinedDateFrom.HasValue)
            {
                query = query.Where(sg => sg.CreatedAt >= filter.JoinedDateFrom);
            }

            if (filter.JoinedDateTo.HasValue)
            {
                query = query.Where(sg => sg.CreatedAt <= filter.JoinedDateTo);
            }

            // Получаем общее количество записей для пагинации
            var totalCount = await query.CountAsync();

           
            var studentGroups = await query
                .OrderByDescending(sg => sg.CreatedAt)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(sg => new GetStudentGroupDto
                {
                    Id = sg.Id,
                    GroupId = sg.GroupId,
                    GroupName = sg.Group.Name,
                    student = new StudentDTO()
                    {
                        Id = sg.Student.Id,
                        Age = sg.Student.Age,
                        FullName = sg.Student.FullName,
                        PhoneNumber = sg.Student.PhoneNumber,
                    },
                    JoinedDate = sg.CreatedAt,
                    IsActive = sg.IsActive ?? false
                })
                .ToListAsync();

            return new PaginationResponse<List<GetStudentGroupDto>>(
                studentGroups,
                filter.PageNumber,
                filter.PageSize,
                totalCount
            );
        }
        catch (Exception ex)
        {
            return new PaginationResponse<List<GetStudentGroupDto>>(
                HttpStatusCode.InternalServerError,
                ex.Message
            );
        }
    }
    #endregion

    #region GetStudentGroupsByStudentAsync
    public async Task<Response<List<GetStudentGroupDto>>> GetStudentGroupsByStudentAsync(int studentId)
    {
        try
        {
            // Проверяем существование студента
            var student = await context.Students
                .FirstOrDefaultAsync(s => s.Id == studentId && !s.IsDeleted);
            
            if (student == null)
                return new Response<List<GetStudentGroupDto>>(HttpStatusCode.NotFound, "Student not found");

            var studentGroups = await context.StudentGroups
                .Include(sg => sg.Group)
                .Where(sg => sg.StudentId == studentId && !sg.IsDeleted)
                .Select(sg => new GetStudentGroupDto
                {
                    Id = sg.Id,
                    GroupId = sg.GroupId,
                    GroupName = sg.Group.Name,
                    student = new StudentDTO()
                    {
                        Id = sg.Student.Id,
                        Age = sg.Student.Age,
                        FullName = sg.Student.FullName,
                        PhoneNumber = sg.Student.PhoneNumber,
                    },
                    JoinedDate = sg.CreatedAt,
                    IsActive = sg.IsActive ?? false
                })
                .ToListAsync();

            if (!studentGroups.Any())
                return new Response<List<GetStudentGroupDto>>(HttpStatusCode.NotFound, "Student is not assigned to any groups");

            return new Response<List<GetStudentGroupDto>>(studentGroups);
        }
        catch (Exception ex)
        {
            return new Response<List<GetStudentGroupDto>>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }
    #endregion

    #region GetStudentGroupsByGroupAsync
    public async Task<Response<List<GetStudentGroupDto>>> GetStudentGroupsByGroupAsync(int groupId)
    {
        try
        {
           
            var group = await context.Groups
                .FirstOrDefaultAsync(g => g.Id == groupId && !g.IsDeleted);
            
            if (group == null)
                return new Response<List<GetStudentGroupDto>>(HttpStatusCode.NotFound, "Group not found");

            var studentGroups = await context.StudentGroups
                .Include(sg => sg.Student)
                .Where(sg => sg.GroupId == groupId && !sg.IsDeleted)
                .Select(sg => new GetStudentGroupDto
                {
                    Id = sg.Id,
                    GroupId = sg.GroupId,
                    GroupName = group.Name,
                    student = new StudentDTO()
                    {
                        Id = sg.Student.Id,
                        Age = sg.Student.Age,
                        FullName = sg.Student.FullName,
                        PhoneNumber = sg.Student.PhoneNumber,
                    },
                    JoinedDate = sg.CreatedAt,
                    IsActive = sg.IsActive ?? false
                })
                .ToListAsync();
            
            return new Response<List<GetStudentGroupDto>>(studentGroups);
        }
        catch (Exception ex)
        {
            return new Response<List<GetStudentGroupDto>>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }
    #endregion

    #region AddMultipleStudentsToGroupAsync
    public async Task<Response<string>> AddMultipleStudentsToGroupAsync(int groupId, List<int> studentIds)
    {
        try
        {
            if (studentIds == null || !studentIds.Any())
                return new Response<string>(HttpStatusCode.BadRequest, "No students specified");

            // Проверяем существование группы
            var group = await context.Groups
                .FirstOrDefaultAsync(g => g.Id == groupId && !g.IsDeleted);
            
            if (group == null)
                return new Response<string>(HttpStatusCode.NotFound, "Group not found");

            // Проверяем существование всех студентов
            var existingStudents = await context.Students
                .Where(s => studentIds.Contains(s.Id) && !s.IsDeleted)
                .Select(s => s.Id)
                .ToListAsync();
            
            var missingStudentIds = studentIds.Except(existingStudents).ToList();
            if (missingStudentIds.Any())
                return new Response<string>(HttpStatusCode.NotFound, $"Students with IDs {string.Join(", ", missingStudentIds)} not found");

            // Получаем существующие связи студентов с этой группой
            var existingStudentGroups = await context.StudentGroups
                .Where(sg => sg.GroupId == groupId && studentIds.Contains(sg.StudentId) && !sg.IsDeleted)
                .ToListAsync();
            
            // Обновляем существующие записи, делая их активными
            foreach (var sg in existingStudentGroups)
            {
                sg.IsActive = true;
                sg.UpdatedAt = DateTime.UtcNow;
                context.StudentGroups.Update(sg);
                studentIds.Remove(sg.StudentId); // Удаляем ID, чтобы не создавать новую запись
            }

            // Создаем новые записи для оставшихся студентов
            var newStudentGroups = studentIds.Select(studentId => new StudentGroup
            {
                StudentId = studentId,
                GroupId = groupId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            await context.StudentGroups.AddRangeAsync(newStudentGroups);
            var result = await context.SaveChangesAsync();

            return result > 0
                ? new Response<string>(HttpStatusCode.Created, "Students added to group successfully")
                : new Response<string>(HttpStatusCode.InternalServerError, "Failed to add students to group");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }
    #endregion

    #region RemoveStudentFromAllGroupsAsync
    public async Task<Response<string>> RemoveStudentFromAllGroupsAsync(int studentId)
    {
        try
        {
            // Проверяем существование студента
            var student = await context.Students
                .FirstOrDefaultAsync(s => s.Id == studentId && !s.IsDeleted);
            
            if (student == null)
                return new Response<string>(HttpStatusCode.NotFound, "Student not found");

            // Получаем все активные группы студента
            var studentGroups = await context.StudentGroups
                .Where(sg => sg.StudentId == studentId && !sg.IsDeleted)
                .ToListAsync();

            if (!studentGroups.Any())
                return new Response<string>(HttpStatusCode.NotFound, "Student is not assigned to any groups");

            // Отмечаем все связи как удаленные
            foreach (var sg in studentGroups)
            {
                sg.IsDeleted = true;
                sg.UpdatedAt = DateTime.UtcNow;
            }

            var result = await context.SaveChangesAsync();

            return result > 0
                ? new Response<string>(HttpStatusCode.OK, $"Student removed from {studentGroups.Count} groups successfully")
                : new Response<string>(HttpStatusCode.InternalServerError, "Failed to remove student from groups");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }
    #endregion

    #region GetActiveStudentsInGroupAsync
    public async Task<Response<List<GetStudentGroupDto>>> GetActiveStudentsInGroupAsync(int groupId)
    {
        try
        {
            var group = await context.Groups
                .FirstOrDefaultAsync(g => g.Id == groupId && !g.IsDeleted);
            
            if (group == null)
                return new Response<List<GetStudentGroupDto>>(HttpStatusCode.NotFound, "Group not found");

            var activeStudents = await context.StudentGroups
                .Include(sg => sg.Student)
                .Where(sg => sg.GroupId == groupId && 
                            sg.IsActive == true && 
                            !sg.IsDeleted)
                .Select(sg => new GetStudentGroupDto
                {
                    Id = sg.Id,
                    GroupId = sg.GroupId,
                    GroupName = group.Name,
                    student = new StudentDTO()
                    {
                        Id = sg.Student.Id,
                        Age = sg.Student.Age,
                        FullName = sg.Student.FullName,
                        PhoneNumber = sg.Student.PhoneNumber,
                    },
                    JoinedDate = sg.CreatedAt,
                    IsActive = sg.IsActive ?? false
                })
                .ToListAsync();

            if (!activeStudents.Any())
                return new Response<List<GetStudentGroupDto>>(HttpStatusCode.NotFound, "No active students in this group");

            return new Response<List<GetStudentGroupDto>>(activeStudents);
        }
        catch (Exception ex)
        {
            return new Response<List<GetStudentGroupDto>>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }
    #endregion

    #region GetInactiveStudentsInGroupAsync
    public async Task<Response<List<GetStudentGroupDto>>> GetInactiveStudentsInGroupAsync(int groupId)
    {
        try
        {
            var group = await context.Groups
                .FirstOrDefaultAsync(g => g.Id == groupId && !g.IsDeleted);
            
            if (group == null)
                return new Response<List<GetStudentGroupDto>>(HttpStatusCode.NotFound, "Group not found");

            var inactiveStudents = await context.StudentGroups
                .Include(sg => sg.Student)
                .Where(sg => sg.GroupId == groupId && 
                            sg.IsActive == false && 
                            !sg.IsDeleted)
                .Select(sg => new GetStudentGroupDto
                {
                    Id = sg.Id,
                    GroupId = sg.GroupId,
                    GroupName = group.Name,
                    student = new StudentDTO()
                    {
                        Id = sg.Student.Id,
                        Age = sg.Student.Age,
                        FullName = sg.Student.FullName,
                        PhoneNumber = sg.Student.PhoneNumber,
                    },
                    JoinedDate = sg.CreatedAt,
                    IsActive = sg.IsActive ?? false
                })
                .ToListAsync();

            if (!inactiveStudents.Any())
                return new Response<List<GetStudentGroupDto>>(HttpStatusCode.NotFound, "No inactive students in this group");

            return new Response<List<GetStudentGroupDto>>(inactiveStudents);
        }
        catch (Exception ex)
        {
            return new Response<List<GetStudentGroupDto>>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }
    #endregion

    #region ActivateStudentInGroupAsync
    public async Task<Response<string>> ActivateStudentInGroupAsync(int studentId, int groupId)
    {
        try
        {
            // Находим запись StudentGroup
            var studentGroup = await context.StudentGroups
                .FirstOrDefaultAsync(sg => sg.StudentId == studentId && 
                                         sg.GroupId == groupId && 
                                         !sg.IsDeleted);
            
            if (studentGroup == null)
                return new Response<string>(HttpStatusCode.NotFound, "Student group membership not found");

            if (studentGroup.IsActive == true)
                return new Response<string>(HttpStatusCode.BadRequest, "Student is already active in this group");

            // Активируем студента
            studentGroup.IsActive = true;
            studentGroup.UpdatedAt = DateTime.UtcNow;
            
            context.StudentGroups.Update(studentGroup);
            var result = await context.SaveChangesAsync();

            return result > 0
                ? new Response<string>(HttpStatusCode.OK, "Student activated in group successfully")
                : new Response<string>(HttpStatusCode.InternalServerError, "Failed to activate student in group");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }
    #endregion

    #region DeactivateStudentInGroupAsync
    public async Task<Response<string>> DeactivateStudentInGroupAsync(int studentId, int groupId)
    {
        try
        {
            // Находим запись StudentGroup
            var studentGroup = await context.StudentGroups
                .FirstOrDefaultAsync(sg => sg.StudentId == studentId && 
                                         sg.GroupId == groupId && 
                                         !sg.IsDeleted);
            
            if (studentGroup == null)
                return new Response<string>(HttpStatusCode.NotFound, "Student group membership not found");

            if (studentGroup.IsActive == false)
                return new Response<string>(HttpStatusCode.BadRequest, "Student is already inactive in this group");

            // Деактивируем студента
            studentGroup.IsActive = false;
            studentGroup.UpdatedAt = DateTime.UtcNow;
            
            context.StudentGroups.Update(studentGroup);
            var result = await context.SaveChangesAsync();

            return result > 0
                ? new Response<string>(HttpStatusCode.OK, "Student deactivated in group successfully")
                : new Response<string>(HttpStatusCode.InternalServerError, "Failed to deactivate student in group");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }
    #endregion

    #region GetStudentGroupCountAsync
    public async Task<Response<int>> GetStudentGroupCountAsync(int groupId)
    {
        try
        {
            var group = await context.Groups
                .FirstOrDefaultAsync(g => g.Id == groupId && !g.IsDeleted);
            
            if (group == null)
                return new Response<int>(HttpStatusCode.NotFound, "Group not found");

            var count = await context.StudentGroups
                .Where(sg => sg.GroupId == groupId && 
                            sg.IsActive == true && 
                            !sg.IsDeleted)
                .CountAsync();

            return new Response<int>(count);
        }
        catch (Exception ex)
        {
            return new Response<int>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }
    #endregion

    #region GetStudentGroupsCountAsync
    public async Task<Response<int>> GetStudentGroupsCountAsync(int studentId)
    {
        try
        {
            var student = await context.Students
                .FirstOrDefaultAsync(s => s.Id == studentId && !s.IsDeleted);
            
            if (student == null)
                return new Response<int>(HttpStatusCode.NotFound, "Student not found");

            var count = await context.StudentGroups
                .Where(sg => sg.StudentId == studentId && 
                            sg.IsActive == true && 
                            !sg.IsDeleted)
                .CountAsync();

            return new Response<int>(count);
        }
        catch (Exception ex)
        {
            return new Response<int>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }
    #endregion
}