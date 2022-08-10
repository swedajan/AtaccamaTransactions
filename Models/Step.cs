using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AtaccamaTransactions
{
    public class Step
    {
        public string Name { get; set; }
        //Step id in format 1.1.1
        public string InternalID { get; set; }
        //All time values are converted to miliseconds to unify scale with request duration and time stamps
        public int Duration { get; set; }
        public int StartTime { get; set; }
        public int EndTime { get; set; }
        //End state of step, either "Pass" or "Fail"
        public string CompletionState { get; set; }

        //Start and end positions of step record in log file, just for internal use
        [JsonIgnore]
        public int StartLineIndex { get; set; }
        [JsonIgnore]
        public int EndLineIndex { get; set; }

        //List of all test Requests which were executed within Step duration
        public List<Request> Requests { get; set; } = new List<Request>();
    }
}