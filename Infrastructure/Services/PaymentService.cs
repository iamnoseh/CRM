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
using System.Net.Mime;

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

			// Closed month check
			var financeService = new FinanceService(db, httpContextAccessor);
			if (await financeService.IsMonthClosedAsync(centerId, dto.Year, dto.Month))
			{
				return new Response<GetPaymentDto>(System.Net.HttpStatusCode.BadRequest, "Ин моҳ баста шудааст, эҷоди пардохт манъ аст");
			}

            var monthsCount = dto.MonthsCount ?? 1;

            if (monthsCount == 1)
            {
                var amountToCharge = dto.Amount ?? preview.Data.PayableAmount;
                if (amountToCharge <= 0)
                {
                    return new Response<GetPaymentDto>(System.Net.HttpStatusCode.BadRequest, "Маблағи пардохт бояд > 0 бошад");
                }
                if (amountToCharge > preview.Data.PayableAmount)
                {
                    return new Response<GetPaymentDto>(System.Net.HttpStatusCode.BadRequest, "Маблағи пардохт набояд аз маблағи пардохтанӣ бештар бошад");
                }

                var payment = new Payment
                {
                    StudentId = dto.StudentId,
                    GroupId = dto.GroupId,
                    OriginalAmount = preview.Data.OriginalAmount,
                    DiscountAmount = preview.Data.DiscountAmount,
                    Amount = amountToCharge,
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
                payment.ReceiptNumber = await DocumentNumberGenerator.GenerateReceiptNumberAsync(db, centerId, dto.Year, dto.Month);

                db.Payments.Add(payment);
                await db.SaveChangesAsync();
                Log.Information("Пардохт эҷод шуд | PaymentId={PaymentId} StudentId={StudentId} GroupId={GroupId} Original={Original} Discount={Discount} Amount={Amount}", payment.Id, payment.StudentId, payment.GroupId, payment.OriginalAmount, payment.DiscountAmount, payment.Amount);
                return new Response<GetPaymentDto>(Map(payment));
            }
            else
            {
                if (dto.Amount.HasValue)
                {
                    return new Response<GetPaymentDto>(System.Net.HttpStatusCode.BadRequest, "Барои пардохти чандмоҳа, `amount`-ро холӣ монед (истифодаи пурра барои ҳар моҳ)");
                }

                var created = new List<Payment>();
                var startMonth = dto.Month;
                var startYear = dto.Year;
                for (var i = 0; i < monthsCount; i++)
                {
                    var targetDate = new DateTime(startYear, startMonth, 1).AddMonths(i);
                    var tYear = targetDate.Year;
                    var tMonth = targetDate.Month;

                    // Check closed month per period (reuse existing service)
                    if (await financeService.IsMonthClosedAsync(centerId, tYear, tMonth))
                    {
                        return new Response<GetPaymentDto>(System.Net.HttpStatusCode.BadRequest, $"Моҳ {tMonth:00}.{tYear} баста мебошад, пардохти чандмоҳа қатъ шуд");
                    }

                    var monthPreview = await discountService.PreviewAsync(dto.StudentId, dto.GroupId, tMonth, tYear);
                    if (monthPreview.StatusCode != (int)System.Net.HttpStatusCode.OK)
                    {
                        return new Response<GetPaymentDto>((System.Net.HttpStatusCode)monthPreview.StatusCode, monthPreview.Message);
                    }

                    var payment = new Payment
                    {
                        StudentId = dto.StudentId,
                        GroupId = dto.GroupId,
                        OriginalAmount = monthPreview.Data.OriginalAmount,
                        DiscountAmount = monthPreview.Data.DiscountAmount,
                        Amount = monthPreview.Data.PayableAmount,
                        PaymentMethod = dto.PaymentMethod,
                        TransactionId = dto.TransactionId,
                        Description = string.IsNullOrWhiteSpace(dto.Description) ? $"Payment for {tMonth:00}.{tYear}" : dto.Description,
                        Status = dto.Status,
                        PaymentDate = DateTime.UtcNow,
                        CenterId = centerId,
                        Month = tMonth,
                        Year = tYear,
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    };
                    payment.ReceiptNumber = await DocumentNumberGenerator.GenerateReceiptNumberAsync(db, centerId, tYear, tMonth);
                    created.Add(payment);
                }

                await db.Payments.AddRangeAsync(created);
                await db.SaveChangesAsync();
                Log.Information("Пардохти чандмоҳа: {Count} сабт тавлид шуд барои StudentId={StudentId} GroupId={GroupId}", created.Count, dto.StudentId, dto.GroupId);
                return new Response<GetPaymentDto>(Map(created.Last()));
            }
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
	};


    public async Task<Response<bool>> RefundAsync(int id, decimal amount, string? reason)
    {
        try
        {
            if (amount <= 0)
                return new Response<bool>(System.Net.HttpStatusCode.BadRequest, "Маблағи refund бояд > 0 бошад");

            var payment = await db.Payments.Include(p => p.Group).ThenInclude(g => g.Course)
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
            if (payment == null)
                return new Response<bool>(System.Net.HttpStatusCode.NotFound, "Пардохт ёфт нашуд");

            var centerId = payment.CenterId ?? payment.Group?.Course?.CenterId;
            if (centerId == null)
                return new Response<bool>(System.Net.HttpStatusCode.BadRequest, "Маркази пардохт муайян нашуд");

            var userCenterId = UserContextHelper.GetCurrentUserCenterId(httpContextAccessor);
            if (userCenterId.HasValue && userCenterId.Value != centerId.Value)
                return new Response<bool>(System.Net.HttpStatusCode.Forbidden, "Дастрасӣ манъ аст ба ин марказ");

            // Refund дар моҳ/соли ҷорӣ ҳамчун Expense: Refund
            var now = DateTimeOffset.UtcNow;
            var financeService = new FinanceService(db, httpContextAccessor);
            if (await financeService.IsMonthClosedAsync(centerId.Value, now.Year, now.Month))
                return new Response<bool>(System.Net.HttpStatusCode.BadRequest, "Ин моҳ баста шудааст, refund манъ аст");

            if (amount > payment.Amount)
                return new Response<bool>(System.Net.HttpStatusCode.BadRequest, "Refund аз маблағи пардохт зиёд аст");

            var expense = new Expense
            {
                CenterId = centerId.Value,
                Amount = amount,
                ExpenseDate = now,
                Category = ExpenseCategory.Refund,
                PaymentMethod = payment.PaymentMethod,
                Description = string.IsNullOrWhiteSpace(reason) ? $"Refund for payment #{payment.Id}" : reason,
                MentorId = null,
                Month = now.Month,
                Year = now.Year,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            db.Expenses.Add(expense);
            await db.SaveChangesAsync();
            return new Response<bool>(true);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Хатогӣ ҳангоми refund пардохт {PaymentId}", id);
            return new Response<bool>(System.Net.HttpStatusCode.InternalServerError, "Refund ноком шуд");
        }
    }
}
