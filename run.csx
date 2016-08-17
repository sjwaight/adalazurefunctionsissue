#r "Newtonsoft.Json"
#r "System.Net.Http"
#r "System.Threading.Tasks"
#r "System.Runtime"

using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Runtime;
using System.Net.Http.Headers;
using System.Threading.Tasks;

public static void Run(dynamic input, TraceWriter log)
{
    ProcessRequest("user-object-id-in-aad", log);
}

public static async Task<bool> ProcessRequest(string userObjectId, TraceWriter log)
{
    var tenant = "yourb2ctenant.onmicrosoft.com";
	// this is a service principal account in the target directory
    var clientId = "688358db-xxxx-xxxx-xxxx-54xxxxf027ff";
    var clientSecret = "not-really-my-secret";

    var userJsonString = await GraphClient.GetUserByObjectId(tenant, clientId, clientSecret, userObjectId, log);

    return true;
}

public static class GraphClient
{
    public static async Task<string> GetUserByObjectId(string targetTenant, string consumerId, string consumerSecret, string objectId, TraceWriter log)
    {
        return await SendGraphGetRequest(consumerId, consumerSecret, targetTenant, "/users/" + objectId, null, log);
    }

    public static async Task<string> SendGraphGetRequest(string consumerId, string consumerSecret, string targetTenant, string api, string query, TraceWriter log)
    {
        try
        {
			// this is where the exception is thrown.
            var authContext = new AuthenticationContext("https://login.microsoftonline.com/" + targetTenant);
			var credentials = new ClientCredential(consumerId, consumerSecret);

			AuthenticationResult result = await authContext.AcquireTokenAsync("https://graph.windows.net", credentials);

            // For B2C user managment, be sure to use the 1.6 Graph API version.
            var http = new HttpClient();
            string url = "https://graph.windows.net/" + targetTenant + api + "?" + "api-version=1.6";
            if (!string.IsNullOrEmpty(query))
            {
                url += "&" + query;
            }

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
            HttpResponseMessage response = await http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                string error = await response.Content.ReadAsStringAsync();
                object formatted = JsonConvert.DeserializeObject(error);
                throw new Exception("Error Calling the Graph API: \n" + JsonConvert.SerializeObject(formatted, Formatting.Indented));
            }

            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            log.Info("error:" + ex.ToString());
        }
        return string.Empty;
    }
}