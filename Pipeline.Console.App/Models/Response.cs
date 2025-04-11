namespace Pipeline.Sample.App.Models
{
    public class Response
    {
        public bool IsSuccess { get; set; }
        public int ErrorCode { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
