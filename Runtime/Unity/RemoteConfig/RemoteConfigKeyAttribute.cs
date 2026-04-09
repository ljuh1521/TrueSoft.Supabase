using System;

namespace Truesoft.Supabase.Unity.RemoteConfig
{
    /// <summary>
    /// <c>RemoteConfigEntry&lt;T&gt;</c>를 반환하는 <c>static partial</c> 메서드에 붙입니다. 인자는 <c>remote_config.key</c>와 동일해야 합니다.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class RemoteConfigKeyAttribute : Attribute
    {
        public string Key { get; }

        public RemoteConfigKeyAttribute(string key) => Key = key;
    }
}
