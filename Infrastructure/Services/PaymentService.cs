using System.Net;
using Domain.DTOs.Payments;
using Domain.Entities;
using Domain.Enums;
using Domain.Responses;
using Infrastructure.Data;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Infrastructure.Helpers;

namespace Infrastructure.Services;

public class PaymentService(DataContext db, IDiscountService discountService, IHttpContextAccessor httpContextAccessor) : IPaymentService
{
    public async Task<Response<GetPaymentDto>> CreateAsync(CreatePaymentDto dto)
    {
        try
        {
            Log.Information("Талаб: эҷоди пардохт | StudentId={StudentId} GroupId={GroupId} {Month}/{Year}", dto.StudentId, dto.GroupId, dto.Month, dto.Year);
            var linkExists = await db.StudentGroups.AnyAsync(sg => sg.StudentId == dto.StudentId && sg.GroupId == dto.GroupId && !sg.IsDeleted);
            if (!linkExists)
            {
                Log.Warning("Донишҷӯ дар ин гурӯҳ узв нест | StudentId={StudentId} GroupId={GroupId}", dto.StudentId, dto.GroupId);
                return new Response<GetPaymentDto>(System.Net.HttpStatusCode.BadRequest, "Донишҷӯ дар ин гурӯҳ узв нест");
            }

            var group = await db.Groups.Include(g => g.Course).FirstOrDefaultAsync(g => g.Id == dto.GroupId && !g.IsDeleted);
            if (group == null || group.Course == null)
            {
                Log.Warning("Гурӯҳ ёфт нашуд | GroupId={GroupId}", dto.GroupId);
                return new Response<GetPaymentDto>(System.Net.HttpStatusCode.NotFound, "Гурӯҳ ёфт нашуд");
            }

            var preview = await discountService.PreviewAsync(dto.StudentId, dto.GroupId, dto.Month, dto.Year);
            if (preview.StatusCode != (int)System.Net.HttpStatusCode.OK)
            {
                Log.Warning("Preview ноком шуд | Status={Status} Message={Message}", preview.StatusCode, preview.Message);
                return new Response<GetPaymentDto>((System.Net.HttpStatusCode)preview.StatusCode, preview.Message);
            }

            var userCenterId = UserContextHelper.GetCurrentUserCenterId(httpContextAccessor);
            var centerId = group.Course.CenterId;
            if (userCenterId.HasValue && userCenterId.Value != centerId)
            {
                Log.Warning("Дастрасӣ рад шуд ба марказ | UserCenterId={UserCenterId} CenterId={CenterId}", userCenterId, centerId);
                return new Response<GetPaymentDto>(System.Net.HttpStatusCode.Forbidden, "Дастрасӣ манъ аст ба ин марказ");
            }

            var payment = new Payment
            {
                StudentId = dto.StudentId,
                GroupId = dto.GroupId,
                OriginalAmount = preview.Data.OriginalAmount,
                DiscountAmount = preview.Data.DiscountAmount,
                Amount = preview.Data.PayableAmount,
                PaymentMethod = dto.PaymentMethod,
                TransactionId = dto.TransactionId,
                Description = dto.Description,
                Status = dto.Status,
                PaymentDate = DateTime.UtcNow,
                CenterId = centerId,
                Month = dto.Month,
                Year = dto.Year,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            db.Payments.Add(payment);
            await db.SaveChangesAsync();
            Log.Information("Пардохт эҷод шуд | PaymentId={PaymentId} StudentId={StudentId} GroupId={GroupId} Original={Original} Discount={Discount} Amount={Amount}", payment.Id, payment.StudentId, payment.GroupId, payment.OriginalAmount, payment.DiscountAmount, payment.Amount);
            return new Response<GetPaymentDto>(Map(payment));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Хатои дохилӣ ҳангоми эҷоди пардохт | StudentId={StudentId} GroupId={GroupId}", dto.StudentId, dto.GroupId);
            return new Response<GetPaymentDto>(System.Net.HttpStatusCode.InternalServerError, $"Хатои дохилӣ: {ex.Message}");
        }
    }

    public async Task<Response<GetPaymentDto>> GetByIdAsync(int id)
    {
        var entity = await db.Payments.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
        if (entity == null)
        {
            Log.Warning("Пардохт ёфт нашуд | Id={Id}", id);
            return new Response<GetPaymentDto>(System.Net.HttpStatusCode.NotFound, "Пардохт ёфт нашуд");
        }
        return new Response<GetPaymentDto>(Map(entity));
    }

    private static GetPaymentDto Map(Payment p) => new()
    {
        Id = p.Id,
        StudentId = p.StudentId,
        GroupId = p.GroupId,
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
    };
}
