namespace VoteRightWebApp.Models
{
    public class Assembly
    {
        public string District { get; set; } = string.Empty;
        public string Number { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int BoothCount { get; set; }
    }
}
