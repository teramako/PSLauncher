using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PSLauncher;

internal static partial class NativeMethods
{
    #region for Window Style
    public const int GWL_EXSTYLE = -20;
    public const int WS_EX_NOACTIVATE = 0x08000000;
    public const int WS_EX_TOOLWINDOW = 0x00000080;
    public const int WM_ACTIVATEAPP = 0x001C;

    [LibraryImport("user32.dll", EntryPoint = "GetWindowLongA")]
    public static partial int GetWindowLong(nint hWnd, int nIndex);

    [LibraryImport("user32.dll", EntryPoint = "SetWindowLongA")]
    public static partial int SetWindowLong(nint hWnd, int nIndex, int dwNewLong);
    #endregion

    #region for HotKey
    public const uint MOD_ALT = 0x0001;
    public const uint MOD_CONTROL = 0x0002;
    public const uint MOD_SHIFT = 0x0004;
    public const uint MOD_WIN = 0x0008;

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool UnregisterHotKey(IntPtr hWnd, int id);
    #endregion

    #region Files/Directories Icon
    [LibraryImport("shell32.dll", EntryPoint = "SHGetFileInfoW", StringMarshalling = StringMarshalling.Utf16)]
    private static partial IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool DestroyIcon(IntPtr hIcon);

    private const uint SHGFI_ICON = 0x100;
    private const uint SHGFI_SMALLICON = 0x1;
    private const uint SHGFI_USEFILEATTRIBUTES = 0x10;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private unsafe struct SHFILEINFO
    {
        public IntPtr hIcon;
        public int iIcon;
        public uint dwAttributes;
        public fixed ushort szDisplayName[260];
        public fixed ushort szTypeName[80];
    }

    public static ImageSource? GetIcon(FileSystemInfo fsi)
    {
        var info = new SHFILEINFO();

        uint flags = SHGFI_ICON | SHGFI_SMALLICON;

        // ファイルの場合のみ USEFILEATTRIBUTES を使う
        if (fsi is FileInfo)
        {
            flags |= SHGFI_USEFILEATTRIBUTES;
        }

        IntPtr result = SHGetFileInfo(fsi.FullName,
                                      0,
                                      ref info,
                                      (uint)Marshal.SizeOf(info),
                                      flags);

        if (result == IntPtr.Zero)
            return null;

        var icon = Imaging.CreateBitmapSourceFromHIcon(info.hIcon,
                                                       Int32Rect.Empty,
                                                       BitmapSizeOptions.FromEmptyOptions());
        DestroyIcon(info.hIcon);
        return icon;
    }
    #endregion
}
