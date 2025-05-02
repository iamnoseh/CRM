// using System.Net;
// using Domain.DTOs.StudentGroup;
// using Domain.Entities;
// using Domain.Responses;
// using Infrastructure.Data;
// using Infrastructure.Interfaces;
// using Microsoft.EntityFrameworkCore;
//
// namespace Infrastructure.Services;
//
// public class StudentGroupService (DataContext context) : IStudentGroupService
// {
//     #region CreateStudentGroup
//     
//     public async Task<Response<string>> CreateStudentGroupAsync(CreateStudentGroup request)
//     {
//         var student = await context.Students.FirstOrDefaultAsync(x=> x.Id == request.StudentId);
//         if (student == null) return new Response<string>(HttpStatusCode.NotFound,"Student not found");
//         var group = await context.Groups.FirstOrDefaultAsync(x=> x.Id == request.GroupId);
//         if (group == null) return new Response<string>(HttpStatusCode.NotFound,"Group not found");
//         var studentGroup = new StudentGroup
//         {
//             GroupId = request.GroupId,
//             StudentId = request.StudentId,
//             CreatedAt = DateTime.UtcNow,
//             UpdatedAt = DateTime.UtcNow,
//             IsActive = true
//         };
//         await context.StudentGroups.AddAsync(studentGroup);
//         var result = await context.SaveChangesAsync();
//         return result > 0 
//             ? new Response<string>(HttpStatusCode.Created, "Group created")
//             : new Response<string>(HttpStatusCode.BadRequest, "Group creation failed");
//     }
//     
//     #endregion
//     
//     #region UpdateStudentGroup
//     public async Task<Response<string>> UpdateStudentGroupAsync(int id, UpdateStudentGroupDto request)
//     {
//         var studentGroup = await context.StudentGroups.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
//         if (studentGroup == null) return new Response<string>(HttpStatusCode.NotFound,"StudentGroup not found");
//         
//         var student = await context.Students.FirstOrDefaultAsync(x => x.Id == request.StudentId);
//         if (student == null) return new Response<string>(HttpStatusCode.NotFound,"Student not found");
//         
//         var group = await context.Groups.FirstOrDefaultAsync(x => x.Id == request.GroupId);
//         if (group == null) return new Response<string>(HttpStatusCode.NotFound,"Group not found");
//         
//         if (request.GroupId != null) studentGroup.GroupId = (int)request.GroupId;
//         if (request.StudentId != null) studentGroup.StudentId = (int)request.StudentId;
//         if (request.IsActive.HasValue) studentGroup.IsActive = request.IsActive.Value;
//         
//         studentGroup.UpdatedAt = DateTime.UtcNow;
//         context.StudentGroups.Update(studentGroup);
//         var result = await context.SaveChangesAsync();
//         
//         return result > 0
//             ? new Response<string>(HttpStatusCode.OK, "Student group updated Successfully")
//             : new Response<string>(HttpStatusCode.BadRequest, "Student group update failed");
//     }
//     #endregion
//
//     #region DeleteStudentGroup
//     public async Task<Response<string>> DeleteStudentGroupAsync(int id)
//     {
//         var studentGroup = await context.StudentGroups.FirstOrDefaultAsync(x=> x.Id == id);
//         if (studentGroup == null) return new Response<string>(HttpStatusCode.NotFound,"Student not found");
//         studentGroup.UpdatedAt = DateTime.UtcNow;
//         studentGroup.IsDeleted = true;
//         context.StudentGroups.Update(studentGroup);
//         var result = await context.SaveChangesAsync();
//         return result > 0
//             ? new Response<string>(HttpStatusCode.OK, "Group deleted Successfully")
//             : new Response<string>(HttpStatusCode.BadRequest, "Group deletion failed");
//     }
//     #endregion
//     
//     #region GetStudentGroup
//     public async Task<Response<GetStudentGroupDto>> GetStudentGroupByIdAsync(int id, string language = "En")
//     {
//         var studentType = typeof(Student);
//         var groupType = typeof(Group);
//
//         var studentGroup = await context.StudentGroups
//             .Include(x => x.Group)
//             .Include(x => x.Student)
//             .FirstOrDefaultAsync(x => x.Id == id);
//
//         if (studentGroup == null) 
//             return new Response<GetStudentGroupDto>(HttpStatusCode.NotFound, "Student group not found");
//
//         string propertySuffix = char.ToUpper(language[0]) + language.Substring(1).ToLower();
//     
//         var dto = new GetStudentGroupDto
//         {
//             GroupId = studentGroup.GroupId,
//             StudentId = studentGroup.StudentId,
//             GroupName = groupType.GetProperty("Name" + propertySuffix)?.GetValue(studentGroup.Group)?.ToString(),
//             StudentFullName = studentType.GetProperty("FullName" + propertySuffix)?.GetValue(studentGroup.Student)?.ToString(),
//             IsActive = studentGroup.IsActive != null && studentGroup.IsActive.Value,
//             JoinedDate = studentGroup.CreatedAt,
//         };
//
//         return new Response<GetStudentGroupDto>(dto);
//     }
//     #endregion
//
//     #region GetStudents
//     public async Task<Response<List<GetStudentGroupDto>>> GetAllStudentGroupsAsync(string language = "En")
//     {
//         var studentType = typeof(Student);
//         var groupType = typeof(Group);
//     
//         string propertySuffix = char.ToUpper(language[0]) + language.Substring(1).ToLower();
//
//         var studentGroups = await context.StudentGroups
//             .Include(x => x.Group)
//             .Include(x => x.Student)
//             .ToListAsync();
//         if (!studentGroups.Any()) 
//             return new Response<List<GetStudentGroupDto>>(HttpStatusCode.NotFound, "Student groups not found");
//
//         var dtoList = studentGroups.Select(studentGroup => new GetStudentGroupDto
//         {
//             GroupId = studentGroup.GroupId,
//             StudentId = studentGroup.StudentId,
//             GroupName = groupType.GetProperty("Name" + propertySuffix)?.GetValue(studentGroup.Group)?.ToString(),
//             StudentFullName = studentType.GetProperty("FullName" + propertySuffix)?.GetValue(studentGroup.Student)?.ToString(),
//             IsActive = studentGroup.IsActive.HasValue && studentGroup.IsActive.Value,
//             JoinedDate = studentGroup.CreatedAt,
//         }).ToList();
//
//         return new Response<List<GetStudentGroupDto>>(dtoList);
//     }
//     #endregion
//
//     #region GetStudentGroupsByStudent
//     public async Task<Response<List<GetStudentGroupDto>>> GetStudentGroupsByStudentAsync(int studentId, string language = "En")
//     {
//         var student = await context.Students.FirstOrDefaultAsync(x => x.Id == studentId && !x.IsDeleted);
//         if (student == null) 
//             return new Response<List<GetStudentGroupDto>>(HttpStatusCode.NotFound, "Student not found");
//             
//         var studentType = typeof(Student);
//         var groupType = typeof(Group);
//     
//         string propertySuffix = char.ToUpper(language[0]) + language.Substring(1).ToLower();
//
//         var studentGroups = await context.StudentGroups
//             .Include(x => x.Group)
//             .Include(x => x.Student)
//             .Where(x => x.StudentId == studentId && !x.IsDeleted)
//             .ToListAsync();
//             
//         if (!studentGroups.Any()) 
//             return new Response<List<GetStudentGroupDto>>(HttpStatusCode.NotFound, "Student groups not found for this student");
//
//         var dtoList = studentGroups.Select(studentGroup => new GetStudentGroupDto
//         {
//             GroupId = studentGroup.GroupId,
//             StudentId = studentGroup.StudentId,
//             GroupName = groupType.GetProperty("Name" + propertySuffix)?.GetValue(studentGroup.Group)?.ToString(),
//             StudentFullName = studentType.GetProperty("FullName" + propertySuffix)?.GetValue(studentGroup.Student)?.ToString(),
//             IsActive = studentGroup.IsActive.HasValue && studentGroup.IsActive.Value,
//             JoinedDate = studentGroup.CreatedAt,
//         }).ToList();
//
//         return new Response<List<GetStudentGroupDto>>(dtoList);
//     }
//     #endregion
//     
//     #region GetStudentGroupsByGroup
//     public async Task<Response<List<GetStudentGroupDto>>> GetStudentGroupsByGroupAsync(int groupId, string language = "En")
//     {
//         var group = await context.Groups.FirstOrDefaultAsync(x => x.Id == groupId && !x.IsDeleted);
//         if (group == null) 
//             return new Response<List<GetStudentGroupDto>>(HttpStatusCode.NotFound, "Group not found");
//             
//         var studentType = typeof(Student);
//         var groupType = typeof(Group);
//     
//         string propertySuffix = char.ToUpper(language[0]) + language.Substring(1).ToLower();
//
//         var studentGroups = await context.StudentGroups
//             .Include(x => x.Group)
//             .Include(x => x.Student)
//             .Where(x => x.GroupId == groupId && !x.IsDeleted)
//             .ToListAsync();
//             
//         if (!studentGroups.Any()) 
//             return new Response<List<GetStudentGroupDto>>(HttpStatusCode.NotFound, "Student groups not found for this group");
//
//         var dtoList = studentGroups.Select(studentGroup => new GetStudentGroupDto
//         {
//             GroupId = studentGroup.GroupId,
//             StudentId = studentGroup.StudentId,
//             GroupName = groupType.GetProperty("Name" + propertySuffix)?.GetValue(studentGroup.Group)?.ToString(),
//             StudentFullName = studentType.GetProperty("FullName" + propertySuffix)?.GetValue(studentGroup.Student)?.ToString(),
//             IsActive = studentGroup.IsActive.HasValue && studentGroup.IsActive.Value,
//             JoinedDate = studentGroup.CreatedAt,
//         }).ToList();
//
//         return new Response<List<GetStudentGroupDto>>(dtoList);
//     }
//     #endregion
// }