namespace VoteRightWebApp.Models
{
    public class LoginViewModel
    {
        public string PhoneNumber { get; set; } = string.Empty;
    }

    public class RegisterViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string WhatsAppNumber { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string PoliticalPartyOrganization { get; set; } = string.Empty;
        public string OrganizationalPosition { get; set; } = string.Empty;
    }
}
