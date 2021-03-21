using System;
using System.IO;
using static vws.web.EmailTemplates.EmailTemplateTypes;

namespace vws.web.EmailTemplates
{
    public static class EmailTemplateUtility
    {
        public static string GetEmailTemplate(int inputType)
        {
            var types = Enum.GetValues(typeof(EmailTemplateEnum));
            string path = Path.Combine(Directory.GetCurrentDirectory(), $"EmailTemplates{Path.DirectorySeparatorChar}");

            foreach (var type in types)
            {
                if ((int)type == inputType)
                {
                    path += type.ToString();
                    break;
                }
            }
            path += ".html";
            string content = File.ReadAllText(path);
            return content;
        }


    }
}
