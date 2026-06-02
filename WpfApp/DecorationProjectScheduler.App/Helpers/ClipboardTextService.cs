using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace DecorationProjectScheduler.App.Helpers;

public static class ClipboardTextService
{
    private const uint CfUnicodeText = 13;
    private const uint GmemMoveable = 0x0002;

    public static bool TrySetText(string text, out string? errorMessage)
    {
        Exception? lastException = null;
        for (var attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                SetUnicodeText(text);
                errorMessage = null;
                return true;
            }
            catch (Exception ex)
            {
                lastException = ex;
                Thread.Sleep(100);
            }
        }

        errorMessage = lastException?.Message;
        return false;
    }

    private static void SetUnicodeText(string text)
    {
        if (!OpenClipboard(IntPtr.Zero))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "剪贴板暂时不可用");
        }

        var clipboardOpened = true;
        var hGlobal = IntPtr.Zero;
        try
        {
            if (!EmptyClipboard())
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "清空剪贴板失败");
            }

            var bytes = Encoding.Unicode.GetBytes(text + '\0');
            hGlobal = GlobalAlloc(GmemMoveable, (UIntPtr)bytes.Length);
            if (hGlobal == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "剪贴板内存申请失败");
            }

            var target = GlobalLock(hGlobal);
            if (target == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "剪贴板内存锁定失败");
            }

            try
            {
                Marshal.Copy(bytes, 0, target, bytes.Length);
            }
            finally
            {
                GlobalUnlock(hGlobal);
            }

            if (SetClipboardData(CfUnicodeText, hGlobal) == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "写入剪贴板失败");
            }

            hGlobal = IntPtr.Zero;
        }
        finally
        {
            if (clipboardOpened)
            {
                CloseClipboard();
            }

            if (hGlobal != IntPtr.Zero)
            {
                GlobalFree(hGlobal);
            }
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool OpenClipboard(IntPtr hWndNewOwner);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EmptyClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool CloseClipboard();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalLock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalUnlock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalFree(IntPtr hMem);
}
