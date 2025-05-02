using Domain.DTOs.Attendance;
using Domain.Responses;
using Infrastructure.Data;
using Infrastructure.Interfaces;

namespace Infrastructure.Services;

public class AttendanceService(DataContext context) : IAttendanceService
{
    public Task<Response<List<GetAttendanceDto>>> GetAttendances()
    {
        throw new NotImplementedException();
    }

    public Task<Response<GetAttendanceDto>> GetAttendanceById(int id)
    {
        throw new NotImplementedException();
    }

    public Task<Response<string>> CreateAttendance(AddAttendanceDto addAttendanceDto)
    {
        throw new NotImplementedException();
    }

    public Task<Response<string>> DeleteAttendanceById(int id)
    {
        throw new NotImplementedException();
    }

    public Task<Response<string>> EditAttendance(EditAttendanceDto editAttendanceDto)
    {
        throw new NotImplementedException();
    }

    public Task<Response<List<GetAttendanceDto>>> GetAttendancesByStudent(int studentId)
    {
        throw new NotImplementedException();
    }

    public Task<Response<List<GetAttendanceDto>>> GetAttendancesByGroup(int groupId)
    {
        throw new NotImplementedException();
    }

    public Task<Response<List<GetAttendanceDto>>> GetAttendancesByLesson(int lessonId)
    {
        throw new NotImplementedException();
    }

    public Task<Response<double>> GetStudentAttendanceRate(int studentId, int? groupId = null)
    {
        throw new NotImplementedException();
    }
}