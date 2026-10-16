# runtime-samples
Playground for .net runtime: minimal reproductions of .NET runtime and compiler issues.

| Sample | Description |
|---|---|
| [maui-aot-hang](maui-aot-hang/README.md) | Mono AOT deadlock when an assembly has more than 65,000 classes (.NET 9, Android/iOS). |
| [nativeaot-async-folding](nativeaot-async-folding/README.md) | Native AOT method body folding (on by default in .NET 10 Release) takes quadratic time in the number of async methods. |
