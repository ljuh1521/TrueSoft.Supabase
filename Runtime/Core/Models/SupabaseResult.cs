namespace Truesoft.Supabase
{
    public sealed class SupabaseResult<T>
    {
        public bool IsSuccess { get; }
        public T Data { get; }
        public string ErrorMessage { get; }
        public SupabaseError Error { get; }

        private SupabaseResult(bool isSuccess, T data, string errorMessage, SupabaseError error)
        {
            IsSuccess = isSuccess;
            Data = data;
            ErrorMessage = errorMessage;
            Error = error;
        }

        public static SupabaseResult<T> Success(T data)
        {
            return new SupabaseResult<T>(true, data, null, null);
        }

        public static SupabaseResult<T> Fail(string errorMessage)
        {
            return new SupabaseResult<T>(false, default, errorMessage, null);
        }

        public static SupabaseResult<T> Fail(SupabaseError error, string fallbackMessage = null)
        {
            var message = error?.message ?? fallbackMessage ?? "Unknown error";
            return new SupabaseResult<T>(false, default, message, error);
        }
    }
}