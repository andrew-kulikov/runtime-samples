# iOS 27 virtual address space exhaustion (.NET 10 Native AOT)

A Native AOT process on iOS 27 throws `OutOfMemoryException` while hundreds of megabytes of physical memory are still available.

On iOS 27, a process without `com.apple.developer.kernel.extended-virtual-addressing` has little usable virtual address space:

- On an iPhone 12 the largest free range at startup is 6.00 GiB at `0x7000000000`–`0x7180000000` (448–454 GiB).
- The address space between the ordinary mappings and that window is already occupied.
- `task_vm_info.virtual_size` is about 442 GiB before the app allocates anything.

.NET on Apple uses the segments GC (regions are not compiled in):

- The card, brick, write-watch and concurrent-GC mark tables are one reservation, sized from the span between the lowest and highest GC segment (about 0.9% of that span).
- `GCMemoryInfo.TotalAvailableMemoryBytes` is reported as the device's physical RAM.
- The GC does not account for the iOS per-process VA ceiling.

Once a segment is placed in the 448 GiB window:

- The span becomes hundreds of gigabytes.
- The next table reservation does not fit in what remains of that window.
- The runtime throws `OutOfMemoryException` while `os_proc_available_memory` still reports hundreds of megabytes.

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

## Expected

- The process keeps allocating until the OS stops it for physical memory (jetsam), with `os_proc_available_memory` near zero.
- A free virtual range of several gigabytes is enough for the next GC reservation.
- `TotalAvailableMemoryBytes` is not reported as the full device RAM when the process cannot reserve that much address space.

`com.apple.developer.kernel.extended-virtual-addressing` raises the VA ceiling (to 512 GiB on this OS) and is the OS-level workaround. A personal development team cannot provision that capability, so this sample has no device log of that configuration.

## Observed

The device log is an iPhone 12 (4 GB), iOS 27.0.1, Xcode 26.6, .NET 10.0.12 Native AOT, without the entitlement. The same failure showed up in production on every device running iOS 27, including high-RAM phones such as iPhone 17 Pro Max:

- Managed `OutOfMemoryException` with a heap of roughly 340–500 MiB, independent of device RAM.
- More than 1 GiB of physical memory still available. Footprint matched iOS 26.
- iOS 26 on the same build, and the previous runtime on iOS 27, did not show this.

| held | os_proc_available_memory | phys_footprint | largest free VA |
|---:|---:|---:|---|
| 0 | 2.04 GiB | 6 MiB | 6.00 GiB at `0x7000000000`–`0x7180000000` |
| 1.17 GiB (12 x 100 MiB) | ~880 MiB | 1.19 GiB | 1.86 GiB at `0x7108c24000`–`0x7180000000` |
| next 100 MiB | 880 MiB | 1.19 GiB | `OutOfMemoryException`, then abort (signal 6) |

- `gc_total_available` stayed 3.60 GiB (device RAM) for the whole run.
- Virtual size was 442 GiB before the first allocation.
- The 12th buffer moved `last_end` from `0x7000000000` into the high window.

Full console log, including every `mach_vm_region` line: [logs/iphone12-ios27.0.1.log](logs/iphone12-ios27.0.1.log). The lines that matter:

Startup. Native AOT (`dynamic_code=False`), 2 GiB of physical budget, 442 GiB already virtual, and the 6 GiB hole is the largest free range. `gc_total_available` is the whole 3.60 GiB of device RAM.

```text
[va] start .NET 10.0.12 framework=.NET 10.0.12 os=27.0.1 model=iPhone dynamic_code=False
[va] SUMMARY os_proc_available_memory=2.04 GiB phys_footprint=6.03 MiB ... device_ram=3.60 GiB virtual=442.57 GiB region_count=105 ... min=0x100958000 max=0x7180000000 gc_heap=0 B gc_total_available=3.60 GiB
[va] FREE_SUMMARY walked=105 ... last_end=0x7000000000 largest_free=6.00 GiB total_free_between_regions=7.42 GiB
[va] LARGEST_FREE[0] 0x7000000000-0x7180000000 size=6.00 GiB
[va] LARGEST_FREE[1] 0x125670000-0x16f3a8000 size=1.15 GiB
```

Before that hole, one reserved mapping covers 384 GiB. That is why a new GC segment cannot be placed between the low mappings and 448 GiB:

```text
[va] MAP  0x1000000000-0x7000000000 size=384.00 GiB prot=--- maxprot=--- shared=0
[va] FREE 0x7000000000-0x7180000000 size=6.00 GiB
```

After the 12th 100 MiB buffer (1.17 GiB held, 881 MiB still available), the same window contains 128 MiB reservations and only 1.86 GiB is left. The next 100 MiB allocation throws. `gc_total_available` is still 3.60 GiB.

```text
[va] dirtied step 12 held 1200 MiB os_proc_available_memory=880.83 MiB managed=1.17 GiB
[va] SUMMARY os_proc_available_memory=880.79 MiB phys_footprint=1.19 GiB ... virtual=448.00 GiB region_count=187 ... gc_heap=500.06 MiB gc_total_available=3.60 GiB managed=1.17 GiB
[va] FREE_SUMMARY ... last_end=0x7108c24000 largest_free=1.86 GiB
[va] LARGEST_FREE[0] 0x7108c24000-0x7180000000 size=1.86 GiB
[va] LARGEST_FREE[1] 0x167f74000-0x16f3a8000 size=116.20 MiB
[va] MAP  0x7000000000-0x7000008000 size=32.00 KiB prot=rw- maxprot=rwx shared=0
[va] MAP  0x7000008000-0x7006408000 size=100.00 MiB prot=rw- maxprot=rwx shared=0
[va] MAP  0x7008000000-0x7010000000 size=128.00 MiB prot=rw- maxprot=rwx shared=0
[va] MAP  0x7100000000-0x7108000000 size=128.00 MiB prot=--- maxprot=rwx shared=0
[va] MAP  0x7108000000-0x7108c24000 size=12.14 MiB prot=--- maxprot=rwx shared=0
[va] FREE 0x7108c24000-0x7180000000 size=1.86 GiB
[va] MANAGED OOM at step 12 held 1200 MiB: Insufficient memory to continue the execution of the program.
[va] SUMMARY os_proc_available_memory=880.68 MiB phys_footprint=1.19 GiB ... gc_heap=1.17 GiB gc_committed=1.17 GiB gc_total_available=3.60 GiB
```

The extended-virtual-addressing control was not run here. This Mac only has a personal development team, and that team cannot be provisioned for the capability.

Issue text ready to paste: [ISSUE.md](ISSUE.md).
