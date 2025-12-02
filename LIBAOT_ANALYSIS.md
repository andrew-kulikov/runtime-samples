# Analyzing libaot-*.dll.so Files

This document explains how to analyze Mono AOT-compiled libraries (`libaot-*.dll.so` files) to understand their content and identify which types/namespaces consume the most space.

## Overview

When you build a .NET MAUI Android app with AOT compilation enabled (`<RunAOTCompilation>true</RunAOTCompilation>`), Mono compiles your .NET assemblies into native shared libraries. Each assembly typically becomes a `libaot-<AssemblyName>.dll.so` file.

## Methods to Analyze libaot Files

### Method 1: Simple File Size Analysis (Works on Windows)

The simplest approach is to analyze file sizes:

```bash
python analyze_libaot_simple.py MauiAotHang\bin\Release\net9.0-android\android-arm64\publish\com.companyname.mauiaothang-Signed\lib\arm64-v8a
```

This will show you which assemblies take the most space.

### Method 2: Symbol Analysis (Requires Linux/WSL/Git Bash)

For detailed analysis of symbols and types, use the full analyzer:

```bash
python analyze_libaot.py MauiAotHang\bin\Release\net9.0-android\android-arm64\publish\com.companyname.mauiaothang-Signed\lib\arm64-v8a\libaot-MauiAotHang.Features.dll.so
```

**Requirements:**
- `readelf` or `nm` (from binutils)
- On Windows: Use WSL, Git Bash, or install binutils

### Method 3: Using readelf Directly

```bash
# List all symbols with sizes
readelf -s -W libaot-MauiAotHang.Features.dll.so | sort -k3 -n -r

# Show section sizes
readelf -S -W libaot-MauiAotHang.Features.dll.so

# Show file header
readelf -h libaot-MauiAotHang.Features.dll.so
```

### Method 4: Using objdump

```bash
# Disassemble and show symbols
objdump -t libaot-MauiAotHang.Features.dll.so

# Show section headers
objdump -h libaot-MauiAotHang.Features.dll.so

# Disassemble code (very verbose)
objdump -d libaot-MauiAotHang.Features.dll.so
```

### Method 5: Using nm

```bash
# List symbols sorted by size
nm -S --size-sort libaot-MauiAotHang.Features.dll.so

# Show only defined symbols with sizes
nm -S -D libaot-MauiAotHang.Features.dll.so | sort -k2 -n -r
```

## Understanding Mono AOT Symbol Names

Mono AOT symbols follow patterns that can help identify .NET types:

- **Namespace_Type_Method**: `System_Collections_Generic_List_1_Add`
- **Type_Method**: `SomeClass1_Calculate`
- **Mono runtime**: `mono_aot_module_*`, `mono_aot_*`

The scripts attempt to parse these patterns to extract namespaces and types.

## Finding Large Types/Namespaces

1. **By Assembly**: Use `analyze_libaot_simple.py` to see which assemblies are largest
2. **By Symbol**: Use `analyze_libaot.py` to see individual symbols and their sizes
3. **By Namespace**: The full analyzer groups symbols by namespace
4. **By Type**: The full analyzer groups symbols by type

## Windows-Specific Notes

On Windows, you have several options:

1. **WSL (Windows Subsystem for Linux)**: Install binutils in WSL
   ```bash
   sudo apt-get install binutils
   ```

2. **Git Bash**: May include some Unix tools

3. **Cygwin/MSYS2**: Install binutils package

4. **Simple Analysis**: Use `analyze_libaot_simple.py` which only needs Python

## Example Output

```
Analyzing: libaot-MauiAotHang.Features.dll.so
File size: 15,234,567 bytes (14,526.34 KB)

Section sizes:
  .text: 12,345,678 bytes (11,789.23 KB)
  .rodata: 1,234,567 bytes (1,205.23 KB)
  .data: 654,321 bytes (638.59 KB)

Found 1,234 symbols

TOP NAMESPACES BY SIZE:
  GeneratedClasses                                   1,234,567 bytes (1,205.23 KB)
  System.Collections.Generic                            567,890 bytes (  554.58 KB)
  System.Threading.Tasks                               345,678 bytes (  337.58 KB)

TOP TYPES BY SIZE:
  GeneratedClasses.SomeClass1                          123,456 bytes (  120.56 KB)
  GeneratedClasses.SomeClass2                          111,234 bytes (  108.63 KB)
```

## Limitations

1. **Symbol Name Parsing**: Mono AOT symbol names don't always perfectly map to .NET types. The parsing is heuristic-based.

2. **Metadata**: Some information (like exact method names) may be lost in AOT compilation.

3. **Inlining**: Methods may be inlined, making it harder to attribute size to specific types.

4. **Native Code**: The actual native code size may differ from the original IL size.

## Tips for Reducing Size

1. **Use Trimming**: Enable `<PublishTrimmed>true</PublishTrimmed>` to remove unused code
2. **Linker Configuration**: Use `linker.xml` to preserve only what you need
3. **Remove Unused Assemblies**: Check which assemblies are actually needed
4. **Profile-Guided AOT**: Use `<AndroidEnableProfiledAot>true</AndroidEnableProfiledAot>` for better optimization

## Additional Tools

- **ILSpy**: Analyze the original .NET assemblies before AOT
- **dotnet-dump**: Analyze .NET dumps (not directly for libaot files)
- **Mono AOT Compiler Options**: Check Mono documentation for AOT-specific options



