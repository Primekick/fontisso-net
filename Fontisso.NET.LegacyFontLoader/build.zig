const std = @import("std");

pub fn build(b: *std.Build) void {
    const target = b.resolveTargetQuery(.{
        .cpu_arch = .x86,
        .os_tag = .windows,
        .abi = .gnu,
        .os_version_min = .{ .windows = .xp }
    });
    const dll = b.addSharedLibrary(.{
        .root_source_file = b.path("main.zig"),
        .name = "Fontisso.NET.LegacyFontLoader",
        .target = target,
        .optimize = std.builtin.OptimizeMode.ReleaseSmall,
        .link_libc = false,
    });
    
    dll.entry = .{ .symbol_name = "DllMain" };
    dll.subsystem = .Console;
    dll.linkSystemLibrary("gdi32");
    
    // settings for producing smallest possible dll
    dll.root_module.single_threaded = true;
    dll.root_module.strip = true;
    
    b.installArtifact(dll);
}