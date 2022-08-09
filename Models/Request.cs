namespace AtaccamaTransactions
{
    public class Request
    {
        public int InternalID { get; set; }
        // Lenght of time (ms) calculated as difference between Time values of RequestHeaders and ResponseHeaders
        public int Duration { get; set; }
        public string URL { get; set; }

        public RequestHeaders RequestHeaders { get; set; }
        public RequestBody RequestBody { get; set; }
        public ResponseHeaders ResponseHeaders { get; set; }
    }
}