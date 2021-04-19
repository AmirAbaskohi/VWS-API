using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace vws.web.Services
{
    public interface INotificationService
    {
        public Task SendMultipleEmails(int template, List<Guid> userIds, string emailMessage, string emailSubject, string[] arguments, bool[] argumentsLocalize = null);
        public Task SendSingleEmail(int template, string emailMessage, string subject, Guid sendToUserId, string[] arguments, bool[] argumentsLocalize = null);
        public void SendMultipleNotification(List<Guid> userIds, byte notificationTypeId, long notificationId);
        public string LocalizeActivityByType(string message, string culture, byte type);
        public List<string> LocalizeActivityParametersByType(List<string> parameters, List<bool> parametersShouldBeLocalized, string culture, byte type);
    }
}
