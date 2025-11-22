using System.Net;
using Domain.DTOs.Payments;
using Domain.Responses;
using Infrastructure.Data;
using Infrastructure.Helpers;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Features.Students.Queries.GetStudentPayments;

public class GetStudentPaymentsHandler(DataContext context, IHttpContextAccessor httpContextAccessor)
    : IRequestHandler<GetStudentPaymentsQuery, PaginationResponse<List<GetPaymentDto>>>
{
    public async Task<PaginationResponse<List<GetPaymentDto>>> Handle(GetStudentPaymentsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var studentsQuery = context.Students.Where(s => !s.IsDeleted && s.Id == request.StudentId);
            studentsQuery =
                QueryFilterHelper.FilterByCenterIfNotSuperAdmin(studentsQuery, httpContextAccessor, s => s.CenterId);
            var student = await studentsQuery.Select(s => new { s.Id, s.CenterId }).FirstOrDefaultAsync(cancellationToken);
            if (student == null)
                return new PaginationResponse<List<GetPaymentDto>>(HttpStatusCode.NotFound, "Донишҷӯ ёфт нашуд");

            var payments = context.Payments.AsNoTracking().Where(p => !p.IsDeleted && p.StudentId == request.StudentId);
            payments = payments.Where(p => p.CenterId == student.CenterId);
            if (request.Month.HasValue)
                payments = payments.Where(p => p.Month == request.Month.Value);
            if (request.Year.HasValue)
                payments = payments.Where(p => p.Year == request.Year.Value);

            var total = await payments.CountAsync(cancellationToken);
            var list = await payments
                .OrderByDescending(p => p.PaymentDate)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(p => new GetPaymentDto
                {
                    Id = p.Id,
                    StudentId = p.StudentId,
                    GroupId = p.GroupId,
                    ReceiptNumber = p.ReceiptNumber,
                    OriginalAmount = p.OriginalAmount,
                    DiscountAmount = p.DiscountAmount,
                    Amount = p.Amount,
                    PaymentMethod = p.PaymentMethod,
                    TransactionId = p.TransactionId,
                    Description = p.Description,
                    Status = p.Status,
                    PaymentDate = p.PaymentDate,
                    CenterId = p.CenterId,
                    Month = p.Month,
                    Year = p.Year
                })
                .ToListAsync(cancellationToken);

            return new PaginationResponse<List<GetPaymentDto>>(list, total, request.PageNumber, request.PageSize);
        }
        catch (Exception ex)
        {
            return new PaginationResponse<List<GetPaymentDto>>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }
}
