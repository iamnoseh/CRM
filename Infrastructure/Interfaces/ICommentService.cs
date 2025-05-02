using Domain.DTOs.Comment;
using Domain.Enums;
using Domain.Filters;
using Domain.Responses;

namespace Infrastructure.Interfaces;

public interface ICommentService
{
    Task<Response<string>> CreateComment(CreateCommentDto request);
    Task<Response<string>> UpdateComment(UpdateCommentDto request);
    Task<Response<string>> DeleteComment(int id);
    Task<Response<List<GetCommentDto>>> GetComments();
    Task<Response<GetCommentDto>> GetCommentById(int id);
    Task<Response<List<GetCommentDto>>> GetCommentsByStudent(int studentId);
    Task<Response<List<GetCommentDto>>> GetCommentsByGroup(int groupId);
    Task<Response<List<GetCommentDto>>> GetCommentsByLesson(int lessonId);
    Task<Response<List<GetCommentDto>>> GetCommentsByType(CommentType type);
    Task<Response<List<GetCommentDto>>> GetPrivateComments(int authorId);
    
}