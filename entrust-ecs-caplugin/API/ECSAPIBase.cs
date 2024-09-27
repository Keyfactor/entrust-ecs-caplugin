using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Keyfactor.Extensions.CAPlugin.Entrust.API
{
    public abstract class ECSBaseRequest
    {
        [JsonIgnore]
        public string Resource { get; internal set; }

        [JsonIgnore]
        public string Method { get; internal set; }

        [JsonIgnore]
        public string TargetURI { get; set; }

        public string BuildParameters()
        {
            return "";
        }
    }

    public abstract class ECSBaseResponse
    {
        public enum StatusType
        {
            SUCCESS,
            ERROR,
            WARNING
        }

        public enum ContentTypes
        {
            XML,
            JSON,
            TEXT
        }

        [JsonIgnore]
        public ContentTypes ContentType { get; internal set; }

        [JsonIgnore]
        public StatusType Status { get; set; }

        [JsonIgnore]
        public List<Error> Errors { get; set; }

        public ECSBaseResponse()
        {
            Errors = new List<Error>();
            Status = StatusType.SUCCESS;
            ContentType = ContentTypes.JSON;
        }
    }
}
