# runtime-samples
Playground for .net runtime

## 1. Mono AOT hang

A critical deadlock occurs in Mono AOT runtime when processing assemblies with a large number of classes (>65,000). The issue causes application hangs on both Android and iOS platforms using .NET 9 with AOT compilation.

The issue was introduced by PR [#85952](https://github.com/dotnet/runtime/pull/85952), which added an "optimization" to prevent failures when an AOT assembly contains more than 65,000 classes. When this threshold is exceeded, the AOT compiler produces a name table of size 0.

In the runtime code at [`aot-runtime.c:2711-2712`](https://github.com/dotnet/runtime/blob/f50a572dd5718d56277356a843989dfae57f301f/src/mono/mono/mini/aot-runtime.c#L2711-L2712), when the name table size is 0, the code path returns early **without calling `amodule_unlock`**. This causes the AOT module lock to be held indefinitely, leading to deadlocks when other threads attempt to access the same module.

Steps to reproduce:  
1.
```bash
cd ./maui-aot-hang
```
2. Generate 65000 types in `MauiAotHang.Features` lib:
```bash
python ./generate_classes.py -o ./MauiAotHang.Features/sample.cs simple -n 65000
```
3. Publish app with mono AOT (for example for Android x64 emulator on windows)
```bash
dotnet publish ./MauiAotHang/MauiAotHang.csproj -f net9.0-android -r android-x64
```
4. Install and run app
