using System;

namespace Truesoft.Supabase
{
    [Serializable]
    public sealed class SupabaseError
    {
        public string code;
        public string error_code;
        public string msg;
        public string message;

        public string GetBestMessage()
        {
            if (string.IsNullOrWhiteSpace(message) == false)
                return message;

            if (string.IsNullOrWhiteSpace(msg) == false)
                return msg;

            if (string.IsNullOrWhiteSpace(error_code) == false)
                return error_code;

            if (string.IsNullOrWhiteSpace(code) == false)
                return code;

            return "Unknown error";
        }
    }
}