# Native AOT: method body folding is quadratic in the number of async methods (struct state machines)

### Description

Since .NET 10, Native AOT folds identical generic method bodies by default (`--methodbodyfolding:generic` when
`Optimize=true`, #117411). For code compiled with optimizations, each `async` method has its own struct state machine,
so ILC emits one copy per async method of `AsyncStateMachineBox<TResult, TSM>.MoveNext`,
`AsyncStateMachineBox<TResult, TSM>.ExecutionContextCallback`, `AsyncMethodBuilderCore.Start<TSM>`,
`AsyncTaskMethodBuilder<TResult>.GetStateMachineBox<TSM>` and similar methods.

These copies have identical code bytes, GC info and unwind info and differ only in relocation targets. `MethodInternKey`
(`ObjectDataInterner` in 10.0, `MethodBodyDeduplicator` in main) does not hash relocations, so all copies of a method
family get the same hash code, and the generics-only check (same typical method definition) passes too. Every
`HashSet.TryGetValue` then runs `MethodInternComparer.AreSame` against all earlier copies, which fails only at the
differing relocation. This is O(n²) per method family, repeated for every pass of the fixed-point loop in `EnsureMap`,
and it all runs single-threaded inside `ObjectWriter.EmitObject`.

In our iOS app (~12,000 async methods) `ObjectDataInterner.EnsureMap` takes ~886 s of the ~1200 s ILC run. With
`IlcFoldIdenticalMethodBodies=false` ILC takes 252 s.

### Reproduction steps

A self-contained sample: https://github.com/andrew-kulikov/runtime-samples/tree/master/nativeaot-async-folding

```bash
git clone https://github.com/andrew-kulikov/runtime-samples
cd runtime-samples/nativeaot-async-folding
python3 ./generate_async.py -n 8000     # 8000 x `async Task<int> M(int x) { await Task.Yield(); return x + i; }`
dotnet publish AsyncFolding -c Release -r osx-arm64                                       # ~270 s
dotnet publish AsyncFolding -c Release -r osx-arm64 -p:IlcFoldIdenticalMethodBodies=false # ~15 s
```

(any RID works; `./measure.sh 2000 4000 8000` runs both variants for several sizes)

Whole `dotnet publish` wall time, M1 Pro, SDK 10.0.401 (ILCompiler 10.0.12):

| async methods | folding on (default) | folding off |
|---:|---:|---:|
| 2000 | 13 s | 7 s |
| 4000 | 47 s | 9 s |
| 8000 | 271 s | 15 s |

The binary size is the same in both cases (the async copies aren't foldable, they only collide in the hash).

### Expected behavior

Enabling method body folding should not make compilation time grow quadratically with the number of async methods.

### Actual behavior

Compilation time with folding grows quadratically; at a few thousand async methods it dominates the whole build.

### Regression?

Yes, in practice: before .NET 10 folding was off by default for these builds (opt-in), see #117411.

### Known workarounds

- `<IlcFoldIdenticalMethodBodies>false</IlcFoldIdenticalMethodBodies>` (loses the size gains of folding elsewhere,
  ~+11 MB in our app).
- `<Optimize>false</Optimize>` for the assemblies that contain most of the async methods. Without optimizations the C#
  compiler emits class state machines instead of struct ones, so all async methods share the canonical
  `AsyncStateMachineBox<TResult, __Canon>` code and there are no per-method copies to deduplicate. In our app, the
  build configuration that compiles its own libraries without optimization spends ~16 s in `EnsureMap` instead of
  ~886 s. The cost is unoptimized IL in those assemblies and an extra heap allocation per async call.

### Proposed fix

Hash relocations in `MethodInternKey` consistently with `MethodInternComparer.AreSame`: reloc type, offset and either
the contents of a `MethodReadOnlyDataNode` target, or the identity of the target after mapping it through the previous
pass's remapping. With that change (on 10.0.13) the 8000-method sample publishes in 16 s and our app's ILC run takes
~250 s, and the object files are byte-identical to the unpatched compiler.

### Configuration

.NET SDK 10.0.401, ILCompiler 10.0.12 / 10.0.13 built from source, macOS 26 arm64 (target osx-arm64 and ios-arm64).
