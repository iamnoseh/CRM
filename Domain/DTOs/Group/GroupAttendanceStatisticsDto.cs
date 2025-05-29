using System;
using System.Collections.Generic;
using Domain.DTOs.Attendance;

namespace Domain.DTOs.Group
{    public class GroupAttendanceStatisticsDto
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public int TotalStudents { get; set; }
        public int CurrentWeek { get; set; }
        
        // Статистика посещаемости по неделям
        public Dictionary<int, WeekAttendanceStatistics> WeeklyAttendance { get; set; } = new Dictionary<int, WeekAttendanceStatistics>();
        
        // Общая статистика
        public double OverallAttendancePercentage { get; set; }
        public int TotalPresentCount { get; set; }
        public int TotalAbsentCount { get; set; }
        public int TotalLateCount { get; set; }
        
        // Последние записи о посещаемости
        public List<GetAttendanceDto> RecentAttendances { get; set; } = new List<GetAttendanceDto>();
        
        public class WeekAttendanceStatistics
        {
            public int WeekNumber { get; set; }
            public int PresentCount { get; set; }
            public int AbsentCount { get; set; }
            public int LateCount { get; set; }
            public double AttendancePercentage { get; set; }
        }
    }
} 