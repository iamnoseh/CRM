using Domain.DTOs.Student;
using Domain.Responses;
using MediatR;

namespace Infrastructure.Features.Students.Queries.GetStudentGroupsOverview;

public record GetStudentGroupsOverviewQuery(int StudentId) : IRequest<Response<List<StudentGroupOverviewDto>>>;
