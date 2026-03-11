namespace Truesoft.Supabase
{
    public sealed class SupabaseOptions
    {
        public string ProjectURL;
        public string PublishableKey;
        public int TimeoutSeconds = 30;

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(ProjectURL))
                throw new System.InvalidOperationException("Supabase URL is empty.");

            if (string.IsNullOrWhiteSpace(PublishableKey))
                throw new System.InvalidOperationException("Supabase PublishableKey is empty.");
        }
    }
}