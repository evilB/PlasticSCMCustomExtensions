using Newtonsoft.Json;

namespace Codice.Client.IssueTracker.SampleExtension.Model
{
    public class MyServiceData
    {
        [JsonProperty("userId")]
        public int UserId { get; set; }

        [JsonProperty("id")]
        public int Id { get; set; }
        
        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("body")]
        public string Body { get; set; }
    }
}
