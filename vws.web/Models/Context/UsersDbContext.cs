﻿using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Models.Context
{
    public class UsersDbContext : IdentityDbContext
    {
        public UsersDbContext(DbContextOptions dbContextOptions)
        : base(dbContextOptions)
        {

        }
    }
}
