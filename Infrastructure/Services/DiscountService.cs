using System.Net;
using Domain.DTOs.Discounts;
using Domain.Entities;
using Domain.Responses;
using Infrastructure.Data;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Infrastructure.Services;

public class DiscountService(DataContext db, IHttpContextAccessor httpContextAccessor) : IDiscountService
{
    public async Task<Response<string>> AssignDiscountAsync(CreateStudentGroupDiscountDto dto)
    {
        try
        {
            Log.Information("Талаб: таъини тахфиф | StudentId={StudentId} GroupId={GroupId} Discount={Discount}", dto.StudentId, dto.GroupId, dto.DiscountAmount);
            if (dto.DiscountAmount < 0)
            {
                Log.Warning("Миқдори тахфиф нодуруст аст (манфӣ) | Discount={Discount}", dto.DiscountAmount);
                return new Response<string>(HttpStatusCode.BadRequest, "Миқдори тахфиф бояд аз 0 бузург ё баробар бошад");
            }

            var existsLink = await db.StudentGroups.AnyAsync(sg => sg.StudentId == dto.StudentId && sg.GroupId == dto.GroupId && !sg.IsDeleted);
            if (!existsLink)
            {
                Log.Warning("Донишҷӯ дар ин гурӯҳ узв нест | StudentId={StudentId} GroupId={GroupId}", dto.StudentId, dto.GroupId);
                return new Response<string>(HttpStatusCode.BadRequest, "Донишҷӯ дар ин гурӯҳ узв нест");
            }

            var existingActive = await db.StudentGroupDiscounts.FirstOrDefaultAsync(x => x.StudentId == dto.StudentId && x.GroupId == dto.GroupId && !x.IsDeleted);
            if (existingActive != null)
            {
                existingActive.DiscountAmount = dto.DiscountAmount;
                existingActive.UpdatedAt = DateTimeOffset.UtcNow;
                db.StudentGroupDiscounts.Update(existingActive);
                await db.SaveChangesAsync();
                Log.Information("Тахфиф навсозӣ шуд | DiscountId={Id} Discount={Discount}", existingActive.Id, existingActive.DiscountAmount);
                return new Response<string>(HttpStatusCode.OK, "Тахфиф навсозӣ шуд");
            }

            var entity = new StudentGroupDiscount
            {
                StudentId = dto.StudentId,
                GroupId = dto.GroupId,
                DiscountAmount = dto.DiscountAmount,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await db.StudentGroupDiscounts.AddAsync(entity);
            await db.SaveChangesAsync();
            Log.Information("Тахфиф таъин карда шуд | DiscountId={Id} StudentId={StudentId} GroupId={GroupId} Discount={Discount}", entity.Id, entity.StudentId, entity.GroupId, entity.DiscountAmount);
            return new Response<string>(HttpStatusCode.Created, "Тахфиф таъин карда шуд");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Хатои дохилӣ ҳангоми таъини тахфиф | StudentId={StudentId} GroupId={GroupId}", dto.StudentId, dto.GroupId);
            return new Response<string>(HttpStatusCode.InternalServerError, $"Хатои дохилӣ: {ex.Message}");
        }
    }

    public async Task<Response<string>> UpdateDiscountAsync(UpdateStudentGroupDiscountDto dto)
    {
        try
        {
            var entity = await db.StudentGroupDiscounts.FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted);
            if (entity == null)
            {
                Log.Warning("Тахфиф ёфт нашуд | Id={Id}", dto.Id);
                return new Response<string>(HttpStatusCode.NotFound, "Тахфиф ёфт нашуд");
            }
            if (dto.DiscountAmount.HasValue)
            {
                if (dto.DiscountAmount.Value < 0)
                {
                    Log.Warning("Миқдори тахфиф нодуруст аст (манфӣ) | Id={Id} Discount={Discount}", dto.Id, dto.DiscountAmount.Value);
                    return new Response<string>(HttpStatusCode.BadRequest, "Миқдори тахфиф бояд аз 0 бузург ё баробар бошад");
                }
                entity.DiscountAmount = dto.DiscountAmount.Value;
            }
            entity.UpdatedAt = DateTimeOffset.UtcNow;
            db.StudentGroupDiscounts.Update(entity);
            await db.SaveChangesAsync();
            Log.Information("Тахфиф навсозӣ шуд | Id={Id} Discount={Discount}", entity.Id, entity.DiscountAmount);
            return new Response<string>(HttpStatusCode.OK, "Тахфиф навсозӣ шуд");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Хатои дохилӣ ҳангоми навсозии тахфиф | Id={Id}", dto.Id);
            return new Response<string>(HttpStatusCode.InternalServerError, $"Хатои дохилӣ: {ex.Message}");
        }
    }

    public async Task<Response<string>> RemoveDiscountAsync(int id)
    {
        var entity = await db.StudentGroupDiscounts.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (entity == null)
        {
            Log.Warning("Тахфиф ёфт нашуд | Id={Id}", id);
            return new Response<string>(HttpStatusCode.NotFound, "Тахфиф ёфт нашуд");
        }
        entity.IsDeleted = true;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        db.StudentGroupDiscounts.Update(entity);
        await db.SaveChangesAsync();
        Log.Information("Тахфиф нест карда шуд | Id={Id}", id);
        return new Response<string>(HttpStatusCode.OK, "Тахфиф нест карда шуд");
    }

    public async Task<Response<GetStudentGroupDiscountDto>> GetDiscountByIdAsync(int id)
    {
        var entity = await db.StudentGroupDiscounts.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (entity == null)
        {
            Log.Warning("Тахфиф ёфт нашуд | Id={Id}", id);
            return new Response<GetStudentGroupDiscountDto>(HttpStatusCode.NotFound, "Тахфиф ёфт нашуд");
        }
        Log.Information("Тахфиф ёфт шуд | Id={Id} StudentId={StudentId} GroupId={GroupId}", entity.Id, entity.StudentId, entity.GroupId);
        return new Response<GetStudentGroupDiscountDto>(new GetStudentGroupDiscountDto
        {
            Id = entity.Id,
            StudentId = entity.StudentId,
            GroupId = entity.GroupId,
            DiscountAmount = entity.DiscountAmount,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        });
    }

    public async Task<Response<List<GetStudentGroupDiscountDto>>> GetDiscountsByStudentGroupAsync(int studentId, int groupId)
    {
        var list = await db.StudentGroupDiscounts
            .Where(x => x.StudentId == studentId && x.GroupId == groupId && !x.IsDeleted)
            .OrderByDescending(x => x.UpdatedAt)
            .Select(x => new GetStudentGroupDiscountDto
            {
                Id = x.Id,
                StudentId = x.StudentId,
                GroupId = x.GroupId,
                DiscountAmount = x.DiscountAmount,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync();
        Log.Information("Рӯйхати тахфифҳо баргардонда шуд | StudentId={StudentId} GroupId={GroupId} Count={Count}", studentId, groupId, list.Count);
        return new Response<List<GetStudentGroupDiscountDto>>(list);
    }

    public async Task<Response<DiscountPreviewDto>> PreviewAsync(int studentId, int groupId, int month, int year)
    {
        // OriginalAmount from course price
        var group = await db.Groups.Include(g => g.Course).FirstOrDefaultAsync(g => g.Id == groupId && !g.IsDeleted);
        if (group == null || group.Course == null)
        {
            Log.Warning("Гурӯҳ ёфт нашуд | GroupId={GroupId}", groupId);
            return new Response<DiscountPreviewDto>(HttpStatusCode.NotFound, "Гурӯҳ ёфт нашуд");
        }
        var original = group.Course.Price;
        var discount = await db.StudentGroupDiscounts
            .Where(x => x.StudentId == studentId && x.GroupId == groupId && !x.IsDeleted)
            .OrderByDescending(x => x.UpdatedAt)
            .Select(x => x.DiscountAmount)
            .FirstOrDefaultAsync();
        var applied = Math.Min(original, discount);
        var net = original - applied;
        Log.Information("Preview ҳисоб шуд | StudentId={StudentId} GroupId={GroupId} Original={Original} Discount={Discount} Net={Net}", studentId, groupId, original, applied, net);
        return new Response<DiscountPreviewDto>(new DiscountPreviewDto
        {
            OriginalAmount = original,
            DiscountAmount = applied,
            PayableAmount = net
        });
    }
}
