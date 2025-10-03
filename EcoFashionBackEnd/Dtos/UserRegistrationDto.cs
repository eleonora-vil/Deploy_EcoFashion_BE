namespace EcoFashionBackEnd.Dtos
{
    public class UserRegistrationPointDto
    {
        public DateTime Month { get; set; }
        public int UserCount { get; set; }
    }

    public class UserRegistrationAnalyticsDto
    {
        public List<UserRegistrationPointDto> RegistrationPoints { get; set; } = new();
        public int TotalUsers { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class UserRegistrationRequestDto
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
