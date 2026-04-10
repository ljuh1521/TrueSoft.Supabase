# RemoteConfig Source Generator

`Truesoft.Supabase.RemoteConfig.SourceGenerator`는 `[RemoteConfig]` / `[RemoteConfigKey("…")]` 선언으로 `RemoteConfigEntry<T>` 접근 코드를 생성합니다.

## 빌드 및 패키지 반영

Release 빌드 시 **ILRepack**으로 `Microsoft.CodeAnalysis` 등 의존성이 제너레이터 DLL 하나로 병합됩니다. Unity 패키지에는 `RoslynAnalyzers/Truesoft.Supabase.RemoteConfig.SourceGenerator.dll`만 두면 됩니다. `bin/Release/netstandard2.0`에는 병합 후에도 위성 DLL이 남을 수 있으니, 패키지에는 위 DLL만 복사하면 됩니다(정리하려면 `dotnet clean` 후 Release 빌드).

저장소 루트에서:

```powershell
dotnet build Generators/Truesoft.Supabase.RemoteConfig.SourceGenerator/Truesoft.Supabase.RemoteConfig.SourceGenerator.csproj -c Release
Copy-Item -Force Generators/Truesoft.Supabase.RemoteConfig.SourceGenerator/bin/Release/netstandard2.0/Truesoft.Supabase.RemoteConfig.SourceGenerator.dll RoslynAnalyzers/
```

Unity는 UPM 패키지 루트의 **`RoslynAnalyzers`** 폴더에 있는 분석기 DLL을 소비 프로젝트 컴파일에 포함합니다(패키지를 참조하는 `asmdef`가 있을 때).

## 요구 사항

- `Truesoft.Supabase.Unity` 참조(속성·`RemoteConfigEntry<T>` 정의 포함).
- `static partial` 클래스에 `[RemoteConfig]`.
- `public static partial RemoteConfigEntry<TYourType> MethodName();` 형태에 `[RemoteConfigKey("remote_config.key")]`.
