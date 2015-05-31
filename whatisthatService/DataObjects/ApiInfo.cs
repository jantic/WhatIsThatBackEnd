using System;
using System.Runtime.Serialization;
using System.ServiceModel.Web;

namespace whatisthatService.DataObjects
{
    [DataContract]
    public class ApiInfo
    {
        public ApiInfo(double apiVersion, long maxImageSize, long minImageSize)
        {
            ApiVersion = apiVersion;
            MaxImageSize = maxImageSize;
            MinImageSize = minImageSize;
        }

        [DataMember] 
        public double ApiVersion { get; set; }

        [DataMember] 
        public long MaxImageSize { get; set; }

        [DataMember] 
        public long MinImageSize { get; set; }
    }
}