using UnityEngine;

namespace Truesoft.Supabase.Samples
{
    public sealed class ExampleBootstrap : MonoBehaviour
    {
        [SerializeField] private SupabaseSettings settings;

        private async void Start()
        {
            await SupabaseSDK.InitializeAsync(settings);
            Debug.Log("Supabase initialized.");
        }
    }
}