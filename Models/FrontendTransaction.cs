using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AtaccamaTransactions
{
    public class FrontendTransaction
	{
		public string Name { get; set; }
		//All time values are converted to miliseconds to unify scale with request duration and time stamps
		public int Duration { get; set; }
		public int ThinkTime { get; set; }
		public int WastedTime { get; set; }
		//End state of transaction, basic states are "Pass" or "Fail", and state "Not Found" is for null and other results
		public string CompletionState { get; set; }
		public string StartMessageID { get; set; }
		public string EndMessageID { get; set; }

		//Start and end positions of transaction record in log file, just for internal use
		[JsonIgnore]
		public int StartLineIndex { get; set; }
		[JsonIgnore]
		public int EndLineIndex { get; set; }

        //List of all test Steps which were executed within Transaction duration
        public List<Step> Steps { get; set; } = new List<Step>();
    }
}