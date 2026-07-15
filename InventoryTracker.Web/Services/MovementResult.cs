namespace InventoryTracker.Web.Services
{
    public class MovementResult
    {
        public bool IsSuccess { get; }
        public string? ErrorMessage { get; }

        private MovementResult(bool isSuccess, string? errorMessage)
        {
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage;
        }

        public static MovementResult Success() => new(true, null);
        public static MovementResult Fail(string errorMessage) => new(false, errorMessage);
    }
}