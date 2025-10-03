using EcoFashionBackEnd.Entities;
using EcoFashionBackEnd.Dtos;
using EcoFashionBackEnd.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EcoFashionBackEnd.Services
{
    public class UserRegistrationAnalyticsService
    {
        private readonly IRepository<User, int> _userRepository;

        public UserRegistrationAnalyticsService(IRepository<User, int> userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<UserRegistrationAnalyticsDto> GetUserRegistrationAnalyticsAsync(UserRegistrationRequestDto request)
        {
            // Set default date range if not provided (last 12 months)
            var endDate = request.EndDate?.Date ?? DateTime.Now.Date;
            var startDate = request.StartDate?.Date ?? endDate.AddMonths(-12);

            // Convert to end of day for inclusive range
            var endDateTime = endDate.AddDays(1).AddTicks(-1);

            // Get all users registered in the date range
            var users = await _userRepository
                .FindByCondition(u => u.CreatedAt >= startDate && u.CreatedAt <= endDateTime)
                .ToListAsync();

            // Group by month and count users
            var registrationPoints = users
                .GroupBy(u => new DateTime(u.CreatedAt.Year, u.CreatedAt.Month, 1))
                .Select(g => new UserRegistrationPointDto
                {
                    Month = g.Key,
                    UserCount = g.Count()
                })
                .OrderBy(p => p.Month)
                .ToList();

            // Fill missing months with zero values
            registrationPoints = FillMissingMonths(registrationPoints, startDate, endDate);

            return new UserRegistrationAnalyticsDto
            {
                RegistrationPoints = registrationPoints,
                TotalUsers = users.Count,
                StartDate = startDate,
                EndDate = endDate
            };
        }

        private List<UserRegistrationPointDto> FillMissingMonths(List<UserRegistrationPointDto> points, DateTime startDate, DateTime endDate)
        {
            var filledPoints = new List<UserRegistrationPointDto>();
            var currentDate = new DateTime(startDate.Year, startDate.Month, 1);
            var endMonth = new DateTime(endDate.Year, endDate.Month, 1);

            while (currentDate <= endMonth)
            {
                var existingPoint = points.FirstOrDefault(p => p.Month == currentDate);
                if (existingPoint != null)
                {
                    filledPoints.Add(existingPoint);
                }
                else
                {
                    filledPoints.Add(new UserRegistrationPointDto
                    {
                        Month = currentDate,
                        UserCount = 0
                    });
                }

                currentDate = currentDate.AddMonths(1);
            }

            return filledPoints.OrderBy(p => p.Month).ToList();
        }
    }
}
