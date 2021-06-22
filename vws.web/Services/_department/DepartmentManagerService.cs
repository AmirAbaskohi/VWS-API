using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using vws.web.Domain;
using vws.web.Domain._base;
using vws.web.Domain._department;
using vws.web.Models;
using vws.web.Models._department;

namespace vws.web.Services._department
{
    public class DepartmentManagerService : IDepartmentManagerService
    {
        private readonly IVWS_DbContext _vwsDbContext;

        public DepartmentManagerService(IVWS_DbContext vwsDbContext)
        {
            _vwsDbContext = vwsDbContext;
        }

        public async Task<Department> CreateDepartment(DepartmentModel model, Guid userId)
        {
            var creationTime = DateTime.UtcNow;
            var newDepartment = new Department()
            {
                Name = model.Name,
                Description = model.Description,
                IsDeleted = false,
                Color = model.Color,
                TeamId = model.TeamId,
                Guid = Guid.NewGuid(),
                CreatedBy = userId,
                ModifiedBy = userId,
                CreatedOn = creationTime,
                ModifiedOn = creationTime
            };
            await _vwsDbContext.AddDepartmentAsync(newDepartment);
            _vwsDbContext.Save();

            model.Users.Add(userId);
            model.Users = model.Users.Distinct().ToList();

            foreach (var user in model.Users)
            {
                await _vwsDbContext.AddDepartmentMemberAsync(new DepartmentMember()
                {
                    CreatedOn = creationTime,
                    IsDeleted = false,
                    DepartmentId = newDepartment.Id,
                    UserProfileId = user
                });
            }
            _vwsDbContext.Save();

            return newDepartment;
        }

        public List<string> CheckDepartmentModel(DepartmentModel model)
        {
            var result = new List<string>();

            #region CheckModel
            if (!String.IsNullOrEmpty(model.Description) && model.Description.Length > 2000)
                result.Add("Length of description is more than 2000 characters.");

            if (model.Name.Length > 500)
                result.Add("Length of name is more than 500 characters.");

            if (!String.IsNullOrEmpty(model.Color) && model.Color.Length > 6)
                result.Add("Length of color is more than 6 characters.");
            #endregion

            #region CheckUsers
            foreach (var user in model.Users)
                if (!_vwsDbContext.TeamMembers.Any(teamMember => teamMember.UserProfileId == user && teamMember.TeamId == model.TeamId && !teamMember.IsDeleted))
                    result.Add("Invalid users to add to department.");
            #endregion

            return result;
        }

        public async Task AddUserToDepartment(Guid user, int departmentId)
        {
            await _vwsDbContext.AddDepartmentMemberAsync(new DepartmentMember()
            {
                CreatedOn = DateTime.UtcNow,
                IsDeleted = false,
                DepartmentId = departmentId,
                UserProfileId = user
            });
            _vwsDbContext.Save();
        }

        public async Task<List<UserModel>> GetDepartmentMembers(int departmentId)
        {
            var result = new List<UserModel>();

            var members = _vwsDbContext.DepartmentMembers.Where(member => member.DepartmentId == departmentId && !member.IsDeleted)
                                                        .Select(member => member.UserProfileId);

            foreach (var member in members)
            {
                UserProfile userProfile = await _vwsDbContext.GetUserProfileAsync(member);
                result.Add(new UserModel()
                {
                    UserId = member,
                    NickName = userProfile.NickName,
                    ProfileImageGuid = userProfile.ProfileImageGuid
                });
            }

            return result;
        }

        public List<Department> GetAllUserDepartments(Guid userId)
        {
            return _vwsDbContext.DepartmentMembers.Include(departmentMember => departmentMember.Department)
                                                 .Where(departmentMember => !departmentMember.IsDeleted &&
                                                                            departmentMember.UserProfileId == userId &&
                                                                            !departmentMember.Department.IsDeleted)
                                                 .Select(departmentMember => departmentMember.Department)
                                                 .ToList() ;
        }
    }
}
