using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace IosVaExhaustion;

static class Probe
{
    const int ChunkMegabytes = 100;
    const int KernSuccess = 0;
    const int MaxRegions = 100_000;

    static readonly List<byte[]> Held = new();

    public static void Run(Action<string> status)
    {
        Log($"start .NET {Environment.Version} framework={RuntimeInformation.FrameworkDescription} os={UIDevice.CurrentDevice.SystemVersion} model={UIDevice.CurrentDevice.Model} dynamic_code={RuntimeFeature.IsDynamicCodeSupported}");
        status("starting");

        var step = 0;
        while (true)
        {
            var heldMb = step * ChunkMegabytes;
            status($"step {step}\nheld {heldMb} MiB");
            LogMap($"before step {step} held {heldMb} MiB");

            Log($"allocating {ChunkMegabytes} MiB");
            byte[] bytes;
            try
            {
                bytes = new byte[ChunkMegabytes * 1024 * 1024];
            }
            catch (OutOfMemoryException ex)
            {
                Log($"MANAGED OOM at step {step} held {heldMb} MiB: {ex.Message}");
                LogMap("after managed OOM");
                status($"managed OOM\nheld {heldMb} MiB");
                Thread.Sleep(1000);
                Environment.FailFast($"managed OOM at step {step}", ex);
                return;
            }

            bytes.AsSpan().Fill(0xFF);
            Held.Add(bytes);
            step++;
            var available = (ulong)Native.os_proc_available_memory();
            Log($"dirtied step {step} held {step * ChunkMegabytes} MiB os_proc_available_memory={Format(available)} managed={Format((ulong)GC.GetTotalMemory(false))}");
            status($"held {step * ChunkMegabytes} MiB\navailable {Format(available)}");
            Thread.Sleep(250);
        }
    }

    static void LogMap(string phase)
    {
        Log($"===== {phase} =====");
        if (!TryReadTaskVmInfo(out var info))
            return;

        var available = (ulong)Native.os_proc_available_memory();
        var physical = NSProcessInfo.ProcessInfo.PhysicalMemory;
        var gc = GC.GetGCMemoryInfo();
        Log(
            $"SUMMARY os_proc_available_memory={Format(available)} phys_footprint={Format(info.PhysFootprint)} " +
            $"resident={Format(info.ResidentSize)} resident_peak={Format(info.ResidentSizePeak)} " +
            $"device_ram={Format(physical)} virtual={Format(info.VirtualSize)} region_count={info.RegionCount} " +
            $"page={info.PageSize} min=0x{info.MinAddress:x} max=0x{info.MaxAddress:x} " +
            $"gc_heap={Format((ulong)gc.HeapSizeBytes)} gc_committed={Format((ulong)gc.TotalCommittedBytes)} " +
            $"gc_total_available={Format((ulong)gc.TotalAvailableMemoryBytes)} managed={Format((ulong)GC.GetTotalMemory(false))}");

        Span<Gap> largestGaps = stackalloc Gap[8];
        var stats = Walk(logEach: false, largestGaps, info.MaxAddress);
        Log(
            $"FREE_SUMMARY walked={stats.Walked} end_code={stats.EndCode} " +
            $"first=0x{stats.First:x} last_end=0x{stats.LastEnd:x} " +
            $"largest_free={Format(stats.Largest)} total_free_between_regions={Format(stats.TotalFree)}");
        LogLargest(largestGaps);

        largestGaps.Clear();
        Walk(logEach: true, largestGaps, info.MaxAddress);
        Log($"===== end {phase} =====");
    }

    static bool TryReadTaskVmInfo(out TaskVmInfoData info)
    {
        info = new TaskVmInfoData();
        var count = (uint)(Marshal.SizeOf<TaskVmInfoData>() / sizeof(uint));
        var result = Native.task_info(Native.mach_task_self(), Native.TaskVmInfo, ref info, ref count);
        if (result == KernSuccess)
            return true;

        Log($"task_info(TASK_VM_INFO) failed code={result} count={count}");
        return false;
    }

    static WalkStats Walk(bool logEach, Span<Gap> largestGaps, ulong maxAddress)
    {
        var stats = new WalkStats();
        var task = Native.mach_task_self();
        ulong address = 0;
        ulong previousEnd = 0;
        var havePrevious = false;
        var info = new VmRegionBasicInfo64Data();

        while (stats.Walked < MaxRegions)
        {
            var infoCount = Native.VmRegionBasicInfo64Count;
            var result = Native.mach_vm_region(task, ref address, out var size, Native.VmRegionBasicInfo64, ref info, ref infoCount, out var objectName);
            if (objectName != 0)
                Native.mach_port_deallocate(task, objectName);

            if (result != KernSuccess || size == 0 || address + size < address)
            {
                stats.EndCode = result;
                break;
            }

            if (!havePrevious)
            {
                stats.First = address;
                havePrevious = true;
            }
            else if (address > previousEnd)
            {
                NoteGap(largestGaps, ref stats, previousEnd, address - previousEnd, logEach);
            }

            var end = address + size;
            if (logEach)
                Log($"MAP  0x{address:x}-0x{end:x} size={Format(size)} prot={Prot(info.Protection)} maxprot={Prot(info.MaxProtection)} shared={info.Shared}");

            previousEnd = end;
            stats.LastEnd = end;
            address = end;
            stats.Walked++;
        }

        if (havePrevious && maxAddress > previousEnd)
            NoteGap(largestGaps, ref stats, previousEnd, maxAddress - previousEnd, logEach);

        return stats;
    }

    static void NoteGap(Span<Gap> largestGaps, ref WalkStats stats, ulong start, ulong size, bool logEach)
    {
        stats.TotalFree += size;
        if (size > stats.Largest)
            stats.Largest = size;
        Consider(largestGaps, start, size);
        if (logEach)
            Log($"FREE 0x{start:x}-0x{start + size:x} size={Format(size)}");
    }

    static void Consider(Span<Gap> top, ulong start, ulong size)
    {
        var slot = 0;
        for (var i = 1; i < top.Length; i++)
        {
            if (top[i].Size < top[slot].Size)
                slot = i;
        }

        if (size > top[slot].Size)
            top[slot] = new Gap { Start = start, Size = size };
    }

    static void LogLargest(Span<Gap> top)
    {
        for (var i = 0; i < top.Length; i++)
        {
            var best = i;
            for (var j = i + 1; j < top.Length; j++)
            {
                if (top[j].Size > top[best].Size)
                    best = j;
            }

            (top[i], top[best]) = (top[best], top[i]);
        }

        for (var i = 0; i < top.Length; i++)
        {
            if (top[i].Size == 0)
                break;
            Log($"LARGEST_FREE[{i}] 0x{top[i].Start:x}-0x{top[i].Start + top[i].Size:x} size={Format(top[i].Size)}");
        }
    }

    static string Prot(int protection)
    {
        var read = (protection & 1) != 0 ? 'r' : '-';
        var write = (protection & 2) != 0 ? 'w' : '-';
        var execute = (protection & 4) != 0 ? 'x' : '-';
        return $"{read}{write}{execute}";
    }

    static string Format(ulong bytes)
    {
        const double kib = 1024d;
        const double mib = 1024d * 1024d;
        const double gib = 1024d * 1024d * 1024d;
        if (bytes >= (ulong)gib)
            return $"{bytes / gib:0.00} GiB";
        if (bytes >= (ulong)mib)
            return $"{bytes / mib:0.00} MiB";
        if (bytes >= (ulong)kib)
            return $"{bytes / kib:0.00} KiB";
        return $"{bytes} B";
    }

    static void Log(string message)
    {
        Console.WriteLine($"[va] {message}");
        Console.Out.Flush();
    }

    struct Gap
    {
        public ulong Start;
        public ulong Size;
    }

    struct WalkStats
    {
        public int Walked;
        public int EndCode;
        public ulong First;
        public ulong LastEnd;
        public ulong Largest;
        public ulong TotalFree;
    }
}

static partial class Native
{
    public const int TaskVmInfo = 22;
    public const int VmRegionBasicInfo64 = 9;
    public const uint VmRegionBasicInfo64Count = 9;

    [LibraryImport(ObjCRuntime.Constants.SystemLibrary)]
    public static partial int task_info(uint targetTask, int flavor, ref TaskVmInfoData taskInfo, ref uint taskInfoCount);

    [LibraryImport(ObjCRuntime.Constants.SystemLibrary)]
    public static partial int mach_vm_region(uint targetTask, ref ulong address, out ulong size, int flavor, ref VmRegionBasicInfo64Data info, ref uint infoCount, out uint objectName);

    [LibraryImport(ObjCRuntime.Constants.SystemLibrary)]
    public static partial int mach_port_deallocate(uint task, uint name);

    [LibraryImport(ObjCRuntime.Constants.SystemLibrary)]
    public static partial uint mach_task_self();

    [LibraryImport(ObjCRuntime.Constants.SystemLibrary)]
    public static partial nuint os_proc_available_memory();
}

[StructLayout(LayoutKind.Sequential)]
struct TaskVmInfoData
{
    public ulong VirtualSize;
    public int RegionCount;
    public int PageSize;
    public ulong ResidentSize;
    public ulong ResidentSizePeak;
    public ulong Device;
    public ulong DevicePeak;
    public ulong Internal;
    public ulong InternalPeak;
    public ulong External;
    public ulong ExternalPeak;
    public ulong Reusable;
    public ulong ReusablePeak;
    public ulong PurgeableVolatilePmap;
    public ulong PurgeableVolatileResident;
    public ulong PurgeableVolatileVirtual;
    public ulong Compressed;
    public ulong CompressedPeak;
    public ulong CompressedLifetime;
    public ulong PhysFootprint;
    public ulong MinAddress;
    public ulong MaxAddress;
}

// vm_region_basic_info_data_64_t. MIG delivers it under #pragma pack(4); VM_REGION_BASIC_INFO_COUNT_64 is 9.
[StructLayout(LayoutKind.Sequential, Pack = 4)]
struct VmRegionBasicInfo64Data
{
    public int Protection;
    public int MaxProtection;
    public uint Inheritance;
    public int Shared;
    public int Reserved;
    public ulong Offset;
    public int Behavior;
    public ushort UserWiredCount;
}
