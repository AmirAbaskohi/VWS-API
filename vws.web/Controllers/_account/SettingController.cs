using Domain.Domain._base;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using vws.web.Domain;
using vws.web.Enums;
using vws.web.Models;
using vws.web.Models._account;

namespace vws.web.Controllers._account
{
    [Route("{culture:culture}/[controller]")]
    [ApiController]
    public class SettingController : BaseController
    {
        #region Feilds
        private readonly IVWS_DbContext _vwsDbContext;
        private readonly IStringLocalizer<SettingController> _localizer;
        #endregion

        #region Ctor
        public SettingController(IVWS_DbContext vwsDbContext, IStringLocalizer<SettingController> localizer)
        {
            _vwsDbContext = vwsDbContext;
            _localizer = localizer;
        }

        #endregion

        #region SettingsAPI

        [HttpPut]
        [Authorize]
        [Route("toggleDarkMode")]
        public IActionResult ToggleDartMode()
        {
            var user = _vwsDbContext.UserProfiles.FirstOrDefault(profile => profile.UserId == LoggedInUserId.Value);
            var response = new ResponseModel<bool>();

            user.IsDarkModeOn = !user.IsDarkModeOn;
            _vwsDbContext.Save();

            response.Message = "Dark mode toggled.";
            response.Value = user.IsDarkModeOn;
            return Ok(response);
        }

        [HttpPut]
        [Authorize]
        [Route("toggleSecondCalendar")]
        public IActionResult ToggleSecondCalendar()
        {
            var user = _vwsDbContext.UserProfiles.FirstOrDefault(profile => profile.UserId == LoggedInUserId.Value);
            var response = new ResponseModel<bool>();

            user.IsSecondCalendarOn = !user.IsSecondCalendarOn;
            _vwsDbContext.Save();

            response.Message = "Second calender toggled.";
            response.Value = user.IsSecondCalendarOn;
            return Ok(response);
        }

        [HttpPut]
        [Authorize]
        [Route("setFirstWeekDay")]
        public IActionResult SetFirstWeekDay(byte day)
        {
            var response = new ResponseModel();

            if (day < 1 || day > 7)
            {
                response.Message = "Invalid day";
                response.AddError(_localizer["Invalid day."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            var user = _vwsDbContext.UserProfiles.Include(profile => profile.UserWeekends)
                                                 .FirstOrDefault(profile => profile.UserId == LoggedInUserId.Value);

            var weekends = user.UserWeekends.Select(day => day.WeekDayId).ToList();

            if (weekends.Contains(day))
            {
                response.Message = "Invalid first weekday";
                response.AddError(_localizer["Invalid first weekday."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            user.FirstWeekDayId = day;
            _vwsDbContext.Save();

            response.Message = "First weekday is set.";
            return Ok(response);
        }

        [HttpPut]
        [Authorize]
        [Route("setFirstCalander")]
        public IActionResult SetFirstCalander(byte calendar)
        {
            var response = new ResponseModel();

            if (Enum.IsDefined(typeof(SeedDataEnum.CalendarType), calendar))
            {
                response.Message = "Invalid calendar";
                response.AddError(_localizer["Invalid calendar."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            var user = _vwsDbContext.UserProfiles.FirstOrDefault(profile => profile.UserId == LoggedInUserId.Value);

            user.FirstCalendarTypeId = calendar;
            _vwsDbContext.Save();

            response.Message = "First calendar is set.";
            return Ok(response);
        }

        [HttpPut]
        [Authorize]
        [Route("setSecondCalander")]
        public IActionResult SetSecondCalendar(byte? calendar)
        {
            var response = new ResponseModel();

            if (calendar.HasValue && Enum.IsDefined(typeof(SeedDataEnum.CalendarType), calendar))
            {
                response.Message = "Invalid calendar";
                response.AddError(_localizer["Invalid calendar."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            var user = _vwsDbContext.UserProfiles.FirstOrDefault(profile => profile.UserId == LoggedInUserId.Value);

            user.SecondCalendarTypeId = calendar;
            _vwsDbContext.Save();

            response.Message = "Second calendar is set.";
            return Ok(response);
        }

        [HttpPut]
        [Authorize]
        [Route("updateWeekends")]
        public IActionResult UpdateWeekends([FromBody] List<byte> weekends)
        {
            var response = new ResponseModel();
            weekends = weekends.Distinct().ToList();

            var user = _vwsDbContext.UserProfiles.Include(profile => profile.UserWeekends).FirstOrDefault(profile => profile.UserId == LoggedInUserId.Value);
            if (weekends.Any(weekend => weekend > 7 || weekend < 1 || weekend == user.FirstWeekDayId))
            {
                response.Message = "Invalid day";
                response.AddError(_localizer["Invalid day."]);
                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            var userWeekends = user.UserWeekends.Select(userWeekend => userWeekend.WeekDayId);
            var shouldBeRemoved = userWeekends.Except(weekends).ToList();
            var shouldBeAdded = weekends.Except(userWeekends).ToList();

            foreach (var weekend in shouldBeRemoved)
                _vwsDbContext.DeleteUserWeekend(weekend, LoggedInUserId.Value);
            _vwsDbContext.Save();

            foreach (var weekend in shouldBeAdded)
                _vwsDbContext.AddUserWeekend(new UserWeekend { WeekDayId = weekend, UserProfileId = LoggedInUserId.Value });
            _vwsDbContext.Save();

            response.Message = "User weekends updated.";
            return Ok(response);
        }

        [HttpGet]
        [Authorize]
        [Route("getSettings")]
        public SettingsModel GetSettings()
        {
            var user = _vwsDbContext.UserProfiles.Include(profile => profile.UserWeekends).FirstOrDefault(profile => profile.UserId == LoggedInUserId.Value);

            return new SettingsModel()
            {
                FirstCalendar = user.FirstCalendarTypeId,
                SecondCalendar = user.SecondCalendarTypeId,
                IsSeondCalendarOn = user.IsSecondCalendarOn,
                IsDarkModeOn = user.IsDarkModeOn,
                FirstWeekDay = user.FirstWeekDayId,
                Weekends = user.UserWeekends.Select(userWeekend => userWeekend.WeekDayId).ToList()
            };
        }

        #endregion
    }
}
