namespace AtaccamaTransactions
{
    public class Request
    {
        public int InternalID { get; set; }
        // Lenght of time (ms) calculated as difference between Time values of RequestHeaders and ResponseHeaders
        public int Duration { get; set; }
        //Request's target url
        public string URL { get; set; }

        //Requests components in separate object classes
        public RequestHeaders RequestHeaders { get; set; }
        public RequestBody RequestBody { get; set; }
        public ResponseHeaders ResponseHeaders { get; set; }
    }
}