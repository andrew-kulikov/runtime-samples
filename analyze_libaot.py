#!/usr/bin/env python3
"""
Analyze libaot-*.dll.so files to find which types/namespaces take the most space.

This script uses readelf/objdump to extract symbol information from AOT-compiled
Mono libraries and attempts to map symbols back to .NET types/namespaces.
"""

import subprocess
import sys
import os
import re
from pathlib import Path
from collections import defaultdict
from typing import Dict, List, Tuple

def run_command(cmd: List[str]) -> str:
    """Run a command and return its output."""
    try:
        result = subprocess.run(cmd, capture_output=True, text=True, check=True)
        return result.stdout
    except subprocess.CalledProcessError as e:
        print(f"Error running command: {' '.join(cmd)}", file=sys.stderr)
        print(f"Error: {e.stderr}", file=sys.stderr)
        return ""
    except FileNotFoundError:
        print(f"Command not found: {cmd[0]}", file=sys.stderr)
        print("Please install binutils (readelf, objdump, nm) or use WSL/Git Bash on Windows", file=sys.stderr)
        return ""

def get_file_size(filepath: str) -> int:
    """Get file size in bytes."""
    return os.path.getsize(filepath)

def extract_symbols_with_readelf(filepath: str) -> List[Tuple[str, int, str]]:
    """
    Extract symbols from ELF file using readelf.
    Returns list of (symbol_name, size, section) tuples.
    """
    # Get symbol table
    output = run_command(["readelf", "-s", "-W", filepath])
    symbols = []
    
    # Parse readelf output
    # Format: Num:    Value          Size Type    Bind   Vis      Ndx Name
    lines = output.split('\n')
    for line in lines:
        if not line.strip() or 'Num:' in line:
            continue
        
        parts = line.split()
        if len(parts) >= 8:
            try:
                size = int(parts[2], 16) if parts[2].startswith('0x') else int(parts[2])
                sym_type = parts[3]
                name = ' '.join(parts[7:])
                
                # Only include FUNC and OBJECT symbols with size > 0
                if sym_type in ['FUNC', 'OBJECT'] and size > 0:
                    symbols.append((name, size, sym_type))
            except (ValueError, IndexError):
                continue
    
    return symbols

def extract_symbols_with_nm(filepath: str) -> List[Tuple[str, int, str]]:
    """
    Extract symbols from ELF file using nm.
    Returns list of (symbol_name, size, type) tuples.
    """
    output = run_command(["nm", "-S", "--size-sort", filepath])
    symbols = []
    
    # Parse nm output
    # Format: address size type name
    for line in output.split('\n'):
        if not line.strip():
            continue
        
        parts = line.split()
        if len(parts) >= 4:
            try:
                size = int(parts[1], 16) if parts[1].startswith('0x') else int(parts[1])
                sym_type = parts[2]
                name = ' '.join(parts[3:])
                
                if size > 0:
                    symbols.append((name, size, sym_type))
            except (ValueError, IndexError):
                continue
    
    return symbols

def extract_section_sizes(filepath: str) -> Dict[str, int]:
    """Extract section sizes from ELF file."""
    output = run_command(["readelf", "-S", "-W", filepath])
    sections = {}
    
    lines = output.split('\n')
    for line in lines:
        if not line.strip() or '[' in line and ']' in line:
            parts = line.split()
            if len(parts) >= 5:
                try:
                    section_name = parts[1].strip('[]')
                    size = int(parts[5], 16) if '0x' in parts[5] else int(parts[5])
                    sections[section_name] = size
                except (ValueError, IndexError):
                    continue
    
    return sections

def parse_dotnet_symbol(symbol: str) -> Tuple[str, str, str]:
    """
    Parse a Mono AOT symbol name to extract namespace, type, and member.
    
    Mono AOT symbols typically follow patterns like:
    - mono_aot_module_<assembly>_init
    - <namespace>_<type>_<method>
    - <type>_<method>
    """
    namespace = ""
    type_name = ""
    member = ""
    
    # Skip common prefixes
    if symbol.startswith('mono_aot_'):
        return ("[Mono Runtime]", symbol, "")
    
    # Try to extract namespace.type::member pattern
    # Common patterns in Mono AOT:
    # - System_Collections_Generic_List_1_Add
    # - GeneratedClasses_SomeClass1_Calculate
    
    parts = symbol.split('_')
    
    # Try to find type boundary (usually capitalized after namespace)
    # This is heuristic-based
    if len(parts) > 1:
        # Look for common .NET namespaces
        namespace_parts = []
        type_parts = []
        member_parts = []
        
        i = 0
        # Collect namespace parts (usually lowercase)
        while i < len(parts) and parts[i][0].islower() if parts[i] else False:
            namespace_parts.append(parts[i])
            i += 1
        
        # Collect type parts (usually starts with capital)
        while i < len(parts) and (parts[i][0].isupper() if parts[i] else False):
            type_parts.append(parts[i])
            i += 1
        
        # Rest is member
        member_parts = parts[i:]
        
        namespace = '.'.join(namespace_parts) if namespace_parts else ""
        type_name = '_'.join(type_parts) if type_parts else symbol
        member = '_'.join(member_parts) if member_parts else ""
    
    return (namespace, type_name, member)

def analyze_libaot_file(filepath: str) -> Dict:
    """Analyze a single libaot file."""
    print(f"\nAnalyzing: {os.path.basename(filepath)}")
    print(f"File size: {get_file_size(filepath):,} bytes ({get_file_size(filepath) / 1024:.2f} KB)")
    
    # Get section sizes
    sections = extract_section_sizes(filepath)
    if sections:
        print("\nSection sizes:")
        for name, size in sorted(sections.items(), key=lambda x: x[1], reverse=True)[:10]:
            print(f"  {name}: {size:,} bytes ({size / 1024:.2f} KB)")
    
    # Try to get symbols
    symbols = []
    if run_command(["which", "readelf"]):
        symbols = extract_symbols_with_readelf(filepath)
    elif run_command(["which", "nm"]):
        symbols = extract_symbols_with_nm(filepath)
    
    if not symbols:
        print("\nCould not extract symbols. Install binutils (readelf/nm) to get detailed analysis.")
        return {
            'file': filepath,
            'size': get_file_size(filepath),
            'sections': sections,
            'symbols': []
        }
    
    print(f"\nFound {len(symbols)} symbols")
    
    # Group by namespace and type
    namespace_sizes = defaultdict(int)
    type_sizes = defaultdict(int)
    symbol_details = []
    
    for symbol, size, sym_type in symbols:
        namespace, type_name, member = parse_dotnet_symbol(symbol)
        
        if namespace:
            namespace_sizes[namespace] += size
        if type_name:
            type_key = f"{namespace}.{type_name}" if namespace else type_name
            type_sizes[type_key] += size
        
        symbol_details.append({
            'symbol': symbol,
            'size': size,
            'type': sym_type,
            'namespace': namespace,
            'type_name': type_name,
            'member': member
        })
    
    return {
        'file': filepath,
        'size': get_file_size(filepath),
        'sections': sections,
        'symbols': symbol_details,
        'namespace_sizes': dict(namespace_sizes),
        'type_sizes': dict(type_sizes)
    }

def main():
    if len(sys.argv) < 2:
        print("Usage: python analyze_libaot.py <libaot-file.so> [<libaot-file2.so> ...]")
        print("   or: python analyze_libaot.py <directory-with-libaot-files>")
        sys.exit(1)
    
    files_to_analyze = []
    
    # Collect files
    for arg in sys.argv[1:]:
        path = Path(arg)
        if path.is_file():
            if path.suffix == '.so' or 'libaot' in path.name:
                files_to_analyze.append(str(path))
        elif path.is_dir():
            # Find all libaot-*.so files in directory
            for so_file in path.rglob('libaot-*.so'):
                files_to_analyze.append(str(so_file))
    
    if not files_to_analyze:
        print("No libaot-*.so files found!")
        sys.exit(1)
    
    print(f"Found {len(files_to_analyze)} file(s) to analyze\n")
    
    all_results = []
    total_namespace_sizes = defaultdict(int)
    total_type_sizes = defaultdict(int)
    
    for filepath in files_to_analyze:
        result = analyze_libaot_file(filepath)
        all_results.append(result)
        
        # Aggregate namespace and type sizes
        for ns, size in result.get('namespace_sizes', {}).items():
            total_namespace_sizes[ns] += size
        for type_key, size in result.get('type_sizes', {}).items():
            total_type_sizes[type_key] += size
    
    # Print summary
    if total_namespace_sizes:
        print("\n" + "="*80)
        print("TOP NAMESPACES BY SIZE (across all files):")
        print("="*80)
        for namespace, size in sorted(total_namespace_sizes.items(), key=lambda x: x[1], reverse=True)[:20]:
            print(f"  {namespace or '(no namespace)':50} {size:>12,} bytes ({size / 1024:>8.2f} KB)")
    
    if total_type_sizes:
        print("\n" + "="*80)
        print("TOP TYPES BY SIZE (across all files):")
        print("="*80)
        for type_key, size in sorted(total_type_sizes.items(), key=lambda x: x[1], reverse=True)[:30]:
            print(f"  {type_key:60} {size:>12,} bytes ({size / 1024:>8.2f} KB)")
    
    # Print per-file summary
    print("\n" + "="*80)
    print("FILE SIZE SUMMARY:")
    print("="*80)
    for result in sorted(all_results, key=lambda x: x['size'], reverse=True):
        filename = os.path.basename(result['file'])
        size = result['size']
        print(f"  {filename:60} {size:>12,} bytes ({size / 1024:>8.2f} KB)")

if __name__ == "__main__":
    main()



