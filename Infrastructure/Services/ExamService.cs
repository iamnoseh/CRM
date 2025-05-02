// using System.Net;
// using Domain.DTOs.Exam;
// using Domain.Entities;
// using Domain.Responses;
// using Infrastructure.Data;
// using Infrastructure.Interfaces;
// using Microsoft.EntityFrameworkCore;
//
// namespace Infrastructure.Services;
//
// public class ExamService(DataContext context) : IExamService
// {
//     public async Task<Response<GetExamDto>> GetExamById(int id)
//     {
//         var exam = await context.Exams
//             .Include(x => x.Student)
//             .Include(x => x.Group)
//             .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
//             
//         if (exam == null) return new Response<GetExamDto>(HttpStatusCode.NotFound, "Exam not found");
//
//         var dto = new GetExamDto
//         {
//             Id = exam.Id,
//             Value = exam.Value,
//             BonusPoints = exam.BonusPoints,
//             Comment = exam.Comment,
//             StudentId = exam.StudentId,
//             GroupId = exam.GroupId,
//             WeekIndex = exam.WeekIndex
//         };
//         return new Response<GetExamDto>(dto);
//     }
//
//     public async Task<Response<List<GetExamDto>>> GetExams()
//     {
//         var exams = await context.Exams
//             .Include(x => x.Student)
//             .Include(x => x.Group)
//             .Where(x => !x.IsDeleted)
//             .ToListAsync();
//             
//         if (exams.Count == 0) return new Response<List<GetExamDto>>(HttpStatusCode.NotFound, "No exams found");
//
//         var dto = exams.Select(x => new GetExamDto
//         {
//             Id = x.Id,
//             Value = x.Value,
//             BonusPoints = x.BonusPoints,
//             Comment = x.Comment,
//             StudentId = x.StudentId,
//             GroupId = x.GroupId,
//             WeekIndex = x.WeekIndex
//         }).ToList();
//         return new Response<List<GetExamDto>>(dto);
//     }
//     
//     public async Task<Response<List<GetExamDto>>> GetExamsByStudent(int studentId)
//     {
//         var student = await context.Students.FirstOrDefaultAsync(x => x.Id == studentId && !x.IsDeleted);
//         if (student == null) return new Response<List<GetExamDto>>(HttpStatusCode.NotFound, "Student not found");
//         
//         var exams = await context.Exams
//             .Include(x => x.Student)
//             .Include(x => x.Group)
//             .Where(x => x.StudentId == studentId && !x.IsDeleted)
//             .ToListAsync();
//         
//         if (exams.Count == 0) 
//             return new Response<List<GetExamDto>>(HttpStatusCode.NotFound, "No exams found for this student");
//
//         var dto = exams.Select(x => new GetExamDto
//         {
//             Id = x.Id,
//             Value = x.Value,
//             BonusPoints = x.BonusPoints,
//             Comment = x.Comment,
//             StudentId = x.StudentId,
//             GroupId = x.GroupId,
//             WeekIndex = x.WeekIndex
//         }).ToList();
//         
//         return new Response<List<GetExamDto>>(dto);
//     }
//     
//     public async Task<Response<List<GetExamDto>>> GetExamsByGroup(int groupId)
//     {
//         var group = await context.Groups.FirstOrDefaultAsync(x => x.Id == groupId && !x.IsDeleted);
//         if (group == null) return new Response<List<GetExamDto>>(HttpStatusCode.NotFound, "Group not found");
//         
//         var exams = await context.Exams
//             .Include(x => x.Student)
//             .Include(x => x.Group)
//             .Where(x => x.GroupId == groupId && !x.IsDeleted)
//             .ToListAsync();
//         
//         if (exams.Count == 0) 
//             return new Response<List<GetExamDto>>(HttpStatusCode.NotFound, "No exams found for this group");
//
//         var dto = exams.Select(x => new GetExamDto
//         {
//             Id = x.Id,
//             Value = x.Value,
//             BonusPoints = x.BonusPoints,
//             Comment = x.Comment,
//             StudentId = x.StudentId,
//             GroupId = x.GroupId,
//             WeekIndex = x.WeekIndex
//         }).ToList();
//         
//         return new Response<List<GetExamDto>>(dto);
//     }
//
//     public async Task<Response<string>> CreateExam(CreateExamDto examDto)
//     {
//         var student = await context.Students.FirstOrDefaultAsync(x => x.Id == examDto.StudentId && !x.IsDeleted);
//         if (student == null) return new Response<string>(HttpStatusCode.NotFound, "Student not found");
//
//         var group = await context.Groups.FirstOrDefaultAsync(x => x.Id == examDto.GroupId && !x.IsDeleted);
//         if (group == null) return new Response<string>(HttpStatusCode.NotFound, "Group not found");
//
//         var newExam = new Exam
//         {
//             StudentId = examDto.StudentId,
//             GroupId = examDto.GroupId,
//             WeekIndex = examDto.WeekIndex,
//             Value = examDto.Value,
//             BonusPoints = examDto.BonusPoints,
//             Comment = examDto.Comment,
//             CreatedAt = DateTime.UtcNow,
//             UpdatedAt = DateTime.UtcNow,
//             IsDeleted = false
//         };
//         
//         await context.Exams.AddAsync(newExam);
//         var res = await context.SaveChangesAsync();
//
//         return res > 0
//             ? new Response<string>(HttpStatusCode.Created, "Exam created successfully")
//             : new Response<string>(HttpStatusCode.InternalServerError, "Failed to create exam");
//     }
//
//     public async Task<Response<string>> UpdateExam(int id, UpdateExamDto examDto)
//     {
//         var exam = await context.Exams.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
//         if (exam == null) return new Response<string>(HttpStatusCode.NotFound, "Exam not found");
//         
//         exam.Value = examDto.Value;
//         exam.BonusPoints = examDto.BonusPoints;
//         exam.Comment = examDto.Comment;
//         exam.UpdatedAt = DateTime.UtcNow;
//         
//         context.Exams.Update(exam);
//         var res = await context.SaveChangesAsync();
//         
//         return res > 0
//             ? new Response<string>(HttpStatusCode.OK, "Exam updated successfully")
//             : new Response<string>(HttpStatusCode.InternalServerError, "Failed to update exam");
//     }
//
//     public async Task<Response<string>> DeleteExam(int id)
//     {
//         var exam = await context.Exams.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
//         if (exam == null) return new Response<string>(HttpStatusCode.NotFound, "Exam not found");
//
//         exam.IsDeleted = true;
//         exam.UpdatedAt = DateTime.UtcNow;
//         
//         context.Exams.Update(exam);
//         var res = await context.SaveChangesAsync();
//
//         return res > 0
//             ? new Response<string>(HttpStatusCode.OK, "Exam deleted successfully")
//             : new Response<string>(HttpStatusCode.InternalServerError, "Failed to delete exam");
//     }
//
//     public async Task<Response<List<GetExamDto>>> GetExamsByGroupAndWeek(int groupId, int weekIndex)
//     {
//         var group = await context.Groups.FirstOrDefaultAsync(x => x.Id == groupId && !x.IsDeleted);
//         if (group == null) return new Response<List<GetExamDto>>(HttpStatusCode.NotFound, "Group not found");
//         
//         var exams = await context.Exams
//             .Include(x => x.Student)
//             .Include(x => x.Group)
//             .Where(x => x.GroupId == groupId && x.WeekIndex == weekIndex && !x.IsDeleted)
//             .ToListAsync();
//             
//         if (exams.Count == 0) 
//             return new Response<List<GetExamDto>>(HttpStatusCode.NotFound, "Exams not found for this group and week");
//             
//         var examDtos = exams.Select(x => new GetExamDto
//         {
//             Id = x.Id,
//             Value = x.Value,
//             BonusPoints = x.BonusPoints,
//             Comment = x.Comment,
//             StudentId = x.StudentId,
//             GroupId = x.GroupId,
//             WeekIndex = x.WeekIndex
//         }).ToList();
//         
//         return new Response<List<GetExamDto>>(examDtos);
//     }
//     
//     public async Task<Response<List<GetExamDto>>> GetStudentExamsByWeek(int studentId, int groupId, int weekIndex)
//     {
//         var student = await context.Students.FirstOrDefaultAsync(x => x.Id == studentId && !x.IsDeleted);
//         if (student == null) return new Response<List<GetExamDto>>(HttpStatusCode.NotFound, "Student not found");
//         
//         var group = await context.Groups.FirstOrDefaultAsync(x => x.Id == groupId && !x.IsDeleted);
//         if (group == null) return new Response<List<GetExamDto>>(HttpStatusCode.NotFound, "Group not found");
//         
//         var exams = await context.Exams
//             .Include(x => x.Student)
//             .Include(x => x.Group)
//             .Where(x => x.StudentId == studentId && x.GroupId == groupId && x.WeekIndex == weekIndex && !x.IsDeleted)
//             .ToListAsync();
//             
//         if (exams.Count == 0) 
//             return new Response<List<GetExamDto>>(HttpStatusCode.NotFound, "Exams not found for this student, group and week");
//             
//         var examDtos = exams.Select(x => new GetExamDto
//         {
//             Id = x.Id,
//             Value = x.Value,
//             BonusPoints = x.BonusPoints,
//             Comment = x.Comment,
//             StudentId = x.StudentId,
//             GroupId = x.GroupId,
//             WeekIndex = x.WeekIndex
//         }).ToList();
//         
//         return new Response<List<GetExamDto>>(examDtos);
//     }
// }