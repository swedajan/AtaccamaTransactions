using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AtaccamaTransactions
{
    public class Step
    {
        public string Name { get; set; }
        public string IdIndex { get; set; }
        public int Duration { get; set; }
        public int StartTime { get; set; }
        public int EndTime { get; set; }
        public string CompletionState { get; set; }

        //Start and end positions of step record in log file
        [JsonIgnore]
        public int StartLineIndex { get; set; }
        [JsonIgnore]
        public int EndLineIndex { get; set; }

        //List of all test Requests which were executed within Step duration
        public List<Request> Requests { get; set; } = new List<Request>();
    }
}