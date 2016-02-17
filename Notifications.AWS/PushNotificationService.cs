using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Newtonsoft.Json;
using Notifications.AWS.Interfaces;

namespace Notifications.AWS
{

    public class PushNotificationService : IPushNotificationService
    {
        private readonly string _accesskey;
        private readonly string _secretkey;

        public PushNotificationService(string accesskey, string secretkey)
        {
            if (string.IsNullOrWhiteSpace(accesskey)) throw new ArgumentNullException("accesskey");
            if (string.IsNullOrWhiteSpace(secretkey)) throw new ArgumentNullException("secretkey");

            _accesskey = accesskey;
            _secretkey = secretkey;
        }


        public int SendPush(string message, string endpointarn, int countnotificationnotread)
        {

            try
            {

                if (endpointarn == null) throw new ArgumentNullException("endpointarn");
                if (string.IsNullOrWhiteSpace(message)) throw new ArgumentNullException("message");

                var endpoint = GetEndpoint(string.Empty, endpointarn);
                if (endpoint == null) throw new ArgumentNullException("endpointarn");

                var platform = GetPlatformArn(endpointarn);
                if (string.IsNullOrEmpty(platform)) throw new ArgumentNullException("platform");

                Dictionary<string, string> customParams = new Dictionary<string, string>();
                customParams.Add("default", message);
                customParams.Add("" + platform + "", FormatDataPlatform(platform, message, countnotificationnotread));

                var msgSerialize = JsonConvert.SerializeObject(customParams);

                return PublishEndpoint(endpoint, msgSerialize);

            }
            catch (Exception ex)
            {
                throw new Exception("SendPush " + ex.Message);
            }
        }


        public IEnumerable<string> ListEndpointsToken()
        {
            try
            {
                return ListEndpoints().Select(e => e.EndpointArn).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception("ListEndpointsToken " + ex.Message);
            }

        }

        public List<string> GetEndpoints(string description)
        {
            try
            {
                var list = ListEndpoints();

                if (!string.IsNullOrWhiteSpace(description))
                    list = ListEndpoints().Where(e => e.Attributes.Any(t => t.Key == "CustomUserData" && t.Value.Contains(description)));

                return list.Select(s => s.EndpointArn).ToList();
            } 
            catch (Exception ex)
            {

                throw new Exception("GetEndpoints " + ex.Message);
            }

        }

        public string GetEndpoint(string endpointtoken, string endpointarn)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(endpointarn))
                {
                    var attr = GetEndpointAttributes(endpointarn);
                    return attr.Any() ? endpointarn : null;
                }
                  
                if (!string.IsNullOrWhiteSpace(endpointtoken))
                    return ListEndpoints().FirstOrDefault(e => e.Attributes.Any(t => t.Key == "Token" && t.Value == endpointtoken))?.EndpointArn;
              
                return null;
            }
            catch (Exception ex)
            {
                
                throw new Exception("GetEndpoint " + ex.Message);
            }
        }

        public string CreateEndpoint(string registrationid, string description, string aplicationarn)
        {

            try
            {
                var endpoint1 = GetEndpoint(registrationid, String.Empty);

                if (endpoint1 != null)
                    throw new ArgumentException("registrationid");

                var endpoint2 = GetEndpoints(description);

                if (endpoint2 != null)
                {
                    if (endpoint2.Count == 1)
                        return endpoint2.FirstOrDefault();

                    if (endpoint2.Count > 1) throw new ArgumentException("description");
                }

                var app = GetApplication(aplicationarn, string.Empty);
                if (string.IsNullOrEmpty(app)) throw new ArgumentException("aplicationtoken");

                using (var snsclient = new AmazonSimpleNotificationServiceClient(_accesskey, _secretkey))
                {
                    var request = new CreatePlatformEndpointRequest()
                    {
                        CustomUserData = description,
                        Token = registrationid,
                        PlatformApplicationArn = aplicationarn
                    };

                    var result = snsclient.CreatePlatformEndpoint(request);

                    return result?.EndpointArn;

                }
            }
            catch (Exception ex)
            {
                throw new Exception("CreateEndpoint " + ex.Message);
            }
 
        }

        public IDictionary<string, string> GetEndpointAttributes(string endpointarn)
        {

            try
            {

                using (var snsclient = new AmazonSimpleNotificationServiceClient(_accesskey, _secretkey))
                {
                    var result = snsclient.GetEndpointAttributes(new GetEndpointAttributesRequest()
                    {
                        EndpointArn = endpointarn
                    });

                    return result.Attributes;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("GetEndpointAttributes " + ex.Message);
            }
        }

        public void SetEndpointAttribute(string endpointarn, string key, string value)
        {

            try
            {

                var endpoint = GetEndpoint(string.Empty, endpointarn);

                if (string.IsNullOrEmpty(endpoint)) throw new ArgumentNullException("endpointarn");

                Dictionary<string, string> attributes = (Dictionary<string, string>) GetEndpointAttributes(endpointarn);

                attributes[key] = value;

                using (var snsclient = new AmazonSimpleNotificationServiceClient(_accesskey, _secretkey))
                {
                    var result = snsclient.SetEndpointAttributes(new SetEndpointAttributesRequest()
                    {
                        EndpointArn = endpointarn,
                        Attributes = attributes
                    });

                }
            }
            catch (Exception ex)
            {
                throw new Exception("SetEndpointAttribute " + ex.Message);
            }

        }

        public string GetApplication(string applicationarn, string endpointarn)
        {
            try
            {

                if (!string.IsNullOrEmpty(applicationarn))
                {

                    return ListApplications().FirstOrDefault(p => p == applicationarn);
         
                }

                if (!string.IsNullOrEmpty(endpointarn))
                {
                    return endpointarn.Substring(0,
                        endpointarn.LastIndexOf("/", StringComparison.Ordinal));
                }

                return null;
            }
            catch (Exception ex)
            {
                throw new Exception("GetApplication " + ex.Message);
            }

        }

        public IEnumerable<string> ListApplications()
        {
            try
            {

                using (var snsclient = new AmazonSimpleNotificationServiceClient(_accesskey, _secretkey))
                {
                    var listapps = snsclient.ListPlatformApplications(new ListPlatformApplicationsRequest());
                    return listapps.PlatformApplications.Select(s => s.PlatformApplicationArn);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("ListApplications " + ex.Message);
            }

        }

        public string GetPlatformArn(string arn)
        {
            try
            {
                if (string.IsNullOrEmpty(arn)) throw new ArgumentException("arn");

                var index = arn.IndexOf("/", StringComparison.Ordinal) + 1;
                var nextindex = arn.Substring(index).IndexOf("/", StringComparison.Ordinal) + index;

                return arn.Substring(index, nextindex -index);
                
            }
            catch (Exception ex)
            {
                throw new Exception("GetPlatformApplication " + ex.Message);
            }
        }


        public bool IsEndpointEnabled(string endpointarn)
        {
            try
            {
                var attr = GetEndpointAttributes(endpointarn);

                return attr.Any(a => a.Key == "Enabled" && a.Value == "true");
            }
            catch (Exception ex)
            {
                throw new Exception("IsEndpointEnable " + ex.Message);
            }
        }

        public bool IsEndpointInApplication(string endpointarn, string applicationarn)
        {
            var list = ListEndpoints(applicationarn);
            return list.Any(l => l.EndpointArn == endpointarn);
        }

        public void DeleteEndpoint(string endpointarn)
        {
            try
            {
                using (var snsclient = new AmazonSimpleNotificationServiceClient(_accesskey, _secretkey))
                {
                    snsclient.DeleteEndpoint(new DeleteEndpointRequest()
                    {
                        EndpointArn = endpointarn
                    });
                }

            }
            catch (Exception ex)
            {
                throw new Exception("DeleteEndpoint " + ex.Message);
            }
        }


        private IEnumerable<Endpoint> ListEndpoints()
        {

            var listendpoints = new List<Endpoint>();

                foreach (var apps in ListApplications())
                {
                    var result = ListEndpoints(apps);

                    if (result != null) listendpoints.AddRange(result);
                }

                return listendpoints;
            
        }

        private IEnumerable<Endpoint> ListEndpoints(string applicationarn)
        {
            using (var snsclient = new AmazonSimpleNotificationServiceClient(_accesskey, _secretkey))
            {

                    var result = snsclient.ListEndpointsByPlatformApplication(new ListEndpointsByPlatformApplicationRequest()
                    {
                        PlatformApplicationArn = applicationarn

                    });             

                return result.Endpoints;
            }
        }

        private int PublishEndpoint(string endpointarn, string msg)
        {

            try
            {


                using (var snsclient = new AmazonSimpleNotificationServiceClient(_accesskey, _secretkey))
                {

                    var publish = new PublishRequest()
                    {
                        TargetArn = endpointarn,
                        Message = msg,
                        MessageStructure = "json"
                    };
                    snsclient.Publish(publish);

                    return 0;

                }
            }
            catch (Exception)
            {
                return -1;
            }
        }

        private string FormatDataPlatform(string platform, string message, int notificationnored)
        {
            switch (platform)
            {
                case "GCM":
                    return "{ \"data\": { \"title\": \"ONnergy\" ,\"message\": \"" + message + "\" } }";
                case "APNS":
                case "APNS_SANDBOX":
                      return "{\"aps\":{\"alert\":\"" + message + "\",\"badge\":" + notificationnored + ", \"sound\" : \"bingbong.aiff\"}}";  
                   // return "{\"aps\":{\"alert\":\"" + message + "\",\"badge\":" + notificationnored + "}}";
            }
            return string.Empty;
        }

    }
}
