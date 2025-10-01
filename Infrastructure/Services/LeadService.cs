﻿using System.Net;
using Domain.DTOs.Lead;
using Domain.Entities;
using Domain.Filters;
using Domain.Responses;
using Infrastructure.Data;
using Infrastructure.Helpers;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class LeadService(
    DataContext context,
    IHttpContextAccessor httpContextAccessor) : ILeadService
{
    public async Task<Response<string>> CreateLead(CreateLeadDto request)
    {
        try
        {
            var centerId = UserContextHelper.GetCurrentUserCenterId(httpContextAccessor);
            if (centerId == null)
                return new Response<string>(HttpStatusCode.BadRequest, "ID-и марказ дар токен ёфт нашуд");
            

            var lead = new Lead
            {
                FullName = request.FullName,
                PhoneNumber = request.PhoneNumber,
                BirthDate = request.BirthDate,
                Gender = request.Gender,
                OccupationStatus = request.OccupationStatus,
                RegisterForMonth = request.RegisterForMonth,
                Course = request.Course ?? string.Empty,
                LessonTime = request.LessonTime,
                Notes = request.Notes ?? string.Empty,
                UtmSource = request.UtmSource ?? string.Empty,
                CenterId = centerId.Value,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                IsDeleted = false
            };

            context.Leads.Add(lead);
            await context.SaveChangesAsync();

            return new Response<string>(HttpStatusCode.Created, "Лид бомуваффақият эҷод шуд");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, $"Хатогии эҷоди лид: {ex.Message}");
        }
    }

   
    public async Task<Response<string>> UpdateLead(UpdateLeadDto request)
    {
        try
        {
            var lead = await context.Leads
                .FirstOrDefaultAsync(l => l.Id == request.Id && !l.IsDeleted);

            if (lead == null)
                return new Response<string>(HttpStatusCode.NotFound, "Лид ёфт нашуд");
            

            lead.FullName = request.FullName ?? lead.FullName;
            lead.PhoneNumber = request.PhoneNumber ?? lead.PhoneNumber;
            lead.BirthDate = request.BirthDate != default ? request.BirthDate : lead.BirthDate;
            lead.Gender = request.Gender != default ? request.Gender : lead.Gender;
            lead.OccupationStatus = request.OccupationStatus != default ? request.OccupationStatus : lead.OccupationStatus;
            lead.RegisterForMonth = request.RegisterForMonth ?? lead.RegisterForMonth;
            lead.Course = request.Course ?? lead.Course;
            lead.LessonTime = request.LessonTime != default ? request.LessonTime : lead.LessonTime;
            lead.Notes = request.Notes ?? lead.Notes;
            lead.UpdatedAt = DateTimeOffset.UtcNow;

            await context.SaveChangesAsync();

            return new Response<string>(HttpStatusCode.OK, "Лид бомуваффақият навсозӣ шуд");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, $"Хатогии навсозии лид: {ex.Message}");
        }
    }

    public async Task<Response<string>> DeleteLead(int id)
    {
        try
        {
            var lead = await context.Leads
                .FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted);

            if (lead == null)
                return new Response<string>(HttpStatusCode.NotFound, "Лид ёфт нашуд");

            lead.IsDeleted = true;
            lead.UpdatedAt = DateTimeOffset.UtcNow;

            await context.SaveChangesAsync();

            return new Response<string>(HttpStatusCode.OK, "Лид бомуваффақият нест карда шуд");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, $"Хатогии нест кардани лид: {ex.Message}");
        }
    }
    
    public async Task<PaginationResponse<List<GetLeadDto>>> GetLeads(LeadFilter filter)
    {
        try
        {
            var centerId = UserContextHelper.GetCurrentUserCenterId(httpContextAccessor);
            if (centerId == null)
                return new PaginationResponse<List<GetLeadDto>>(HttpStatusCode.BadRequest,
                    "ID-и марказ дар токен ёфт нашуд");

            var leadsQuery = context.Leads
                .Include(l => l.Center)
                .Where(l => !l.IsDeleted && l.CenterId == centerId.Value);

            if (!string.IsNullOrEmpty(filter.FullName))
                leadsQuery = leadsQuery.Where(l => EF.Functions.ILike(l.FullName, $"%{filter.FullName}%"));

            if (!string.IsNullOrEmpty(filter.PhoneNumber))
                leadsQuery = leadsQuery.Where(l => EF.Functions.ILike(l.PhoneNumber, $"%{filter.PhoneNumber}%"));

            if (filter.Gender.HasValue)
                leadsQuery = leadsQuery.Where(l => l.Gender == filter.Gender.Value);

            if (filter.OccupationStatus.HasValue)
                leadsQuery = leadsQuery.Where(l => l.OccupationStatus == filter.OccupationStatus.Value);

            if (filter.RegisterForMonth.HasValue)
                leadsQuery = leadsQuery.Where(l => l.RegisterForMonth.HasValue && 
                                       l.RegisterForMonth.Value.Month == filter.RegisterForMonth.Value.Month &&
                                       l.RegisterForMonth.Value.Year == filter.RegisterForMonth.Value.Year);

            if (!string.IsNullOrEmpty(filter.Course))
                leadsQuery = leadsQuery.Where(l => EF.Functions.ILike(l.Course, $"%{filter.Course}%"));

            if (!string.IsNullOrEmpty(filter.UtmSource))
                leadsQuery = leadsQuery.Where(l => EF.Functions.ILike(l.UtmSource, $"%{filter.UtmSource}%"));

            if (filter.StartDate.HasValue)
                leadsQuery = leadsQuery.Where(l => l.CreatedAt >= filter.StartDate.Value);

            if (filter.EndDate.HasValue)
                leadsQuery = leadsQuery.Where(l => l.CreatedAt <= filter.EndDate.Value);

            var totalRecords = await leadsQuery.CountAsync();
            var skip = (filter.PageNumber - 1) * filter.PageSize;

            var leads = await leadsQuery
                .OrderByDescending(l => l.CreatedAt)
                .Skip(skip)
                .Take(filter.PageSize)
                .Select(l => new GetLeadDto
                {
                    Id = l.Id,
                    FullName = l.FullName,
                    PhoneNumber = l.PhoneNumber,
                    BirthDate = l.BirthDate,
                    Gender = l.Gender,
                    OccupationStatus = l.OccupationStatus,
                    RegisterForMonth = l.RegisterForMonth,
                    Course = l.Course,
                    LessonTime = l.LessonTime,
                    Notes = l.Notes,
                    UtmSource = l.UtmSource,
                    CenterId = l.CenterId,
                    CenterName = l.Center != null ? l.Center.Name : string.Empty,
                    CreatedAt = l.CreatedAt,
                    UpdatedAt = l.UpdatedAt
                })
                .ToListAsync();

            if (leads.Count == 0)
                return new PaginationResponse<List<GetLeadDto>>(HttpStatusCode.NotFound, "Лидҳо ёфт нашуданд");

            return new PaginationResponse<List<GetLeadDto>>(
                leads,
                totalRecords,
                filter.PageNumber,
                filter.PageSize
            );
        }
        catch (Exception ex)
        {
            return new PaginationResponse<List<GetLeadDto>>(HttpStatusCode.InternalServerError, $"Хатогии гирифтани лидҳо: {ex.Message}");
        }
    }
    
    public async Task<Response<GetLeadDto>> GetLead(int id)
    {
        try
        {
            var centerId = UserContextHelper.GetCurrentUserCenterId(httpContextAccessor);
            if (centerId == null)
                return new Response<GetLeadDto>(HttpStatusCode.BadRequest, "ID-и марказ дар токен ёфт нашуд");

            var lead = await context.Leads
                .Include(l => l.Center)
                .Where(l => l.Id == id && !l.IsDeleted && l.CenterId == centerId.Value)
                .Select(l => new GetLeadDto
                {
                    Id = l.Id,
                    FullName = l.FullName,
                    PhoneNumber = l.PhoneNumber,
                    BirthDate = l.BirthDate,
                    Gender = l.Gender,
                    OccupationStatus = l.OccupationStatus,
                    RegisterForMonth = l.RegisterForMonth,
                    Course = l.Course,
                    LessonTime = l.LessonTime,
                    Notes = l.Notes,
                    UtmSource = l.UtmSource,
                    CenterId = l.CenterId,
                    CenterName = l.Center != null ? l.Center.Name : string.Empty,
                    CreatedAt = l.CreatedAt,
                    UpdatedAt = l.UpdatedAt
                })
                .FirstOrDefaultAsync();

            if (lead == null)
                return new Response<GetLeadDto>(HttpStatusCode.NotFound, "Лид ёфт нашуд");

            return new Response<GetLeadDto>(lead);
        }
        catch (Exception ex)
        {
            return new Response<GetLeadDto>(HttpStatusCode.InternalServerError, $"Хатогии гирифтани лид: {ex.Message}");
        }
    }


}