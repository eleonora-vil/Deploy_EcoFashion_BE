namespace EcoFashionBackEnd.Exceptions
{
    public class UnauthorizedException : Exception
    {
        public UnauthorizedException() : base("You are not authenticated to access this resource.")
        {
        }

        public UnauthorizedException(string message) : base(message)
        {
        }
    }
}