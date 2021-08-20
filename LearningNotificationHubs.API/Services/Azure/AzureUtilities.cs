using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace LearningNotificationHubs.API.Services.Azure
{
    public static class AzureUtililities
    {
        public static string GenerateSASToken(string resourceUri, string keyName, string key, TimeSpan? expires = null)
        {
            if (expires is null)
            {
                expires = TimeSpan.FromHours(1);
            }
            TimeSpan expirySinceEpoch = DateTime.UtcNow - new DateTime(1970, 1, 1) + expires.Value;
            string expiresString = Convert.ToString((int)expirySinceEpoch.TotalSeconds);

            string stringToSign = HttpUtility.UrlEncode(resourceUri) + "\n" + expiresString;
            string signature;
            using (HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key)))
            {
                signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));
            }

            string sasToken = $"SharedAccessSignature sr={HttpUtility.UrlEncode(resourceUri)}&sig={HttpUtility.UrlEncode(signature)}&se={expiresString}&skn={keyName}";

            return sasToken;
        }
    }
    
}
