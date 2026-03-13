# Truesoft Supabase SDK

Unity Package Manager Git package for Supabase Auth, REST API, and Edge Functions.

## Install

Window > Package Manager > + > Install package from git URL

Example:
https://github.com/your-org/com.truesoft.supabase.git#0.1.0

## Setup

1. Tools > Truesoft > Supabase > Create Settings Asset
2. Fill ProjectUrl and PublishableKey
3. Tools > Truesoft > Supabase > Install In Current Scene
4. Assign SupabaseSettings asset to SupabaseRunner

## Scope

- Auth: sign-in, refresh, sign-out
- Database: select, filter, insert
- Functions: invoke




의존성 항목 추가
    implementation "androidx.credentials:credentials:1.6.0-rc02"
    implementation "androidx.credentials:credentials-play-services-auth:1.6.0-rc02"
    implementation "com.google.android.libraries.identity.googleid:googleid:1.1.1"
    implementation "org.jetbrains.kotlinx:kotlinx-coroutines-android:1.8.1"