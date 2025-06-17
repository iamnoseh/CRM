using System;
using System.Collections.Generic;
using Domain.DTOs.Grade;
using Domain.DTOs.Exam;
using Domain.Enums;

namespace Domain.DTOs.Student;

public class GetStudentDetailedDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;   
    public string Address { get; set; } = string.Empty;
    public DateTime? Birthday { get; set; }
    public int Age { get; set; }
    public Gender Gender { get; set; }
    public ActiveStatus ActiveStatus { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public string? ImagePath { get; set; }
    public int UserId { get; set; }
    public int CenterId { get; set; }
    
    // Статистика
    public double AverageGrade { get; set; }
    public int GroupsCount { get; set; }
    public List<GroupInfo> Groups { get; set; } = new();
    public List<GetGradeDto> RecentGrades { get; set; } = new ();
    public List<GetExamDto> RecentExams { get; set; } = new();
    
    public class GroupInfo
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public double GroupAverageGrade { get; set; }
    }
} 