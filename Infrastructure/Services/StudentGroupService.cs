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
using Infrastructure.Constants;

namespace Infrastructure.Services;

public class StudentGroupService(
    DataContext context,
    IJournalService journalService,
    IStudentAccountService studentAccountService,
    IDiscountService discountService) : IStudentGroupService
{
    #region CreateStudentGroupAsync

    public async Task<Response<string>> CreateStudentGroupAsync(CreateStudentGroup request)
    {
        try
        {
            var student = await context.Students.FirstOrDefaultAsync(s => s.Id == request.StudentId && !s.IsDeleted);
            if (student == null)
                return new Response<string>(HttpStatusCode.NotFound, Messages.Student.NotFound);

            var group = await context.Groups.FirstOrDefaultAsync(g => g.Id == request.GroupId && !g.IsDeleted);
            if (group == null)
                return new Response<string>(HttpStatusCode.NotFound, Messages.Group.NotFound);

            var existingStudentGroup = await context.StudentGroups
                .FirstOrDefaultAsync(sg => sg.StudentId == request.StudentId && sg.GroupId == request.GroupId);

            if (existingStudentGroup != null)
            {
                if (existingStudentGroup is { IsActive: true, IsDeleted: false, IsLeft: false })
                    return new Response<string>(HttpStatusCode.BadRequest, Messages.StudentGroup.StudentAlreadyAssigned);

                existingStudentGroup.IsActive = true;
                existingStudentGroup.IsDeleted = false;
                existingStudentGroup.IsLeft = false;
                existingStudentGroup.LeftReason = null;
                existingStudentGroup.LeftDate = null;
                existingStudentGroup.LeaveDate = null;
                existingStudentGroup.UpdatedAt = DateTimeOffset.UtcNow;
                context.StudentGroups.Update(existingStudentGroup);

                var updated = await context.SaveChangesAsync();
                if (updated > 0)
                {
                    await journalService.BackfillCurrentWeekForStudentAsync(request.GroupId, request.StudentId);
                    var now = DateTime.UtcNow;
                    await studentAccountService.ChargeForGroupAsync(request.StudentId, request.GroupId, now.Month, now.Year);
                    await studentAccountService.RecalculateStudentPaymentStatusAsync(request.StudentId, now.Month, now.Year);
                    return new Response<string>(HttpStatusCode.OK, Messages.StudentGroup.Reactivated);
                }

                return new Response<string>(HttpStatusCode.InternalServerError, Messages.StudentGroup.ReactivateFailed);
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
                await journalService.BackfillCurrentWeekForStudentAsync(request.GroupId, request.StudentId);
                var now = DateTime.UtcNow;
                await studentAccountService.ChargeForGroupAsync(request.StudentId, request.GroupId, now.Month, now.Year);
                await studentAccountService.RecalculateStudentPaymentStatusAsync(request.StudentId, now.Month, now.Year);
                return new Response<string>(HttpStatusCode.Created, Messages.StudentGroup.Created);
            }

            return new Response<string>(HttpStatusCode.InternalServerError, Messages.StudentGroup.CreateFailed);
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
                return new Response<string>(HttpStatusCode.NotFound, Messages.StudentGroup.MembershipNotFound);

            if (request.GroupId.HasValue)
            {
                var group = await context.Groups.FirstOrDefaultAsync(g => g.Id == request.GroupId.Value && !g.IsDeleted);
                if (group == null)
                    return new Response<string>(HttpStatusCode.NotFound, Messages.Group.NotFound);
                studentGroup.GroupId = request.GroupId.Value;
            }

            if (request.StudentId.HasValue)
            {
                var student = await context.Students.FirstOrDefaultAsync(s => s.Id == request.StudentId.Value && !s.IsDeleted);
                if (student == null)
                    return new Response<string>(HttpStatusCode.NotFound, Messages.Student.NotFound);
                studentGroup.StudentId = request.StudentId.Value;
            }

            if (request.IsActive.HasValue)
                studentGroup.IsActive = request.IsActive.Value;

            if (request.IsLeft.HasValue)
                studentGroup.IsLeft = request.IsLeft.Value;

            if (request.LeftReason != null)
                studentGroup.LeftReason = request.LeftReason;

            if (request.LeftDate.HasValue)
                studentGroup.LeftDate = request.LeftDate.Value;

            studentGroup.UpdatedAt = DateTimeOffset.UtcNow;

            context.StudentGroups.Update(studentGroup);
            var result = await context.SaveChangesAsync();

            return result > 0
                ? new Response<string>(HttpStatusCode.OK, Messages.StudentGroup.Updated)
                : new Response<string>(HttpStatusCode.InternalServerError, Messages.StudentGroup.UpdateFailed);
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    #endregion

    #region GetLeftStudentsInGroupAsync

    public async Task<Response<List<LeftStudentDto>>> GetLeftStudentsInGroupAsync(int groupId)
    {
        try
        {
            var group = await context.Groups.FirstOrDefaultAsync(g => g.Id == groupId && !g.IsDeleted);
            if (group == null)
                return new Response<List<LeftStudentDto>>(HttpStatusCode.NotFound, Messages.Group.NotFound);

            var leftStudents = await context.StudentGroups
                .Include(sg => sg.Student)
                .Where(sg => sg.GroupId == groupId && sg.IsLeft && !sg.IsDeleted)
                .Select(sg => new LeftStudentDto
                {
                    StudentId = sg.StudentId,
                    GroupId = sg.GroupId,
                    GroupName = group.Name,
                    FullName = sg.Student!.FullName,
                    Birthday = sg.Student.Birthday,
                    PhoneNumber = sg.Student.PhoneNumber,
                    ImagePath = sg.Student.ProfileImage ?? context.Users
                        .Where(u => u.Id == sg.Student.UserId)
                        .Select(u => u.ProfileImagePath)
                        .FirstOrDefault(),
                    LeftReason = sg.LeftReason,
                    LeftDate = sg.LeftDate
                })
                .ToListAsync();

            return new Response<List<LeftStudentDto>>(leftStudents);
        }
        catch (Exception ex)
        {
            return new Response<List<LeftStudentDto>>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    #endregion

    #region DeleteStudentGroupAsync

    public async Task<Response<string>> DeleteStudentGroupAsync(int id)
    {
        try
        {
            var studentGroup = await context.StudentGroups.FirstOrDefaultAsync(sg => sg.Id == id && !sg.IsDeleted);
            if (studentGroup == null)
                return new Response<string>(HttpStatusCode.NotFound, Messages.StudentGroup.MembershipNotFound);

            studentGroup.IsDeleted = true;
            studentGroup.IsActive = false;
            studentGroup.LeaveDate = DateTime.UtcNow;
            studentGroup.UpdatedAt = DateTimeOffset.UtcNow;

            context.StudentGroups.Update(studentGroup);
            var result = await context.SaveChangesAsync();

            return result > 0
                ? new Response<string>(HttpStatusCode.OK, Messages.StudentGroup.Deleted)
                : new Response<string>(HttpStatusCode.InternalServerError, Messages.StudentGroup.DeleteFailed);
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
                return new Response<GetStudentGroupDto>(HttpStatusCode.NotFound, Messages.StudentGroup.MembershipNotFound);

            var dto = await BuildStudentGroupDtoAsync(studentGroup, studentGroup.Group?.Name, DateTime.UtcNow);
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
                .Where(sg => !sg.IsDeleted && !sg.IsLeft)
                .ToListAsync();

            if (!studentGroups.Any())
                return new Response<List<GetStudentGroupDto>>(HttpStatusCode.NotFound, Messages.StudentGroup.NoMembershipsFound);

            var dtos = await BuildStudentGroupDtosAsync(studentGroups, DateTime.UtcNow);
            return new Response<List<GetStudentGroupDto>>(dtos);
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
                .Where(sg => !sg.IsDeleted && !sg.IsLeft);

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                query = query.Where(sg =>
                    sg.Student!.FullName.Contains(filter.Search) ||
                    sg.Group!.Name.Contains(filter.Search));
            }

            if (filter.StudentId.HasValue)
                query = query.Where(sg => sg.StudentId == filter.StudentId.Value);

            if (filter.GroupId.HasValue)
                query = query.Where(sg => sg.GroupId == filter.GroupId.Value);

            if (filter.IsActive.HasValue)
                query = query.Where(sg => sg.IsActive == filter.IsActive.Value);

            if (filter.JoinedDateFrom.HasValue)
                query = query.Where(sg => sg.JoinDate >= filter.JoinedDateFrom.Value);

            if (filter.JoinedDateTo.HasValue)
                query = query.Where(sg => sg.JoinDate <= filter.JoinedDateTo.Value);

            var totalCount = await query.CountAsync();
            var studentGroups = await query
                .OrderByDescending(sg => sg.JoinDate)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            var dtos = await BuildStudentGroupDtosAsync(studentGroups, DateTime.UtcNow);

            return new PaginationResponse<List<GetStudentGroupDto>>(
                dtos,
                totalCount,
                filter.PageNumber,
                filter.PageSize);
        }
        catch (Exception ex)
        {
            return new PaginationResponse<List<GetStudentGroupDto>>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    #endregion

    #region GetStudentGroupsByStudentAsync

    public async Task<Response<List<GetStudentGroupDto>>> GetStudentGroupsByStudentAsync(int studentId)
    {
        try
        {
            var student = await context.Students.FirstOrDefaultAsync(s => s.Id == studentId && !s.IsDeleted);
            if (student == null)
                return new Response<List<GetStudentGroupDto>>(HttpStatusCode.NotFound, Messages.Student.NotFound);

            var studentGroups = await context.StudentGroups
                .Include(sg => sg.Student)
                .Include(sg => sg.Group)
                .Where(sg => sg.StudentId == studentId && !sg.IsDeleted && !sg.IsLeft)
                .ToListAsync();

            if (!studentGroups.Any())
                return new Response<List<GetStudentGroupDto>>(HttpStatusCode.NotFound, Messages.StudentGroup.StudentNotInGroups);

            var dtos = await BuildStudentGroupDtosAsync(studentGroups, DateTime.UtcNow);
            return new Response<List<GetStudentGroupDto>>(dtos);
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
            var group = await context.Groups.FirstOrDefaultAsync(g => g.Id == groupId && !g.IsDeleted);
            if (group == null)
                return new Response<List<GetStudentGroupDto>>(HttpStatusCode.NotFound, Messages.Group.NotFound);

            var studentGroups = await context.StudentGroups
                .Include(sg => sg.Student)
                .Where(sg => sg.GroupId == groupId && !sg.IsDeleted && !sg.IsLeft)
                .ToListAsync();

            if (!studentGroups.Any())
                return new Response<List<GetStudentGroupDto>>(new List<GetStudentGroupDto>());

            var dtos = new List<GetStudentGroupDto>();
            var nowUtc = DateTime.UtcNow;
            foreach (var studentGroup in studentGroups)
            {
                dtos.Add(await BuildStudentGroupDtoAsync(studentGroup, group.Name, nowUtc));
            }

            return new Response<List<GetStudentGroupDto>>(dtos);
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
                        await studentAccountService.RecalculateStudentPaymentStatusAsync(sid, now.Month, now.Year);
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
                return new Response<string>(HttpStatusCode.NotFound, Messages.Student.NotFound);

            var group = await context.Groups
                .FirstOrDefaultAsync(g => g.Id == groupId && !g.IsDeleted);
            
            if (group == null)
                return new Response<string>(HttpStatusCode.NotFound, Messages.Group.NotFound);

            var studentGroup = await context.StudentGroups
                .FirstOrDefaultAsync(sg => sg.StudentId == studentId && 
                                           sg.GroupId == groupId);
            
            if (studentGroup == null)
                return new Response<string>(HttpStatusCode.NotFound, Messages.StudentGroup.MembershipNotFound);

            context.StudentGroups.Remove(studentGroup);
            var result = await context.SaveChangesAsync();

            if (result > 0)
            {
                
                _ = await journalService.RemoveFutureEntriesForStudentAsync(groupId, studentId);
                return new Response<string>(HttpStatusCode.OK, Messages.StudentGroup.Deleted);
            }
            return new Response<string>(HttpStatusCode.InternalServerError, Messages.StudentGroup.DeleteFailed);
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
                            sg.IsLeft == false &&
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

            foreach (var item in activeStudents)
            {
                if (item.student.PaymentStatus != PaymentStatus.Completed)
                {
                    var preview = await discountService.PreviewAsync(item.student.Id, item.GroupId, now4.Month, now4.Year);
                    if (preview.Data.PayableAmount == 0)
                    {
                        item.student.PaymentStatus = PaymentStatus.Completed;
                    }
                }
            }

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

            foreach (var item in inactiveStudents)
            {
                if (item.student.PaymentStatus != PaymentStatus.Completed)
                {
                    var preview = await discountService.PreviewAsync(item.student.Id, item.GroupId, now5.Month, now5.Year);
                    if (preview.Data.PayableAmount == 0)
                    {
                        item.student.PaymentStatus = PaymentStatus.Completed;
                    }
                }
            }

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


    #region TransferStudentsGroupBulk
    public async Task<Response<string>> TransferStudentsGroupBulk(int sourceGroupId, int targetGroupId, List<int> studentIds)
    {
        try
        {
            if (sourceGroupId == targetGroupId)
                return new Response<string>(HttpStatusCode.BadRequest, "Группа-источник и группа-цель не могут быть одинаковыми");

            if (!studentIds.Any())
                return new Response<string>(HttpStatusCode.BadRequest, "Список студентов пуст");

            var sourceGroup = await context.Groups.FirstOrDefaultAsync(g => g.Id == sourceGroupId && !g.IsDeleted);
            if (sourceGroup == null)
                return new Response<string>(HttpStatusCode.NotFound, "Исходная группа не найдена");

            var targetGroup = await context.Groups.FirstOrDefaultAsync(g => g.Id == targetGroupId && !g.IsDeleted);
            if (targetGroup == null)
                return new Response<string>(HttpStatusCode.NotFound, "Целевая группа не найдена");

            var existingStudents = await context.Students
                .Where(s => studentIds.Contains(s.Id) && !s.IsDeleted)
                .Select(s => s.Id).ToListAsync();

            var missing = studentIds.Except(existingStudents).ToList();
            if (missing.Any())
                return new Response<string>(HttpStatusCode.NotFound, $"Студенты с ID {string.Join(", ", missing)} не найдены");

            var movedCount = 0;
            var skippedCount = 0;

            foreach (var studentId in studentIds.ToList())
            {
                var studentInSourceGroup = await context.StudentGroups
                    .FirstOrDefaultAsync(sg => sg.StudentId == studentId &&
                                             sg.GroupId == sourceGroupId &&
                                             sg.IsActive &&
                                             !sg.IsDeleted);
                if (studentInSourceGroup == null)
                {
                    skippedCount++;
                    continue;
                }

                studentInSourceGroup.IsActive = false;
                studentInSourceGroup.LeaveDate = DateTime.UtcNow;
                studentInSourceGroup.UpdatedAt = DateTimeOffset.UtcNow;
                context.StudentGroups.Update(studentInSourceGroup);
                
                var studentInTargetGroup = await context.StudentGroups
                    .FirstOrDefaultAsync(sg => sg.StudentId == studentId && sg.GroupId == targetGroupId);

                if (studentInTargetGroup != null)
                {
                    if (studentInTargetGroup.IsActive && !studentInTargetGroup.IsDeleted && !studentInTargetGroup.IsLeft)
                    {
                        skippedCount++;
                        continue;
                    }

                    studentInTargetGroup.IsActive = true;
                    studentInTargetGroup.IsDeleted = false;
                    studentInTargetGroup.IsLeft = false;
                    studentInTargetGroup.LeftReason = null;
                    studentInTargetGroup.LeftDate = null;
                    studentInTargetGroup.LeaveDate = null;
                    studentInTargetGroup.JoinDate = DateTime.UtcNow;
                    studentInTargetGroup.UpdatedAt = DateTimeOffset.UtcNow;
                    context.StudentGroups.Update(studentInTargetGroup);
                }
                else
                {
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

                movedCount++;
            }

            var saved = await context.SaveChangesAsync();

            if (saved > 0)
            {
                foreach (var studentId in studentIds)
                {
                    _ = await journalService.RemoveFutureEntriesForStudentAsync(sourceGroupId, studentId);
                    _ = await journalService.BackfillCurrentWeekForStudentAsync(targetGroupId, studentId);
                    var now = DateTime.UtcNow;
                    _ = await studentAccountService.ChargeForGroupAsync(studentId, targetGroupId, now.Month, now.Year);
                    _ = await studentAccountService.RecalculateStudentPaymentStatusAsync(studentId, now.Month, now.Year);
                }

                var msg = $"Переведено: {movedCount}. Пропущено: {skippedCount}.";
                return new Response<string>(HttpStatusCode.OK, msg);
            }

            return new Response<string>(HttpStatusCode.InternalServerError, "Не удалось выполнить массовый перенос студентов");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }
    #endregion

    #region Helper Methods

    private async Task<GetStudentGroupDto> BuildStudentGroupDtoAsync(StudentGroup studentGroup, string? groupName, DateTime nowUtc)
    {
        var dto = new GetStudentGroupDto
        {
            Id = studentGroup.Id,
            GroupId = studentGroup.GroupId,
            GroupName = groupName,
            student = new StudentDTO
            {
                Id = studentGroup.Student!.Id,
                ImagePath = studentGroup.Student.ProfileImage ?? await context.Users
                    .Where(u => u.Id == studentGroup.Student.UserId)
                    .Select(u => u.ProfileImagePath)
                    .FirstOrDefaultAsync(),
                Age = studentGroup.Student.Age,
                FullName = studentGroup.Student.FullName,
                PhoneNumber = studentGroup.Student.PhoneNumber,
                JoinedDate = studentGroup.JoinDate,
                PaymentStatus = await context.Payments.AnyAsync(p =>
                    !p.IsDeleted &&
                    p.StudentId == studentGroup.StudentId &&
                    p.GroupId == studentGroup.GroupId &&
                    p.Year == nowUtc.Year &&
                    p.Month == nowUtc.Month &&
                    (p.Status == PaymentStatus.Completed || p.Status == PaymentStatus.Paid))
                    ? PaymentStatus.Completed
                    : PaymentStatus.Pending,
                Discount = await context.StudentGroupDiscounts
                    .Where(sgd => sgd.StudentId == studentGroup.Student.Id && sgd.GroupId == studentGroup.GroupId && !sgd.IsDeleted)
                    .Select(sgd => new GetStudentGroupDiscountDto
                    {
                        Id = sgd.Id,
                        StudentId = sgd.StudentId,
                        GroupId = sgd.GroupId,
                        DiscountAmount = sgd.DiscountAmount
                    })
                    .FirstOrDefaultAsync() ?? new GetStudentGroupDiscountDto()
            },
            IsActive = studentGroup.IsActive,
            IsLeft = studentGroup.IsLeft,
            LeftReason = studentGroup.LeftReason,
            LeftDate = studentGroup.LeftDate,
            JoinDate = studentGroup.JoinDate,
            LeaveDate = studentGroup.LeaveDate
        };

        if (dto.student.PaymentStatus != PaymentStatus.Completed)
        {
            var preview = await discountService.PreviewAsync(dto.student.Id, dto.GroupId, nowUtc.Month, nowUtc.Year);
            if (preview.Data.PayableAmount == 0)
            {
                dto.student.PaymentStatus = PaymentStatus.Completed;
            }
        }

        return dto;
    }

    private async Task<List<GetStudentGroupDto>> BuildStudentGroupDtosAsync(List<StudentGroup> studentGroups, DateTime nowUtc)
    {
        var dtos = new List<GetStudentGroupDto>();
        foreach (var studentGroup in studentGroups)
        {
            dtos.Add(await BuildStudentGroupDtoAsync(studentGroup, studentGroup.Group?.Name, nowUtc));
        }
        return dtos;
    }

    private async Task<Response<string>> AfterActivateChargeAsync(int studentId, int groupId)
    {
        await journalService.BackfillCurrentWeekForStudentAsync(groupId, studentId);
        var now = DateTime.UtcNow;
        await studentAccountService.ChargeForGroupAsync(studentId, groupId, now.Month, now.Year);
        await studentAccountService.RecalculateStudentPaymentStatusAsync(studentId, now.Month, now.Year);
        return new Response<string>(HttpStatusCode.OK, Messages.StudentGroup.Reactivated);
    }

    #endregion
}
