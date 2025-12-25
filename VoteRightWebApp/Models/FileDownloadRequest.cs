namespace VoteRightWebApp.Models
{
    public class FileDownloadRequest
    {
        public required string AssemblyNumber { get; set; }
        public required string AssemblyName { get; set; }
        public string? BoothRange { get; set; }
    }
}
