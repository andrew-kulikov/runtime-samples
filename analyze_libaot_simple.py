#!/usr/bin/env python3
"""
Simple script to analyze libaot file sizes and extract basic information.
Works on Windows with minimal dependencies.
"""

import os
import sys
from pathlib import Path
from collections import defaultdict

def get_file_size(filepath: str) -> int:
    """Get file size in bytes."""
    return os.path.getsize(filepath)

def extract_assembly_name(filename: str) -> str:
    """Extract assembly name from libaot-<AssemblyName>.dll.so"""
    if filename.startswith('libaot-'):
        name = filename[7:]  # Remove 'libaot-' prefix
        if name.endswith('.dll.so'):
            name = name[:-7]  # Remove '.dll.so' suffix
        elif name.endswith('.so'):
            name = name[:-3]  # Remove '.so' suffix
        return name
    return filename

def main():
    if len(sys.argv) < 2:
        print("Usage: python analyze_libaot_simple.py <directory-with-libaot-files>")
        print("   or: python analyze_libaot_simple.py <libaot-file.so> [<libaot-file2.so> ...]")
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
    
    print(f"Found {len(files_to_analyze)} libaot file(s)\n")
    
    # Group by assembly
    assembly_sizes = defaultdict(int)
    file_details = []
    
    for filepath in files_to_analyze:
        size = get_file_size(filepath)
        filename = os.path.basename(filepath)
        assembly = extract_assembly_name(filename)
        
        assembly_sizes[assembly] += size
        file_details.append({
            'file': filename,
            'assembly': assembly,
            'size': size
        })
    
    # Print summary
    print("="*80)
    print("ASSEMBLY SIZES (sorted by size):")
    print("="*80)
    total_size = 0
    for assembly, size in sorted(assembly_sizes.items(), key=lambda x: x[1], reverse=True):
        total_size += size
        percentage = (size / sum(assembly_sizes.values())) * 100 if assembly_sizes else 0
        print(f"  {assembly:60} {size:>12,} bytes ({size / 1024:>8.2f} KB) ({percentage:>5.1f}%)")
    
    print(f"\n  {'TOTAL':60} {total_size:>12,} bytes ({total_size / 1024:>8.2f} KB)")
    
    print("\n" + "="*80)
    print("INDIVIDUAL FILES (sorted by size):")
    print("="*80)
    for detail in sorted(file_details, key=lambda x: x['size'], reverse=True):
        print(f"  {detail['file']:60} {detail['size']:>12,} bytes ({detail['size'] / 1024:>8.2f} KB)")

if __name__ == "__main__":
    main()



