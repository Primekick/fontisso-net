const std = @import("std");
const win = std.os.windows;
const W = std.unicode.utf8ToUtf16LeStringLiteral;

const DLL_PROCESS_ATTACH: u32 = 1;
const DLL_PROCESS_DETACH: u32 = 0;

extern "gdi32" fn AddFontResourceExW(name: [*:0]const u16, fl: u32, res: ?*anyopaque) callconv(win.WINAPI) i32;
extern "gdi32" fn RemoveFontResourceExW(name: [*:0]const u16, fl: u32, res: ?*anyopaque) callconv(win.WINAPI) i32;

export fn DllMain(h_module: win.HMODULE, dw_reason: u32, lp_reserved: ?*anyopaque) callconv(win.WINAPI) win.BOOL {
    _ = h_module;
    _ = lp_reserved;
    
    switch (dw_reason) {
        DLL_PROCESS_ATTACH => {
            _ = AddFontResourceExW(W("Fonts\\RPG2000.fon"), 0x10, null);
            _ = AddFontResourceExW(W("Fonts\\RPG2000G.fon"), 0x10, null);
        },
        DLL_PROCESS_DETACH => {
            _ = RemoveFontResourceExW(W("Fonts\\RPG2000.fon"), 0x10, null);
            _ = RemoveFontResourceExW(W("Fonts\\RPG2000G.fon"), 0x10, null);
        },
        else => {},
    }
    
    return win.TRUE;
}

export fn Dummy() callconv(.C) void {}
