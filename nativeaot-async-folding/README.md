# Native AOT: method body folding is quadratic for struct async state machines

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

Steps to reproduce (any OS/RID supported by Native AOT; use `python` instead of `python3` on Windows):
1. Generate N async methods (default 4000) in `AsyncFolding/Generated.cs`:
```bash
python3 ./generate_async.py -n 4000
```
2. Publish with folding (the default) and without it, and compare the `IlcCompile` time:
```bash
python3 ./measure.py 4000 8000
```
`measure.py` publishes `-c Release` for the host RID (or `--rid`) twice per N, with the default settings and with `-p:IlcFoldIdenticalMethodBodies=false`,
and prints the wall time of each publish. Pass `--ilc-tools-path <runtime>/artifacts/bin/coreclr/<rid>.Release/ilc-published/`
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
