using System;

namespace Truesoft.Supabase.Core.Data
{
    /// <summary>
    /// <c>user_saves</c>(또는 커스텀 세이브 테이블) 컬럼과 C# 멤버를 묶습니다.
    /// <see cref="UserSaveSchema"/>가 <c>select</c> CSV와 PATCH 키를 자동 생성할 때 사용합니다.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public sealed class UserSaveColumnAttribute : Attribute
    {
        /// <param name="columnName">Postgres/PostgREST 컬럼명(예: <c>gold</c>, <c>coin_count</c>). 비우면 멤버 이름을 그대로 씁니다.</param>
        public UserSaveColumnAttribute(string columnName = null)
        {
            ColumnName = string.IsNullOrWhiteSpace(columnName) ? null : columnName.Trim();
        }

        /// <summary>DB 컬럼명. null이면 대상 멤버의 이름을 사용합니다.</summary>
        public string ColumnName { get; }
    }
}
