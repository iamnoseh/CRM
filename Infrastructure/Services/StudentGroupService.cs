using System.Net;
using Domain.DTOs.Discounts;
using Domain.DTOs.StudentGroup;
using Domain.Enums;
using Domain.Entities;
using Domain.Filters;
using Domain.Responses;
using Infrastructure.Data;
using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class StudentGroupService(DataContext context, IJournalService journalService, Infrastructure.Interfaces.IStudentAccountService studentAccountService) : IStudentGroupService
{
    private async Task<Response<string>> AfterActivateChargeAsync(int studentId, int groupId)
    {
        var now = DateTime.UtcNow;
        await studentAccountService.ChargeForGroupAsync(studentId, groupId, now.Month, now.Year);
        return new Response<string>(HttpStatusCode.OK, "Студент успешно активирован в группе");
    }
    #region CreateStudentGroupAsync
    public async Task<Response<string>> CreateStudentGroupAsync(CreateStudentGroup request)
    {
        try
        {
            var student = await context.Students.FirstOrDefaultAsync(s => s.Id == request.StudentId && !s.IsDeleted);
            if (student == null)
                return new Response<string>(HttpStatusCode.NotFound, "Студент не найден");

            var group = await context.Groups.FirstOrDefaultAsync(g => g.Id == request.GroupId && !g.IsDeleted);
            if (group == null)
                return new Response<string>(HttpStatusCode.NotFound, "Группа не найдена");
            var existingStudentGroup = await context.StudentGroups
                .FirstOrDefaultAsync(sg => sg.StudentId == request.StudentId && 
                                          sg.GroupId == request.GroupId);
            
            if (existingStudentGroup != null)
            {
                if (existingStudentGroup.IsActive && !existingStudentGroup.IsDeleted && !existingStudentGroup.IsLeft)
                    return new Response<string>(HttpStatusCode.BadRequest, "Студент уже назначен в эту группу");

                existingStudentGroup.IsActive = true;
                existingStudentGroup.IsDeleted = false;
                existingStudentGroup.IsLeft = false;
                existingStudentGroup.LeftReason = null;
                existingStudentGroup.LeftDate = null;
                existingStudentGroup.LeaveDate = null;
                existingStudentGroup.UpdatedAt = DateTimeOffset.UtcNow;
                context.StudentGroups.Update(existingStudentGroup);
                
                var updateResult = await context.SaveChangesAsync();
                if (updateResult > 0)
                {
                    _ = await journalService.BackfillCurrentWeekForStudentAsync(request.GroupId, request.StudentId);
                    // charge immediately on re-activation as well
                    var now = DateTime.UtcNow;
                    _ = await studentAccountService.ChargeForGroupAsync(request.StudentId, request.GroupId, now.Month, now.Year);
                    return new Response<string>(HttpStatusCode.OK, "Членство студента в группе переактивировано");
                }
                return new Response<string>(HttpStatusCode.InternalServerError, "Не удалось переактивировать членство студента в группе");
            }

            var studentGroup = new StudentGroup
            {
                StudentId = request.StudentId,
                GroupId = request.GroupId,
                IsActive = request.IsActive,
                JoinDate = DateTime.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            await context.StudentGroups.AddAsync(studentGroup);
            var result = await context.SaveChangesAsync();

            if (result > 0)
            {
                _ = await journalService.BackfillCurrentWeekForStudentAsync(request.GroupId, request.StudentId);
                // attempt immediate monthly charge for the new group
                var now = DateTime.UtcNow;
                _ = await studentAccountService.ChargeForGroupAsync(request.StudentId, request.GroupId, now.Month, now.Year);
                return new Response<string>(HttpStatusCode.Created, "Студент успешно добавлен в группу");
            }
            return new Response<string>(HttpStatusCode.InternalServerError, "Не удалось добавить студента в группу");
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
            var studentGroup = await context.StudentGroups
                .FirstOrDefaultAsync(sg => sg.Id == id && !sg.IsDeleted);
            
            if (studentGroup == null)
                return new Response<string>(HttpStatusCode.NotFound, "Членство студента в группе не найдено");

            if (request.StudentId.HasValue && request.StudentId.Value != studentGroup.StudentId)
            {
                var student = await context.Students
                    .FirstOrDefaultAsync(s => s.Id == request.StudentId.Value && !s.IsDeleted);
                
                if (student == null)
                    return new Response<string>(HttpStatusCode.NotFound, "Студент не найден");
                
                studentGroup.StudentId = request.StudentId.Value;
            }

            if (request.GroupId.HasValue && request.GroupId.Value != studentGroup.GroupId)
            {
                var group = await context.Groups
                    .FirstOrDefaultAsync(g => g.Id == request.GroupId.Value && !g.IsDeleted);
                
                if (group == null)
                    return new Response<string>(HttpStatusCode.NotFound, "Группа не найдена");
                
                studentGroup.GroupId = request.GroupId.Value;
            }

            if (request.StudentId.HasValue && request.GroupId.HasValue)
            {
                var existingStudentGroup = await context.StudentGroups
                    .FirstOrDefaultAsync(sg => sg.StudentId == request.StudentId.Value && 
                                             sg.GroupId == request.GroupId.Value && 
                                             sg.Id != id &&
                                             !sg.IsDeleted);
                
                if (existingStudentGroup != null)
                    return new Response<string>(HttpStatusCode.BadRequest, "Студент уже назначен в эту группу");
            }

            if (request.IsActive.HasValue)
            {
                studentGroup.IsActive = request.IsActive.Value;
                
                if (!request.IsActive.Value && studentGroup.LeaveDate == null)
                {
                    studentGroup.LeaveDate = DateTime.UtcNow;
                }
                else if (request.IsActive.Value)
                {
                    studentGroup.LeaveDate = null;
                }
            }

            studentGroup.UpdatedAt = DateTimeOffset.UtcNow;
            
            context.StudentGroups.Update(studentGroup);
            var result = await context.SaveChangesAsync();

            return result > 0
                ? new Response<string>(HttpStatusCode.OK, "Членство студента в группе успешно обновлено")
                : new Response<string>(HttpStatusCode.InternalServerError, "Не удалось обновить членство студента в группе");
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
            var studentGroup = await context.StudentGroups
                .FirstOrDefaultAsync(sg => sg.Id == id && !sg.IsDeleted);
            
            if (studentGroup == null)
                return new Response<string>(HttpStatusCode.NotFound, "Членство студента в группе не найдено");

            studentGroup.IsDeleted = true;
            studentGroup.IsActive = false;
            studentGroup.LeaveDate = DateTime.UtcNow;
            studentGroup.UpdatedAt = DateTimeOffset.UtcNow;
            
            context.StudentGroups.Update(studentGroup);
            var result = await context.SaveChangesAsync();

            return result > 0
                ? new Response<string>(HttpStatusCode.OK, "Студент успешно удален из группы")
                : new Response<string>(HttpStatusCode.InternalServerError, "Не удалось удалить студента из группы");
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
            
            if (studentGroup == null)
                return new Response<GetStudentGroupDto>(HttpStatusCode.NotFound, "Членство студента в группе не найдено");
            
            var studentGroupDiscount = await context.StudentGroupDiscounts
                .FirstOrDefaultAsync(sd => sd.StudentId == studentGroup.StudentId && sd.GroupId == studentGroup.GroupId);

            var now = DateTime.UtcNow;
            var dto = new GetStudentGroupDto
            {
                Id = studentGroup.Id,
                GroupId = studentGroup.GroupId,
                GroupName = studentGroup.Group?.Name,
                student = new StudentDTO()
                {
                    Id = studentGroup.Student!.Id,
                    ImagePath = context.Users.Where(u => u.Id == studentGroup.Student.UserId).Select(u => u.ProfileImagePath).FirstOrDefault() ?? studentGroup.Student.ProfileImage,
                    Age = studentGroup.Student.Age,
                    FullName = studentGroup.Student.FullName,
                    PhoneNumber = studentGroup.Student.PhoneNumber,
                    JoinedDate = studentGroup.JoinDate,  
                    PaymentStatus = await context.Payments.AnyAsync(p => !p.IsDeleted && p.StudentId == studentGroup.StudentId && p.GroupId == studentGroup.GroupId && p.Year == now.Year && p.Month == now.Month && (p.Status == PaymentStatus.Completed || p.Status == PaymentStatus.Paid)) ? PaymentStatus.Completed : PaymentStatus.Pending,
                    Discount = studentGroupDiscount != null ? new GetStudentGroupDiscountDto 
                    {
                        Id = studentGroupDiscount.Id,
                        StudentId = studentGroupDiscount.StudentId,
                        GroupId = studentGroupDiscount.GroupId,
                        DiscountAmount = studentGroupDiscount.DiscountAmount
                    } : new GetStudentGroupDiscountDto()
                },
                IsActive = studentGroup.IsActive,
                IsLeft = studentGroup.IsLeft,
                LeftReason = studentGroup.LeftReason,
                LeftDate = studentGroup.LeftDate,
                JoinDate = studentGroup.JoinDate,
                LeaveDate = studentGroup.LeaveDate
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
            var now1 = DateTime.UtcNow;
            var studentGroups = await context.StudentGroups
                .Include(sg => sg.Student)
                .Include(sg => sg.Group)
                .Where(sg => !sg.IsDeleted)
                .Select(sg => new GetStudentGroupDto
                {
                    Id = sg.Id,
                    GroupId = sg.GroupId,
                    GroupName = sg.Group!.Name,
                    student = new StudentDTO{
                        Id = sg.Student!.Id,
                        ImagePath = sg.Student.ProfileImage ?? context.Users.Where(u => u.Id == sg.Student.UserId).Select(u => u.ProfileImagePath).FirstOrDefault(),
                        Age = sg.Student.Age,
                        FullName = sg.Student.FullName,
                        PhoneNumber = sg.Student.PhoneNumber,
                        JoinedDate = sg.JoinDate,
                        PaymentStatus = context.Payments.Any(p => !p.IsDeleted && p.StudentId == sg.StudentId && p.GroupId == sg.GroupId && p.Year == now1.Year && p.Month == now1.Month && (p.Status == PaymentStatus.Completed || p.Status == PaymentStatus.Paid)) ? PaymentStatus.Completed : PaymentStatus.Pending,
                        Discount = context.StudentGroupDiscounts
                                            .Where(sgd => sgd.StudentId == sg.Student.Id && sgd.GroupId == sg.GroupId)
                                            .Select(sgd => new GetStudentGroupDiscountDto 
                                            { 
                                                Id = sgd.Id, 
                                                StudentId = sgd.StudentId, 
                                                GroupId = sgd.GroupId, 
                                                DiscountAmount = sgd.DiscountAmount 
                                            }).FirstOrDefault() ?? new GetStudentGroupDiscountDto()
                    },
                    IsActive = sg.IsActive,
                    IsLeft = sg.IsLeft,
                    LeftReason = sg.LeftReason,
                    LeftDate = sg.LeftDate,
                    JoinDate = sg.JoinDate,
                    LeaveDate = sg.LeaveDate
                })
                .ToListAsync();

            if (!studentGroups.Any())
                return new Response<List<GetStudentGroupDto>>(HttpStatusCode.NotFound, "Членства студентов в группах не найдены");

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
                query = query.Where(sg => sg.Student!.FullName.Contains(filter.Search) ||
                                        sg.Group!.Name.Contains(filter.Search));
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
                query = query.Where(sg => sg.JoinDate >= filter.JoinedDateFrom);
            }

            if (filter.JoinedDateTo.HasValue)
            {
                query = query.Where(sg => sg.JoinDate <= filter.JoinedDateTo);
            }

            var totalCount = await query.CountAsync();

            var now2 = DateTime.UtcNow;
            var studentGroups = await query
                .OrderByDescending(sg => sg.JoinDate)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(sg => new GetStudentGroupDto
                {
                    Id = sg.Id,
                    GroupId = sg.GroupId,
                    GroupName = sg.Group!.Name,
                    student = new StudentDTO
                    {
                        Id = sg.Student!.Id,
                        ImagePath = sg.Student.ProfileImage ?? context.Users.Where(u => u.Id == sg.Student.UserId).Select(u => u.ProfileImagePath).FirstOrDefault(),
                        Age = sg.Student.Age,
                        FullName = sg.Student.FullName,
                        PhoneNumber = sg.Student.PhoneNumber,
                        JoinedDate = sg.JoinDate,
                        PaymentStatus = context.Payments.Any(p => !p.IsDeleted && p.StudentId == sg.StudentId && p.GroupId == sg.GroupId && p.Year == now2.Year && p.Month == now2.Month && (p.Status == PaymentStatus.Completed || p.Status == PaymentStatus.Paid)) ? PaymentStatus.Completed : PaymentStatus.Pending,
                        Discount = context.StudentGroupDiscounts
                                            .Where(sgd => sgd.StudentId == sg.Student.Id && sgd.GroupId == sg.GroupId)
                                            .Select(sgd => new GetStudentGroupDiscountDto 
                                            { 
                                                Id = sgd.Id, 
                                                StudentId = sgd.StudentId, 
                                                GroupId = sgd.GroupId, 
                                                DiscountAmount = sgd.DiscountAmount 
                                            }).FirstOrDefault() ?? new GetStudentGroupDiscountDto()
                    },
                    IsActive = sg.IsActive,
                    IsLeft = sg.IsLeft,
                    LeftReason = sg.LeftReason,
                    LeftDate = sg.LeftDate,
                    JoinDate = sg.JoinDate,
                    LeaveDate = sg.LeaveDate
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
            var student = await context.Students
                .FirstOrDefaultAsync(s => s.Id == studentId && !s.IsDeleted);
            
            if (student == null)
                return new Response<List<GetStudentGroupDto>>(HttpStatusCode.NotFound, "Студент не найден");

            var now2 = DateTime.UtcNow;
            var studentGroups = await context.StudentGroups
                .Include(sg => sg.Group)
                .Include(sg => sg.Student)
                .Where(sg => sg.StudentId == studentId && !sg.IsDeleted)
                .Select(sg => new GetStudentGroupDto
                {
                    Id = sg.Id,
                    GroupId = sg.GroupId,
                    GroupName = sg.Group!.Name,
                    student = new StudentDTO()
                    {
                        Id = sg.Student!.Id,
                        ImagePath = sg.Student.ProfileImage ?? context.Users.Where(u => u.Id == sg.Student.UserId).Select(u => u.ProfileImagePath).FirstOrDefault(),
                        Age = sg.Student.Age,
                        FullName = sg.Student.FullName,
                        PhoneNumber = sg.Student.PhoneNumber,
                        JoinedDate = sg.JoinDate,
                        PaymentStatus = context.Payments.Any(p => !p.IsDeleted && p.StudentId == sg.StudentId && p.GroupId == sg.GroupId && p.Year == now2.Year && p.Month == now2.Month && (p.Status == PaymentStatus.Completed || p.Status == PaymentStatus.Paid)) ? PaymentStatus.Completed : PaymentStatus.Pending,
                        Discount = context.StudentGroupDiscounts
                                            .Where(sgd => sgd.StudentId == sg.Student.Id && sgd.GroupId == sg.GroupId)
                                            .Select(sgd => new GetStudentGroupDiscountDto 
                                            { 
                                                Id = sgd.Id, 
                                                StudentId = sgd.StudentId, 
                                                GroupId = sgd.GroupId, 
                                                DiscountAmount = sgd.DiscountAmount 
                                            }).FirstOrDefault() ?? new GetStudentGroupDiscountDto()
                    },
                    IsActive = sg.IsActive,
                    IsLeft = sg.IsLeft,
                    LeftReason = sg.LeftReason,
                    LeftDate = sg.LeftDate,
                    JoinDate = sg.JoinDate,
                    LeaveDate = sg.LeaveDate
                })
                .ToListAsync();

            if (!studentGroups.Any())
                return new Response<List<GetStudentGroupDto>>(HttpStatusCode.NotFound, "Студент не назначен ни в одну группу");

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
                return new Response<List<GetStudentGroupDto>>(HttpStatusCode.NotFound, "Группа не найдена");

            var now3 = DateTime.UtcNow;
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
                        Id = sg.Student!.Id,
                        ImagePath = sg.Student.ProfileImage ?? context.Users.Where(u => u.Id == sg.Student.UserId).Select(u => u.ProfileImagePath).FirstOrDefault(),
                        Age = sg.Student.Age,
                        FullName = sg.Student.FullName,
                        PhoneNumber = sg.Student.PhoneNumber,
                        JoinedDate = sg.JoinDate,
                        PaymentStatus = context.Payments.Any(p => !p.IsDeleted && p.StudentId == sg.StudentId && p.GroupId == sg.GroupId && p.Year == now3.Year && p.Month == now3.Month && (p.Status == PaymentStatus.Completed || p.Status == PaymentStatus.Paid)) ? PaymentStatus.Completed : PaymentStatus.Pending,
                        Discount = context.StudentGroupDiscounts
                                            .Where(sgd => sgd.StudentId == sg.Student.Id && sgd.GroupId == sg.GroupId)
                                            .Select(sgd => new GetStudentGroupDiscountDto 
                                            { 
                                                Id = sgd.Id, 
                                                StudentId = sgd.StudentId, 
                                                GroupId = sgd.GroupId, 
                                                DiscountAmount = sgd.DiscountAmount 
                                            }).FirstOrDefault() ?? new GetStudentGroupDiscountDto()
                    },
                    IsActive = sg.IsActive,
                    IsLeft = sg.IsLeft,
                    LeftReason = sg.LeftReason,
                    LeftDate = sg.LeftDate,
                    JoinDate = sg.JoinDate,
                    LeaveDate = sg.LeaveDate
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
            if (!studentIds.Any())
                return new Response<string>(HttpStatusCode.BadRequest, "Студенты не указаны");

            var group = await context.Groups
                .FirstOrDefaultAsync(g => g.Id == groupId && !g.IsDeleted);
            
            if (group == null)
                return new Response<string>(HttpStatusCode.NotFound, "Группа не найдена");

            var existingStudents = await context.Students
                .Where(s => studentIds.Contains(s.Id) && !s.IsDeleted)
                .Select(s => s.Id)
                .ToListAsync();
            
            var missingStudentIds = studentIds.Except(existingStudents).ToList();
            if (missingStudentIds.Any())
                return new Response<string>(HttpStatusCode.NotFound, $"Студенты с ID {string.Join(", ", missingStudentIds)} не найдены");

            var existingStudentGroups = await context.StudentGroups
                .Where(sg => sg.GroupId == groupId && studentIds.Contains(sg.StudentId) && !sg.IsDeleted)
                .ToListAsync();
            
            foreach (var sg in existingStudentGroups)
            {
                sg.IsActive = true;
                sg.LeaveDate = null;
                sg.UpdatedAt = DateTimeOffset.UtcNow;
                context.StudentGroups.Update(sg);
                studentIds.Remove(sg.StudentId); 
            }

            var newStudentGroups = studentIds.Select(studentId => new StudentGroup
            {
                StudentId = studentId,
                GroupId = groupId,
                IsActive = true,
                JoinDate = DateTime.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });

            await context.StudentGroups.AddRangeAsync(newStudentGroups);
            var result = await context.SaveChangesAsync();

            if (result > 0)
            {
                var affectedIds = existingStudentGroups.Select(x => x.StudentId).Concat(studentIds).Distinct().ToList();
                if (affectedIds.Count > 0)
                {
                    _ = await journalService.BackfillCurrentWeekForStudentsAsync(groupId, affectedIds);
                    // charge each affected student for current month
                    var now = DateTime.UtcNow;
                    foreach (var sid in affectedIds)
                    {
                        await studentAccountService.ChargeForGroupAsync(sid, groupId, now.Month, now.Year);
                    }
                }
                return new Response<string>(HttpStatusCode.Created, "Студенты успешно добавлены в группу");
            }
            return new Response<string>(HttpStatusCode.InternalServerError, "Не удалось добавить студентов в группу");
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
            var student = await context.Students
                .FirstOrDefaultAsync(s => s.Id == studentId && !s.IsDeleted);
            
            if (student == null)
                return new Response<string>(HttpStatusCode.NotFound, "Студент не найден");

            var studentGroups = await context.StudentGroups
                .Where(sg => sg.StudentId == studentId && !sg.IsDeleted)
                .ToListAsync();

            if (!studentGroups.Any())
                return new Response<string>(HttpStatusCode.NotFound, "Студент не назначен ни в одну группу");

            foreach (var sg in studentGroups)
            {
                sg.IsDeleted = true;
                sg.IsActive = false;
                sg.LeaveDate = DateTime.UtcNow;
                sg.UpdatedAt = DateTimeOffset.UtcNow;
            }

            var result = await context.SaveChangesAsync();

            return result > 0
                ? new Response<string>(HttpStatusCode.OK, $"Студент успешно удален из {studentGroups.Count} групп")
                : new Response<string>(HttpStatusCode.InternalServerError, "Не удалось удалить студента из групп");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }
    #endregion

    #region RemoveStudentFromGroup
    public async Task<Response<string>> RemoveStudentFromGroup(int studentId, int groupId)
    {
        try
        {
            var student = await context.Students
                .FirstOrDefaultAsync(s => s.Id == studentId && !s.IsDeleted);
            
            if (student == null)
                return new Response<string>(HttpStatusCode.NotFound, "Студент не найден");

            var group = await context.Groups
                .FirstOrDefaultAsync(g => g.Id == groupId && !g.IsDeleted);
            
            if (group == null)
                return new Response<string>(HttpStatusCode.NotFound, "Группа не найдена");

            var studentGroup = await context.StudentGroups
                .FirstOrDefaultAsync(sg => sg.StudentId == studentId && 
                                           sg.GroupId == groupId);
            
            if (studentGroup == null)
                return new Response<string>(HttpStatusCode.NotFound, "Студент не назначен в эту группу");

            // Soft-remove: deactivate and mark as deleted to hide from active lists,
            // but keep the record for potential reactivation later
            studentGroup.IsDeleted = true;
            studentGroup.IsActive = false;
            studentGroup.LeaveDate = DateTime.UtcNow;
            studentGroup.UpdatedAt = DateTimeOffset.UtcNow;
            
            context.StudentGroups.Update(studentGroup);
            var result = await context.SaveChangesAsync();

            if (result > 0)
            {
                // Cleanup any future journal entries for this student in this group
                _ = await journalService.RemoveFutureEntriesForStudentAsync(groupId, studentId);
                return new Response<string>(HttpStatusCode.OK, "Студент успешно удален из группы");
            }
            return new Response<string>(HttpStatusCode.InternalServerError, "Не удалось удалить студента из группы");
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
                return new Response<List<GetStudentGroupDto>>(HttpStatusCode.NotFound, "Группа не найдена");

            var now4 = DateTime.UtcNow;
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
                        Id = sg.Student!.Id,
                        ImagePath = sg.Student.ProfileImage ?? context.Users.Where(u => u.Id == sg.Student.UserId).Select(u => u.ProfileImagePath).FirstOrDefault(),
                        Age = sg.Student.Age,
                        FullName = sg.Student.FullName,
                        PhoneNumber = sg.Student.PhoneNumber,
                        JoinedDate = sg.JoinDate,
                        PaymentStatus = context.Payments.Any(p => !p.IsDeleted && p.StudentId == sg.StudentId && p.GroupId == sg.GroupId && p.Year == now4.Year && p.Month == now4.Month && (p.Status == PaymentStatus.Completed || p.Status == PaymentStatus.Paid)) ? PaymentStatus.Completed : PaymentStatus.Pending,
                        Discount = context.StudentGroupDiscounts
                                            .Where(sgd => sgd.StudentId == sg.Student.Id && sgd.GroupId == sg.GroupId)
                                            .Select(sgd => new GetStudentGroupDiscountDto 
                                            { 
                                                Id = sgd.Id, 
                                                StudentId = sgd.StudentId, 
                                                GroupId = sgd.GroupId, 
                                                DiscountAmount = sgd.DiscountAmount 
                                            }).FirstOrDefault() ?? new GetStudentGroupDiscountDto()
                    },
                    IsActive = sg.IsActive,
                    IsLeft = sg.IsLeft,
                    LeftReason = sg.LeftReason,
                    LeftDate = sg.LeftDate,
                    JoinDate = sg.JoinDate,
                    LeaveDate = sg.LeaveDate
                })
                .ToListAsync();

            if (!activeStudents.Any())
                return new Response<List<GetStudentGroupDto>>(HttpStatusCode.NotFound, "В этой группе нет активных студентов");

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
                return new Response<List<GetStudentGroupDto>>(HttpStatusCode.NotFound, "Группа не найдена");

            var now5 = DateTime.UtcNow;
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
                        Id = sg.Student!.Id,
                        ImagePath = sg.Student.ProfileImage ?? context.Users.Where(u => u.Id == sg.Student.UserId).Select(u => u.ProfileImagePath).FirstOrDefault(),
                        Age = sg.Student.Age,
                        FullName = sg.Student.FullName,
                        PhoneNumber = sg.Student.PhoneNumber,
                        JoinedDate = sg.JoinDate,
                        PaymentStatus = context.Payments.Any(p => !p.IsDeleted && p.StudentId == sg.StudentId && p.GroupId == sg.GroupId && p.Year == now5.Year && p.Month == now5.Month && (p.Status == PaymentStatus.Completed || p.Status == PaymentStatus.Paid)) ? PaymentStatus.Completed : PaymentStatus.Pending,
                        Discount = context.StudentGroupDiscounts
                                            .Where(sgd => sgd.StudentId == sg.Student.Id && sgd.GroupId == sg.GroupId)
                                            .Select(sgd => new GetStudentGroupDiscountDto 
                                            { 
                                                Id = sgd.Id, 
                                                StudentId = sgd.StudentId, 
                                                GroupId = sgd.GroupId, 
                                                DiscountAmount = sgd.DiscountAmount 
                                            }).FirstOrDefault() ?? new GetStudentGroupDiscountDto()
                    },
                    IsActive = sg.IsActive,
                    IsLeft = sg.IsLeft,
                    LeftReason = sg.LeftReason,
                    LeftDate = sg.LeftDate,
                    JoinDate = sg.JoinDate,
                    LeaveDate = sg.LeaveDate
                })
                .ToListAsync();

            if (!inactiveStudents.Any())
                return new Response<List<GetStudentGroupDto>>(HttpStatusCode.NotFound, "В этой группе нет неактивных студентов");

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
            var studentGroup = await context.StudentGroups
                .FirstOrDefaultAsync(sg => sg.StudentId == studentId && 
                                         sg.GroupId == groupId && 
                                         !sg.IsDeleted);
            
            if (studentGroup == null)
                return new Response<string>(HttpStatusCode.NotFound, "Членство студента в группе не найдено");

            if (studentGroup.IsActive)
                return new Response<string>(HttpStatusCode.BadRequest, "Студент уже активен в этой группе");

            studentGroup.IsActive = true;
            studentGroup.LeaveDate = null;
            studentGroup.UpdatedAt = DateTimeOffset.UtcNow;
            
            context.StudentGroups.Update(studentGroup);
            var result = await context.SaveChangesAsync();

            return result > 0
                ? await AfterActivateChargeAsync(studentId, groupId)
                : new Response<string>(HttpStatusCode.InternalServerError, "Не удалось активировать студента в группе");
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
            var studentGroup = await context.StudentGroups
                .FirstOrDefaultAsync(sg => sg.StudentId == studentId && 
                                         sg.GroupId == groupId && 
                                         !sg.IsDeleted);
            
            if (studentGroup == null)
                return new Response<string>(HttpStatusCode.NotFound, "Членство студента в группе не найдено");

            if (studentGroup.IsActive == false)
                return new Response<string>(HttpStatusCode.BadRequest, "Студент уже неактивен в этой группе");

            studentGroup.IsActive = false;
            studentGroup.LeaveDate = DateTime.UtcNow;
            studentGroup.UpdatedAt = DateTimeOffset.UtcNow;
            
            context.StudentGroups.Update(studentGroup);
            var result = await context.SaveChangesAsync();

            if (result > 0)
            {
                _ = await journalService.RemoveFutureEntriesForStudentAsync(groupId, studentId);
                return new Response<string>(HttpStatusCode.OK, "Студент успешно деактивирован в группе");
            }
            return new Response<string>(HttpStatusCode.InternalServerError, "Не удалось деактивировать студента в группе");
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
                return new Response<int>(HttpStatusCode.NotFound, "Группа не найдена");

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
                return new Response<int>(HttpStatusCode.NotFound, "Студент не найден");

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

    #region LeftStudentFromGroup
    public async Task<Response<string>> LeftStudentFromGroup(int studentId, int groupId, string leftReason)
    {
        try
        {
            var studentGroup = await context.StudentGroups
                .FirstOrDefaultAsync(sg => sg.StudentId == studentId && 
                                         sg.GroupId == groupId && 
                                         !sg.IsDeleted);
            
            if (studentGroup == null)
                return new Response<string>(HttpStatusCode.NotFound, "Членство студента в группе не найдено");

            if (studentGroup.IsLeft)
                return new Response<string>(HttpStatusCode.BadRequest, "Студент уже покинул эту группу");

            studentGroup.IsLeft = true;
            studentGroup.LeftReason = leftReason;
            studentGroup.LeftDate = DateTime.UtcNow;
            studentGroup.IsActive = false;
            studentGroup.LeaveDate = DateTime.UtcNow;
            studentGroup.UpdatedAt = DateTimeOffset.UtcNow;
            
            context.StudentGroups.Update(studentGroup);
            var result = await context.SaveChangesAsync();

            if (result > 0)
            {
                _ = await journalService.RemoveFutureEntriesForStudentAsync(groupId, studentId);
                return new Response<string>(HttpStatusCode.OK, "Студент успешно покинул группу");
            }
            return new Response<string>(HttpStatusCode.InternalServerError, "Не удалось удалить студента из группы");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }
    #endregion

    #region ReverseLeftStudentFromGroup
    public async Task<Response<string>> ReverseLeftStudentFromGroup(int studentId, int groupId)
    {
        try
        {
            var studentGroup = await context.StudentGroups
                .FirstOrDefaultAsync(sg => sg.StudentId == studentId && 
                                         sg.GroupId == groupId && 
                                         sg.IsLeft && 
                                         !sg.IsDeleted);
            
            if (studentGroup == null)
                return new Response<string>(HttpStatusCode.NotFound, "Членство студента в группе не найдено или студент не покидал группу");

            studentGroup.IsLeft = false;
            studentGroup.LeftReason = null;
            studentGroup.LeftDate = null;
            studentGroup.IsActive = true; // Re-activate student upon reversal
            studentGroup.LeaveDate = null;
            studentGroup.UpdatedAt = DateTimeOffset.UtcNow;
            
            context.StudentGroups.Update(studentGroup);
            var result = await context.SaveChangesAsync();

            if (result > 0)
            {
                _ = await journalService.BackfillCurrentWeekForStudentAsync(groupId, studentId); 
                return new Response<string>(HttpStatusCode.OK, "Студент успешно возвращен в группу");
            }
            return new Response<string>(HttpStatusCode.InternalServerError, "Не удалось вернуть студента в группу");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }
    #endregion

    #region TransferStudentGroup
    public async Task<Response<string>> TransferStudentGroup(int studentId, int sourceGroupId, int targetGroupId)
    {
        try
        {
            if (sourceGroupId == targetGroupId)
                return new Response<string>(HttpStatusCode.BadRequest, "Группа-источник и группа-цель не могут быть одинаковыми");

            var student = await context.Students.FirstOrDefaultAsync(s => s.Id == studentId && !s.IsDeleted);
            if (student == null)
                return new Response<string>(HttpStatusCode.NotFound, "Студент не найден");

            var sourceGroup = await context.Groups.FirstOrDefaultAsync(g => g.Id == sourceGroupId && !g.IsDeleted);
            if (sourceGroup == null)
                return new Response<string>(HttpStatusCode.NotFound, "Исходная группа не найдена");

            var targetGroup = await context.Groups.FirstOrDefaultAsync(g => g.Id == targetGroupId && !g.IsDeleted);
            if (targetGroup == null)
                return new Response<string>(HttpStatusCode.NotFound, "Целевая группа не найдена");

            var studentInSourceGroup = await context.StudentGroups
                .FirstOrDefaultAsync(sg => sg.StudentId == studentId && 
                                         sg.GroupId == sourceGroupId && 
                                         sg.IsActive && 
                                         !sg.IsDeleted);
            
            if (studentInSourceGroup == null)
                return new Response<string>(HttpStatusCode.BadRequest, "Студент не является активным членом исходной группы");

            // Deactivate from source group
            studentInSourceGroup.IsActive = false;
            studentInSourceGroup.LeaveDate = DateTime.UtcNow;
            studentInSourceGroup.UpdatedAt = DateTimeOffset.UtcNow;
            context.StudentGroups.Update(studentInSourceGroup);

            // Check if student already exists in target group (even if inactive/deleted/left)
            var studentInTargetGroup = await context.StudentGroups
                .FirstOrDefaultAsync(sg => sg.StudentId == studentId && 
                                         sg.GroupId == targetGroupId);

            if (studentInTargetGroup != null)
            {
                // Reactivate if already exists
                if (studentInTargetGroup.IsActive && !studentInTargetGroup.IsDeleted && !studentInTargetGroup.IsLeft)
                    return new Response<string>(HttpStatusCode.BadRequest, "Студент уже активен в целевой группе");

                studentInTargetGroup.IsActive = true;
                studentInTargetGroup.IsDeleted = false;
                studentInTargetGroup.IsLeft = false;
                studentInTargetGroup.LeftReason = null;
                studentInTargetGroup.LeftDate = null;
                studentInTargetGroup.LeaveDate = null;
                studentInTargetGroup.JoinDate = DateTime.UtcNow; // Update join date to reflect transfer
                studentInTargetGroup.UpdatedAt = DateTimeOffset.UtcNow;
                context.StudentGroups.Update(studentInTargetGroup);
            }
            else
            {
                // Create new entry if not exists
                var newStudentGroup = new StudentGroup
                {
                    StudentId = studentId,
                    GroupId = targetGroupId,
                    IsActive = true,
                    JoinDate = DateTime.UtcNow,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                };
                await context.StudentGroups.AddAsync(newStudentGroup);
            }

            var result = await context.SaveChangesAsync();

            if (result > 0)
            {
                _ = await journalService.RemoveFutureEntriesForStudentAsync(sourceGroupId, studentId);
                _ = await journalService.BackfillCurrentWeekForStudentAsync(targetGroupId, studentId);
                // charge for target group for current month
                var now = DateTime.UtcNow;
                _ = await studentAccountService.ChargeForGroupAsync(studentId, targetGroupId, now.Month, now.Year);
                return new Response<string>(HttpStatusCode.OK, "Студент успешно переведен в новую группу");
            }
            return new Response<string>(HttpStatusCode.InternalServerError, "Не удалось перевести студента в новую группу");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }
    #endregion
}
