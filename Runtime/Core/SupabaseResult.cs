namespace Truesoft.Supabase
{
    public readonly struct SupabaseResult<T>
    {
        public readonly bool Success;
        public readonly T Data;
        public readonly SupabaseError Error;

        public SupabaseResult(T data)
        {
            Success = true;
            Data = data;
            Error = null;
        }

        public SupabaseResult(SupabaseError error)
        {
            Success = false;
            Data = default;
            Error = error;
        }
    }
}