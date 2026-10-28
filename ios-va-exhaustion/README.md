# iOS 27 virtual address space exhaustion (.NET 10 Native AOT)

A Native AOT process on iOS 27 throws `OutOfMemoryException` while hundreds of megabytes of physical memory are still available.

Without `com.apple.developer.kernel.extended-virtual-addressing`, iOS 27 leaves the process about 6 GiB of free virtual address space at `0x7000000000`–`0x7180000000` (448–454 GiB). Everything between the low mappings and that window is already mapped. .NET on Apple uses the segments GC. Its card, brick, write-watch and mark tables are one reservation sized from the span between the lowest and highest GC segment. After a segment is placed in the high window, that span is hundreds of gigabytes and the next table reservation does not fit.

`os_proc_available_memory` is the physical budget the system will still let this process use. The failure is that value staying large while the largest free virtual range collapses.

## What it does

On launch the app:

1. Logs `task_info(TASK_VM_INFO)` and `os_proc_available_memory`.
2. Walks every mapping with `mach_vm_region(VM_REGION_BASIC_INFO_64)` and logs each region, each free gap, and the largest gaps. The gap from the last mapping to `task_vm_info.max_address` is included.
3. Allocates 100 MiB, fills it with `0xFF`, keeps it alive, and repeats until the process dies.

Stdout is prefixed with `[va]`. Stream it with `devicectl --console`.

The default entitlements file does not request extended virtual addressing. That is the failing configuration. `Entitlements.ExtendedVa.plist` is the control: pass `-p:ExtendedVirtualAddressing=true`. The Apple team must already have the Extended Virtual Addressing capability. Personal development teams do not, and Xcode refuses to provision it (`Personal development teams do not support the Extended Virtual Addressing capability`).

## Build and run

Xcode 26.6 and the iPhoneOS 26.5 SDK are enough for an iOS 27 device. Publish Release so the app is Native AOT. A Debug or Mono build uses a different GC and will not reproduce this.

```bash
dotnet publish IosVaExhaustion/IosVaExhaustion.csproj -c Release -r ios-arm64 \
  -p:CodesignKey="Apple Development: <name>" \
  -p:CodesignProvision="<provisioning profile name>"

xcrun devicectl device install app --device <udid> \
  IosVaExhaustion/bin/Release/net10.0-ios26.5/ios-arm64/IosVaExhaustion.app
xcrun devicectl device process launch --console --device <udid> \
  com.runtime.samples.iosvaexhaustion
```

## Observed

[logs/iphone12-ios27.0.1.log](logs/iphone12-ios27.0.1.log) is the console from an iPhone 12 (4 GB), iOS 27.0.1, Xcode 26.6, .NET 10.0.12 Native AOT, without the entitlement.

- At start, `os_proc_available_memory` was 2.04 GiB and the largest free range was 6.00 GiB at `0x7000000000`–`0x7180000000`. Virtual size was already 442 GiB. `gc_total_available` was the full 3.60 GiB of device RAM.
- Twelve 100 MiB dirty buffers succeeded (1.17 GiB held). Available physical memory was still about 880 MiB.
- The 12th buffer placed reservations in the high window (`last_end` moved to `0x7108c24000`). The largest free range dropped from 6.00 GiB to 1.86 GiB.
- The next 100 MiB allocation threw `OutOfMemoryException` at a 1.19 GiB footprint. The process then aborted (signal 6).

The extended-virtual-addressing control was not run here. This Mac only has a personal development team, and that team cannot be provisioned for the capability.
