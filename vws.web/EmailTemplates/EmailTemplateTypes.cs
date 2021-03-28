using System;
namespace vws.web.EmailTemplates
{
    public class EmailTemplateTypes
    {
        public enum EmailTemplateEnum : int
        {
            EmailVerificationCode = 1,
            ResetPassword = 2,
            ChangePasswordAlert = 3,
            TaskAssign = 4
        };
    }
}
