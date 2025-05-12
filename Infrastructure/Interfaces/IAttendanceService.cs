using Domain.DTOs.Attendance;
using Domain.Filters;
using Domain.Responses;

namespace Infrastructure.Interfaces;

public interface IAttendanceService
{
    Task<Response<List<GetAttendanceDto>>> GetAttendances();
    Task<Response<GetAttendanceDto>> GetAttendanceById(int id);
    Task<Response<string>> CreateAttendance(AddAttendanceDto addAttendanceDto);
    Task<Response<string>> DeleteAttendanceById(int id);
    Task<Response<string>> EditAttendance(EditAttendanceDto editAttendanceDto);
    Task<Response<List<GetAttendanceDto>>> GetAttendancesByStudent(int studentId);
    Task<Response<List<GetAttendanceDto>>> GetAttendancesByGroup(int groupId);
    Task<Response<List<GetAttendanceDto>>> GetAttendancesByLesson(int lessonId);
}