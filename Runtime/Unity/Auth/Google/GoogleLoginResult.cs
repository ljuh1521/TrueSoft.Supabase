using System;

namespace Truesoft.Supabase.Unity.Auth.Google
{
    [Serializable]
    public sealed class GoogleLoginResult
    {
        public string IdToken;
        public string GoogleUserId;
        public string DisplayName;
        public string GivenName;
        public string FamilyName;
        public string ProfileImageUrl;
    }
}