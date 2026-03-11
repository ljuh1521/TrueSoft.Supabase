namespace Truesoft.Supabase
{
    public interface ISupabaseAuthStorage
    {
        void SaveSession(string json);
        string LoadSession();
        void ClearSession();
    }
}