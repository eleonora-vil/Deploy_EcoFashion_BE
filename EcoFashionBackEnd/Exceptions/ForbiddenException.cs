namespace EcoFashionBackEnd.Exceptions
{
    public class ForbiddenException : Exception
    {
        public ForbiddenException() : base("You don't have permission to perform this action.")
        {
        }

        public ForbiddenException(string message) : base(message)
        {
        }
    }
}