namespace Truesoft.Supabase
{
    public sealed class SupabaseOptions
    {
        public string Url;
        public string ApiKey;
        public int TimeoutSeconds = 30;

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(Url))
                throw new System.InvalidOperationException("Supabase Url is empty.");

            if (string.IsNullOrWhiteSpace(ApiKey))
                throw new System.InvalidOperationException("Supabase ApiKey is empty.");
        }
    }
}