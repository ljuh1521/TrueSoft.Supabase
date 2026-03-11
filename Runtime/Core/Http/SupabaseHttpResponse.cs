namespace Truesoft.Supabase
{
    public sealed class SupabaseHttpResponse
    {
        public long StatusCode;
        public string Text;
        public string ErrorMessage;

        public bool IsSuccess => StatusCode >= 200 && StatusCode < 300;
    }
}