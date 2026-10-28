# [NativeAOT] iOS 27 GC reports OOM while physical memory remains, after a segment is placed in the high VA window

### Description

On iOS 27, a process without `com.apple.developer.kernel.extended-virtual-addressing` has a small amount of usable virtual address space. On an iPhone 12 the largest free range at startup is 6.00 GiB at `0x7000000000`–`0x7180000000` (448–454 GiB). The address space between the ordinary mappings and that window is already occupied, so `task_vm_info.virtual_size` is about 442 GiB before the app allocates anything.

.NET on Apple uses the segments GC (regions are not compiled in). The card, brick, write-watch and concurrent-GC mark tables are reserved as one block sized from the span between the lowest and highest GC segment, on the order of 0.9% of that span. The GC also treats `GCMemoryInfo.TotalAvailableMemoryBytes` as the device's physical RAM. It does not know about the iOS per-process VA ceiling.

Once a segment is placed in the 448 GiB window, the span becomes hundreds of gigabytes. The next table reservation does not fit in what remains of that window, and the runtime throws `OutOfMemoryException` while `os_proc_available_memory` still reports hundreds of megabytes.

### Reproduction steps

Sample: https://github.com/andrew-kulikov/runtime-samples/tree/master/ios-va-exhaustion

```bash
git clone https://github.com/andrew-kulikov/runtime-samples
cd runtime-samples/ios-va-exhaustion
dotnet publish IosVaExhaustion/IosVaExhaustion.csproj -c Release -r ios-arm64 \
  -p:CodesignKey="Apple Development: <name>" \
  -p:CodesignProvision="<profile>"
xcrun devicectl device install app --device <udid> \
  IosVaExhaustion/bin/Release/net10.0-ios26.5/ios-arm64/IosVaExhaustion.app
xcrun devicectl device process launch --console --device <udid> \
  com.runtime.samples.iosvaexhaustion
```

Publish Release. Native AOT is required. A Mono or interpreter build will not reproduce it. Xcode 26.6 with the iPhoneOS 26.5 SDK can install onto iOS 27.

The app logs `task_info(TASK_VM_INFO)`, `os_proc_available_memory`, and every `mach_vm_region` mapping, then retains dirty 100 MiB buffers until it dies. Captured output: `logs/iphone12-ios27.0.1.log`.

### Expected behavior

The process should keep allocating until the OS stops it for physical memory (jetsam), with `os_proc_available_memory` near zero. A free virtual range of several gigabytes should be enough to satisfy the next GC reservation. `TotalAvailableMemoryBytes` should not be reported as the full device RAM when the process cannot actually reserve that much address space.

Enabling `com.apple.developer.kernel.extended-virtual-addressing` raises the VA ceiling (to 512 GiB on this OS) and is the OS-level workaround. A personal development team cannot provision that capability, so this sample does not include a device log of that configuration.

### Actual behavior

iPhone 12, 4 GB RAM, iOS 27.0.1, .NET 10.0.12 Native AOT, no extended-virtual-addressing entitlement:

| held | os_proc_available_memory | phys_footprint | largest free VA |
|---:|---:|---:|---|
| 0 | 2.04 GiB | 6 MiB | 6.00 GiB at `0x7000000000`–`0x7180000000` |
| 1.17 GiB (12 x 100 MiB) | ~880 MiB | 1.19 GiB | 1.86 GiB at `0x7108c24000`–`0x7180000000` |
| next 100 MiB | 880 MiB | 1.19 GiB | `OutOfMemoryException`, then abort (signal 6) |

`gc_total_available` stayed 3.60 GiB (device RAM) for the whole run. Virtual size was 442 GiB before the first allocation. The 12th buffer is what moved `last_end` from `0x7000000000` into the high window.

### Configuration

- .NET SDK 10.0.401, runtime 10.0.12, `PublishAot=true`, `ios-arm64`
- Xcode 26.6, iPhoneOS 26.5 SDK, iPhone 12, iOS 27.0.1
- No `com.apple.developer.kernel.extended-virtual-addressing` entitlement
