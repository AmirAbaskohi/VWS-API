using Domain.Domain._base;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using vws.web.Domain._calendar;
using vws.web.Domain._file;
using vws.web.Domain._task;

namespace vws.web.Domain._base
{
    [Table("Base_UserProfile")]
    public class UserProfile
    {
        public UserProfile()
        {
            TaskAssigns = new HashSet<TaskAssign>();
            EventUsers = new HashSet<EventMember>();
            UserWeekends = new HashSet<UserWeekend>();
        }
        [Key]
        public Guid UserId { get; set; }

        public byte? CultureId { get; set; }

        [MaxLength(100)]
        public string NickName{ get; set; }

        public Guid NickNameSecurityStamp { get; set; }

        [MaxLength(6)]
        public string ThemeColorCode { get; set; }

        [ForeignKey("ProfileImage")]
        public int? ProfileImageId { get; set; }

        public Guid? ProfileImageGuid { get; set; }

        public Guid ProfileImageSecurityStamp { get; set; }

        public DateTime CreatedOn { get; set; }

        public DateTime ModifiedOn { get; set; }

        [ForeignKey("FirstWeekDay")]
        public byte FirstWeekDayId { get; set; }

        public bool IsDarkModeOn { get; set; }

        public float ZoomRatio { get; set; }

        public bool IsSecondCalendarOn { get; set; }

        [ForeignKey("FirstCalendarType")]
        public byte FirstCalendarTypeId { get; set; }

        [ForeignKey("SecondCalendarType")]
        public byte? SecondCalendarTypeId { get; set; }

        public virtual Culture Culture { get; set; }

        public virtual FileContainer ProfileImage { get; set; }

        public virtual WeekDay FirstWeekDay { get; set; }

        public virtual CalendarType FirstCalendarType { get; set; }

        public virtual CalendarType SecondCalendarType { get; set; }

        public virtual ICollection<TaskAssign> TaskAssigns { get; set; }

        public virtual ICollection<EventMember> EventUsers { get; set; }

        public virtual ICollection<UserWeekend> UserWeekends { get; set; }
    }
}
