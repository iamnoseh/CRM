using System.Net;
using Domain.DTOs.Comment;
using Domain.Entities;
using Domain.Enums;
using Domain.Responses;
using Infrastructure.Data;
using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class CommentService(DataContext context) : ICommentService
{
    #region CreateComment
    public async Task<Response<string>> CreateComment(CreateCommentDto request)
    {
        var student = await context.Students.AnyAsync(x => x.Id == request.StudentId);
        if (!student) return new Response<string>(HttpStatusCode.NotFound, "Student not found");

        var group = await context.Groups.AnyAsync(x => x.Id == request.GroupId);
        if (!group) return new Response<string>(HttpStatusCode.NotFound, "Group not found");
        
        var lesson = await context.Lessons.AnyAsync(x => x.Id == request.LessonId);
        if (!lesson) return new Response<string>(HttpStatusCode.NotFound, "Lesson not found");

        var comment = new Comment
        {
            Text = request.Text,
            StudentId = request.StudentId,
            GroupId = request.GroupId,
            LessonId = request.LessonId,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await context.Comments.AddAsync(comment);
        var result = await context.SaveChangesAsync();

        return result > 0
            ? new Response<string>("Comment created successfully")
            : new Response<string>(HttpStatusCode.InternalServerError, "Failed to create comment");
    }
    
    #endregion
    
    #region UpdateComment
    public async Task<Response<string>> UpdateComment(UpdateCommentDto request)
    {
        var comment = await context.Comments.FirstOrDefaultAsync(x => x.Id == request.CommentId);
        if (comment == null) return new Response<string>(HttpStatusCode.NotFound, "Comment not found");

        comment.Text = request.Text;
        comment.UpdatedAt = DateTimeOffset.UtcNow;

        var result = await context.SaveChangesAsync();

        return result > 0
            ? new Response<string>("Comment updated successfully")
            : new Response<string>(HttpStatusCode.InternalServerError, "Failed to update comment");
    }
    #endregion

    #region DeleteComment
    public async Task<Response<string>> DeleteComment(int id)
    {
        var comment = await context.Comments.FirstOrDefaultAsync(x => x.Id == id);
        if (comment == null) return new Response<string>(HttpStatusCode.NotFound, "Comment not found");

        comment.IsDeleted = true;
        comment.UpdatedAt = DateTimeOffset.UtcNow;

        var result = await context.SaveChangesAsync();

        return result > 0
            ? new Response<string>("Comment deleted successfully")
            : new Response<string>(HttpStatusCode.InternalServerError, "Failed to delete comment");
    }

    public Task<Response<List<GetCommentDto>>> GetComments()
    {
        throw new NotImplementedException();
    }

    public Task<Response<GetCommentDto>> GetCommentById(int id)
    {
        throw new NotImplementedException();
    }

    public Task<Response<List<GetCommentDto>>> GetCommentsByStudent(int studentId)
    {
        throw new NotImplementedException();
    }

    public Task<Response<List<GetCommentDto>>> GetCommentsByGroup(int groupId)
    {
        throw new NotImplementedException();
    }

    public Task<Response<List<GetCommentDto>>> GetCommentsByLesson(int lessonId)
    {
        throw new NotImplementedException();
    }

    public Task<Response<List<GetCommentDto>>> GetCommentsByType(CommentType type)
    {
        throw new NotImplementedException();
    }

    public Task<Response<List<GetCommentDto>>> GetPrivateComments(int authorId)
    {
        throw new NotImplementedException();
    }

    #endregion
    

    
} 