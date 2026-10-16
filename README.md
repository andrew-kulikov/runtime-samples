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

## 2. Native AOT: method body folding is quadratic for struct async state machines

.NET 10 folds identical generic method bodies by default ([#117411](https://github.com/dotnet/runtime/pull/117411),
`--methodbodyfolding:generic` whenever `Optimize=true`). When code is compiled with optimizations, every `async` method
gets its own struct state machine `TSM`, so Native AOT compiles a separate copy of
`AsyncStateMachineBox<TResult, TSM>.MoveNext`, `.ExecutionContextCallback`, `AsyncMethodBuilderCore.Start<TSM>`,
`AsyncTaskMethodBuilder<TResult>.GetStateMachineBox<TSM>` etc. for each async method. These copies are byte-identical;
only their relocations (the `TSM` type handle, `TSM.MoveNext`) differ.

`MethodInternKey` in `ObjectDataInterner`/`MethodBodyDeduplicator` hashes code bytes, GC info, frame infos and EH info,
but not relocations. All copies therefore share one hash code, and every insertion into the `HashSet` compares the new
body against all previous copies (`MethodInternComparer.AreSame`), failing only at the differing relocation. That is
O(n²) per method family per pass, and `EnsureMap` repeats the pass until the remapping converges. In a real iOS app with
~12,000 async methods this phase takes ~15 minutes of single-threaded time inside `ObjectWriter.EmitObject`
(the whole ILC run takes ~20 min instead of ~4 min with folding disabled).

Steps to reproduce (osx-arm64 shown; any RID works):
1.
```bash
cd ./nativeaot-async-folding
```
2. Generate N async methods (default 4000) in `AsyncFolding/Generated.cs`:
```bash
python3 ./generate_async.py -n 4000
```
3. Publish with folding (the default) and without it, and compare the `IlcCompile` time:
```bash
./measure.sh 4000 8000
```
`measure.sh` publishes `-c Release` twice per N, with the default settings and with `-p:IlcFoldIdenticalMethodBodies=false`,
and prints the wall time of each publish. Pass `ILC_TOOLS_PATH=<runtime>/artifacts/bin/coreclr/<rid>.Release/ilc-published/`
to use a locally built ILC.

Results (M1 Pro, SDK 10.0.401 / ILCompiler 10.0.12, whole `dotnet publish` wall time):

| N async methods | folding on (default) | folding off | folding on, relocations hashed |
|---:|---:|---:|---:|
| 2000 | 13 s | 7 s | |
| 4000 | 47 s | 9 s | 9 s |
| 8000 | 271 s | 15 s | 16 s |

Doubling N makes the extra time ~6x larger, and the folding gives almost nothing here (the async copies are not foldable,
they only look alike to the hash). The last column is ILC 10.0.13 with relocations added to the `MethodInternKey`
hash; its output is byte-identical to the unpatched ILC.
