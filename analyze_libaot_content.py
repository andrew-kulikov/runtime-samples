#!/usr/bin/env python3
"""
Analyze the content of a specific libaot-*.dll.so file.
Calculates sizes grouped by Namespace and Type.

Requirements:
    pip install pyelftools
    
    OR
    
    have 'nm' or 'readelf' in your PATH (Linux/WSL/Git Bash)
"""

import sys
import os
import re
from collections import defaultdict

try:
    from elftools.elf.elffile import ELFFile
    from elftools.elf.sections import SymbolTableSection
    HAS_PYELFTOOLS = True
except ImportError:
    HAS_PYELFTOOLS = False

def parse_symbol_name(name):
    """
    Parse Mono AOT symbol name into (namespace, type, method).
    Heuristic approach as AOT symbols flatten the hierarchy.
    """
    # Filter out internal Mono symbols
    if name.startswith('mono_') or name.startswith('plt_'):
        return ('[Runtime]', '[Runtime]', name)
    
    if name.startswith('temp_'):
        return ('[Thunks]', '[Thunks]', name)

    # Example: System_Collections_Generic_List_1_Add
    # Example: MauiAotHang_Features_SomeClass1_Calculate
    
    parts = name.split('_')
    if len(parts) < 2:
        return ('[Global]', '[Global]', name)

    # Try to identify the split points
    # This is tricky because namespaces and types both use PascalCase usually
    # We'll try to build up a likely namespace/type split
    
    # Common root namespaces
    common_roots = {'System', 'Microsoft', 'Xamarin', 'Android', 'Java', 'MauiAotHang'}
    
    if parts[0] in common_roots:
        # Heuristic: Look for the transition from Namespace to Type
        # In many cases, the Type is the first component after the known namespace prefix
        # or we can try to guess.
        
        # For now, let's assume the last 1 or 2 parts are Method/Member, 
        # and the one before is Type, and the rest is Namespace.
        
        if len(parts) >= 3:
            method = parts[-1]
            # Handle generic arity suffixes like _1, _2
            if method.isdigit() and len(parts) >= 4:
                 method = parts[-2] + "_" + method
                 type_idx = -3
            else:
                type_idx = -2
                
            type_name = parts[type_idx]
            namespace = '.'.join(parts[:type_idx])
            return (namespace, type_name, method)
    
    # Fallback
    return ('[Unknown]', '[Unknown]', name)

def analyze_with_pyelftools(filepath):
    print(f"Using pyelftools to analyze {os.path.basename(filepath)}...")
    
    with open(filepath, 'rb') as f:
        elf = ELFFile(f)
        symbol_tables = [s for s in elf.iter_sections() if isinstance(s, SymbolTableSection)]
        
        if not symbol_tables:
            print("No symbol tables found.")
            return None

        namespace_sizes = defaultdict(int)
        type_sizes = defaultdict(int)
        total_size = 0

        for section in symbol_tables:
            for symbol in section.iter_symbols():
                if symbol['st_size'] > 0 and symbol['st_info']['type'] == 'STT_FUNC':
                    name = symbol.name
                    size = symbol['st_size']
                    
                    ns, typename, _ = parse_symbol_name(name)
                    
                    namespace_sizes[ns] += size
                    type_sizes[f"{ns}.{typename}"] += size
                    total_size += size

        return namespace_sizes, type_sizes, total_size

def analyze_with_nm(filepath):
    # ... (Previous implementation logic for nm/readelf fallback)
    import subprocess
    
    print(f"Using 'nm' to analyze {os.path.basename(filepath)}...")
    try:
        # Try running nm
        result = subprocess.run(['nm', '--print-size', '--size-sort', '--radix=d', filepath], 
                              capture_output=True, text=True)
        if result.returncode != 0:
            return None
            
        namespace_sizes = defaultdict(int)
        type_sizes = defaultdict(int)
        total_size = 0

        for line in result.stdout.splitlines():
            parts = line.split()
            if len(parts) >= 4:
                try:
                    size = int(parts[1])
                    name = parts[3]
                    
                    ns, typename, _ = parse_symbol_name(name)
                    
                    namespace_sizes[ns] += size
                    type_sizes[f"{ns}.{typename}"] += size
                    total_size += size
                except ValueError:
                    continue
                    
        return namespace_sizes, type_sizes, total_size
        
    except FileNotFoundError:
        return None

def print_results(namespace_sizes, type_sizes, total_size):
    print(f"\nTotal Code Size (Symbols): {total_size:,} bytes ({total_size/1024:.2f} KB)\n")
    
    print("="*80)
    print(f"{'NAMESPACE':<60} {'SIZE (Bytes)':>12} {'%':>6}")
    print("="*80)
    
    sorted_ns = sorted(namespace_sizes.items(), key=lambda x: x[1], reverse=True)
    for ns, size in sorted_ns:
        pct = (size / total_size) * 100 if total_size > 0 else 0
        print(f"{ns:<60} {size:>12,} {pct:>6.1f}")

    print("\n" + "="*80)
    print(f"{'TYPE':<60} {'SIZE (Bytes)':>12} {'%':>6}")
    print("="*80)
    
    sorted_types = sorted(type_sizes.items(), key=lambda x: x[1], reverse=True)[:50]
    for typename, size in sorted_types:
        pct = (size / total_size) * 100 if total_size > 0 else 0
        print(f"{typename:<60} {size:>12,} {pct:>6.1f}")

def main():
    if len(sys.argv) != 2:
        print("Usage: python analyze_libaot_content.py <path_to_libaot.so>")
        sys.exit(1)
        
    filepath = sys.argv[1]
    if not os.path.exists(filepath):
        print(f"File not found: {filepath}")
        sys.exit(1)

    results = None
    
    if HAS_PYELFTOOLS:
        results = analyze_with_pyelftools(filepath)
    
    if results is None:
        results = analyze_with_nm(filepath)
        
    if results is None:
        print("Error: Could not analyze file.")
        if not HAS_PYELFTOOLS:
            print("\nPlease install pyelftools:")
            print("    pip install pyelftools")
            print("\nOr ensure 'nm' (from binutils/Android NDK) is in your PATH.")
        sys.exit(1)

    print_results(*results)

if __name__ == "__main__":
    main()


