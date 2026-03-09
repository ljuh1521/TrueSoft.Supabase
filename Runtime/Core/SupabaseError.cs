namespace Truesoft.Supabase
{
    public sealed class SupabaseError
    {
        public long HttpStatus;
        public string Code;
        public string Message;
        public string Raw;
    }
}