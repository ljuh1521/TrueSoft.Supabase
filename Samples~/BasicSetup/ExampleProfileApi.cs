using System;
using UnityEngine;

namespace Truesoft.Supabase.Samples
{
    [Serializable]
    public sealed class PlayerProfileDto
    {
        public string user_id;
        public string nickname;
    }

    public sealed class ExampleProfileApi : MonoBehaviour
    {
        [SerializeField] private string email;
        [SerializeField] private string password;

        private async void Start()
        {
            var signIn = await SupabaseSDK.Auth.SignInWithPasswordAsync(email, password);

            if (!signIn.Success)
            {
                Debug.LogError($"SignIn Failed: {signIn.Error?.Message} / {signIn.Error?.Raw}");
                return;
            }

            var result = await SupabaseSDK.Database.FilterEqAsync<PlayerProfileDto>(
                "player_profiles",
                "user_id",
                SupabaseSDK.Auth.UserId,
                new QueryOptions { Select = "*", Limit = 1 });

            if (!result.Success)
            {
                Debug.LogError($"Profile Load Failed: {result.Error?.Message} / {result.Error?.Raw}");
                return;
            }

            if (result.Data.Length > 0)
                Debug.Log($"Nickname = {result.Data[0].nickname}");
            else
                Debug.Log("Profile not found.");
        }
    }
}