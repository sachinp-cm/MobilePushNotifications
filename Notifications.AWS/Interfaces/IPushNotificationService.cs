using System.Collections.Generic;

namespace Notifications.AWS.Interfaces
{
    public interface IPushNotificationService
    {
        int SendPush(string message, string endpointarn, int countnotificationnotread);
        IEnumerable<string> ListEndpointsToken();
        List<string> GetEndpoints(string description);
        string GetEndpoint(string endpointtoken, string endpointarn);
        string CreateEndpoint(string registrationid, string description, string aplicationarn);
        IDictionary<string, string> GetEndpointAttributes(string endpointarn);
        void SetEndpointAttribute(string endpointarn, string key, string value);
        string GetApplication(string applicationarn, string endpointarn);
        IEnumerable<string> ListApplications();
        string GetPlatformArn(string arn);
        bool IsEndpointEnabled(string endpointarn);
        bool IsEndpointInApplication(string endpointarn, string applicationarn);
        void DeleteEndpoint(string endpointarn);
    }
}