using Domain.DTOs.Payments;
using Domain.Responses;
using MediatR;

namespace Infrastructure.Features.Students.Queries.GetStudentPayments;

public record GetStudentPaymentsQuery(int StudentId, int? Month, int? Year, int PageNumber, int PageSize) 
    : IRequest<PaginationResponse<List<GetPaymentDto>>>;
