using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace Installer
{
	[Flags]
	public enum SHGetFileInfoConstants
	{
		SHGFI_ICON = 0x100,                // get icon 
		SHGFI_DISPLAYNAME = 0x200,         // get display name 
		SHGFI_TYPENAME = 0x400,            // get type name 
		SHGFI_ATTRIBUTES = 0x800,          // get attributes 
		SHGFI_ICONLOCATION = 0x1000,       // get icon location 
		SHGFI_EXETYPE = 0x2000,            // return exe type 
		SHGFI_SYSICONINDEX = 0x4000,       // get system icon index 
		SHGFI_LINKOVERLAY = 0x8000,        // put a link overlay on icon 
		SHGFI_SELECTED = 0x10000,          // show icon in selected state 
		SHGFI_ATTR_SPECIFIED = 0x20000,    // get only specified attributes 
		SHGFI_LARGEICON = 0x0,             // get large icon 
		SHGFI_SMALLICON = 0x1,             // get small icon 
		SHGFI_OPENICON = 0x2,              // get open icon 
		SHGFI_SHELLICONSIZE = 0x4,         // get shell size icon 
		//SHGFI_PIDL = 0x8,                  // pszPath is a pidl 
		SHGFI_USEFILEATTRIBUTES = 0x10,     // use passed dwFileAttribute 
		SHGFI_ADDOVERLAYS = 0x000000020,     // apply the appropriate overlays
		SHGFI_OVERLAYINDEX = 0x000000040     // Get the index of the overlay
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct SHFILEINFO
	{
		public IntPtr hIcon;
		public int iIcon;
		public int dwAttributes;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
		public string szDisplayName;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
		public string szTypeName;
	}

	public static class PInvoke
	{
		[DllImport("shell32")]
		public static extern int SHGetFileInfo(
			string pszPath,
			int dwFileAttributes,
			ref SHFILEINFO psfi,
			uint cbFileInfo,
			uint uFlags);

		[DllImport("user32")]
		public static extern int DestroyIcon(IntPtr hIcon);

		[DllImport("kernel32")]
		public extern static int FormatMessage(
			int dwFlags,
			IntPtr lpSource,
			int dwMessageId,
			int dwLanguageId,
			string lpBuffer,
			uint nSize,
			int argumentsLong);

		[DllImport("kernel32")]
		public extern static int GetLastError();

		[DllImport("Shell32", CharSet=CharSet.Auto)]
		internal extern static int ExtractIconEx (
			[MarshalAs(UnmanagedType.LPTStr)] 
				string lpszFile,
			int nIconIndex,
			IntPtr[] phIconLarge, 
			IntPtr[] phIconSmall,
			int nIcons);
	}

	public class FileIcon
	{
		private const int MAX_PATH = 260;

		private const int FORMAT_MESSAGE_ALLOCATE_BUFFER = 0x100;
		private const int FORMAT_MESSAGE_ARGUMENT_ARRAY = 0x2000;
		private const int FORMAT_MESSAGE_FROM_HMODULE = 0x800;
		private const int FORMAT_MESSAGE_FROM_STRING = 0x400;
		private const int FORMAT_MESSAGE_FROM_SYSTEM = 0x1000;
		private const int FORMAT_MESSAGE_IGNORE_INSERTS = 0x200;
		private const int FORMAT_MESSAGE_MAX_WIDTH_MASK = 0xFF;

		/// <summary>
		/// Gets/sets the flags used to extract the icon
		/// </summary>
		public SHGetFileInfoConstants Flags { get;  set; }

		/// <summary>
		/// Gets/sets the filename to get the icon for
		/// </summary>
		public string FileName { get; set; }

		/// <summary>
		/// Gets the icon for the chosen file
		/// </summary>
		public Icon ShellIcon { get; private set; }

		/// <summary>
		/// Gets the display name for the selected file
		/// if the SHGFI_DISPLAYNAME flag was set.
		/// </summary>
		public string DisplayName { get; private set; }

		/// <summary>
		/// Gets the type name for the selected file
		/// if the SHGFI_TYPENAME flag was set.
		/// </summary>
		public string TypeName { get; private set; }

		/// <summary>
		///  Gets the information for the specified 
		///  file name and flags.
		/// </summary>
		public void GetInfo()
		{
			ShellIcon = null;
			TypeName = "";
			DisplayName = "";

			var shfi = new SHFILEINFO();
			var shfiSize = (uint)Marshal.SizeOf(shfi.GetType());

			var ret = PInvoke.SHGetFileInfo(FileName, 0, ref shfi, shfiSize, (uint)Flags);
			if (ret == 0)
			{
				var errorCode = PInvoke.GetLastError();
				var txtS = new string('\0', 256);
				var len = PInvoke.FormatMessage(FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS,
				                                IntPtr.Zero, errorCode, 0, txtS, 256, 0);
				throw new Exception(String.Format("Faild SHGetFileInfo return code {0} last error {1} message {2}", ret, errorCode, txtS));
			}
			if (shfi.hIcon != IntPtr.Zero)
			{
				ShellIcon = Icon.FromHandle(shfi.hIcon);
				// Now owned by the GDI+ object
				//DestroyIcon(shfi.hIcon);
			}

			TypeName = shfi.szTypeName;
			DisplayName = shfi.szDisplayName;
		}

		/// <summary>
		/// Constructs a new, default instance of the FileIcon
		/// class.  Specify the filename and call GetInfo()
		/// to retrieve an icon.
		/// </summary>
		public FileIcon()
		{
			Flags = SHGetFileInfoConstants.SHGFI_ICON |
			        SHGetFileInfoConstants.SHGFI_DISPLAYNAME |
			        SHGetFileInfoConstants.SHGFI_TYPENAME |
			        SHGetFileInfoConstants.SHGFI_ATTRIBUTES |
			        SHGetFileInfoConstants.SHGFI_EXETYPE;
		}
		/// <summary>
		/// Constructs a new instance of the FileIcon class
		/// and retrieves the icon, display name and type name
		/// for the specified file.		
		/// </summary>
		/// <param name="fileName">The filename to get the icon, 
		/// display name and type name for</param>
		public FileIcon(string fileName)
			: this()
		{
			FileName = fileName;
			GetInfo();
		}
		/// <summary>
		/// Constructs a new instance of the FileIcon class
		/// and retrieves the information specified in the 
		/// flags.
		/// </summary>
		/// <param name="fileName">The filename to get information
		/// for</param>
		/// <param name="flags">The flags to use when extracting the
		/// icon and other shell information.</param>
		public FileIcon(string fileName, SHGetFileInfoConstants flags)
		{
			FileName = fileName;
			Flags = flags;
			GetInfo();
		}

	}

	[ComImport]
	[Guid("0000010C-0000-0000-C000-000000000046")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IPersist
	{
		[PreserveSig]
		//[helpstring("Returns the class identifier for the component object")]
		void GetClassID(out Guid pClassID);
	}

	[ComImport]
	[Guid("0000010B-0000-0000-C000-000000000046")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IPersistFile
	{
		// can't get this to go if I extend IPersist, so put it here:
		[PreserveSig]
		void GetClassID(out Guid pClassID);

		//[helpstring("Checks for changes since last file write")]		
		void IsDirty();

		//[helpstring("Opens the specified file and initializes the object from its contents")]		
		void Load(
			[MarshalAs(UnmanagedType.LPWStr)] string pszFileName,
			uint dwMode);

		//[helpstring("Saves the object into the specified file")]		
		void Save(
			[MarshalAs(UnmanagedType.LPWStr)] string pszFileName,
			[MarshalAs(UnmanagedType.Bool)] bool fRemember);

		//[helpstring("Notifies the object that save is completed")]		
		void SaveCompleted(
			[MarshalAs(UnmanagedType.LPWStr)] string pszFileName);

		//[helpstring("Gets the current name of the file associated with the object")]		
		void GetCurFile(
			[MarshalAs(UnmanagedType.LPWStr)] out string ppszFileName);
	}

	[ComImport()]
	[Guid("000214F9-0000-0000-C000-000000000046")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IShellLinkW
	{
		//[helpstring("Retrieves the path and filename of a shell link object")]
		void GetPath(
			[Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile,
			int cchMaxPath,
			ref _WIN32_FIND_DATAW pfd,
			uint fFlags);

		//[helpstring("Retrieves the list of shell link item identifiers")]
		void GetIDList(out IntPtr ppidl);

		//[helpstring("Sets the list of shell link item identifiers")]
		void SetIDList(IntPtr pidl);

		//[helpstring("Retrieves the shell link description string")]
		void GetDescription(
			[Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile,
			int cchMaxName);

		//[helpstring("Sets the shell link description string")]
		void SetDescription(
			[MarshalAs(UnmanagedType.LPWStr)] string pszName);

		//[helpstring("Retrieves the name of the shell link working directory")]
		void GetWorkingDirectory(
			[Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir,
			int cchMaxPath);

		//[helpstring("Sets the name of the shell link working directory")]
		void SetWorkingDirectory(
			[MarshalAs(UnmanagedType.LPWStr)] string pszDir);

		//[helpstring("Retrieves the shell link command-line arguments")]
		void GetArguments(
			[Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs,
			int cchMaxPath);

		//[helpstring("Sets the shell link command-line arguments")]
		void SetArguments(
			[MarshalAs(UnmanagedType.LPWStr)] string pszArgs);

		//[propget, helpstring("Retrieves or sets the shell link hot key")]
		void GetHotkey(out short pwHotkey);
		//[propput, helpstring("Retrieves or sets the shell link hot key")]
		void SetHotkey(short pwHotkey);

		//[propget, helpstring("Retrieves or sets the shell link show command")]
		void GetShowCmd(out uint piShowCmd);
		//[propput, helpstring("Retrieves or sets the shell link show command")]
		void SetShowCmd(uint piShowCmd);

		//[helpstring("Retrieves the location (path and index) of the shell link icon")]
		void GetIconLocation(
			[Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath,
			int cchIconPath,
			out int piIcon);

		//[helpstring("Sets the location (path and index) of the shell link icon")]
		void SetIconLocation(
			[MarshalAs(UnmanagedType.LPWStr)] string pszIconPath,
			int iIcon);

		//[helpstring("Sets the shell link relative path")]
		void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);

		//[helpstring("Resolves a shell link. The system searches for the shell link object and updates the shell link path and its list of identifiers (if necessary)")]
		void Resolve(IntPtr hWnd, uint fFlags);

		//[helpstring("Sets the shell link path and filename")]
		void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
	}

	[Guid("00021401-0000-0000-C000-000000000046")]
	[ClassInterface(ClassInterfaceType.None)]
	[ComImport()]
	public class CShellLink { }

	public enum EShellLinkGP : uint
	{
		SLGP_SHORTPATH = 1,
		SLGP_UNCPRIORITY = 2
	}

	[Flags]
	public enum EShowWindowFlags : uint
	{
		SW_HIDE = 0,
		SW_SHOWNORMAL = 1,
		SW_NORMAL = 1,
		SW_SHOWMINIMIZED = 2,
		SW_SHOWMAXIMIZED = 3,
		SW_MAXIMIZE = 3,
		SW_SHOWNOACTIVATE = 4,
		SW_SHOW = 5,
		SW_MINIMIZE = 6,
		SW_SHOWMINNOACTIVE = 7,
		SW_SHOWNA = 8,
		SW_RESTORE = 9,
		SW_SHOWDEFAULT = 10,
		SW_MAX = 10
	}

	[StructLayout(LayoutKind.Sequential, Pack = 4, Size = 0, CharSet = CharSet.Unicode)]
	public struct _WIN32_FIND_DATAW
	{
		public uint dwFileAttributes;
		public _FILETIME ftCreationTime;
		public _FILETIME ftLastAccessTime;
		public _FILETIME ftLastWriteTime;
		public uint nFileSizeHigh;
		public uint nFileSizeLow;
		public uint dwReserved0;
		public uint dwReserved1;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] // MAX_PATH
			public string cFileName;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
		public string cAlternateFileName;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 4, Size = 0, CharSet = CharSet.Ansi)]
	public struct _WIN32_FIND_DATAA
	{
		public uint dwFileAttributes;
		public _FILETIME ftCreationTime;
		public _FILETIME ftLastAccessTime;
		public _FILETIME ftLastWriteTime;
		public uint nFileSizeHigh;
		public uint nFileSizeLow;
		public uint dwReserved0;
		public uint dwReserved1;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] // MAX_PATH
			public string cFileName;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
		public string cAlternateFileName;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 4, Size = 0)]
	public struct _FILETIME
	{
		public uint dwLowDateTime;
		public uint dwHighDateTime;
	}

	/// <summary>
	/// Flags determining how the links with missing
	/// targets are resolved.
	/// </summary>
	[Flags]
	public enum EShellLinkResolveFlags : uint
	{
		/// <summary>
		/// Allow any match during resolution.  Has no effect
		/// on ME/2000 or above, use the other flags instead.
		/// </summary>
		SLR_ANY_MATCH = 0x2,
		/// <summary>
		/// Call the Microsoft Windows Installer. 
		/// </summary>
		SLR_INVOKE_MSI = 0x80,
		/// <summary>
		/// Disable distributed link tracking. By default, 
		/// distributed link tracking tracks removable media 
		/// across multiple devices based on the volume name. 
		/// It also uses the UNC path to track remote file 
		/// systems whose drive letter has changed. Setting 
		/// SLR_NOLINKINFO disables both types of tracking.
		/// </summary>
		SLR_NOLINKINFO = 0x40,
		/// <summary>
		/// Do not display a dialog box if the link cannot be resolved. 
		/// When SLR_NO_UI is set, a time-out value that specifies the 
		/// maximum amount of time to be spent resolving the link can 
		/// be specified in milliseconds. The function returns if the 
		/// link cannot be resolved within the time-out duration. 
		/// If the timeout is not set, the time-out duration will be 
		/// set to the default value of 3,000 milliseconds (3 seconds). 
		/// </summary>										    
		SLR_NO_UI = 0x1,
		/// <summary>
		/// Not documented in SDK.  Assume same as SLR_NO_UI but 
		/// intended for applications without a hWnd.
		/// </summary>
		SLR_NO_UI_WITH_MSG_PUMP = 0x101,
		/// <summary>
		/// Do not update the link information. 
		/// </summary>
		SLR_NOUPDATE = 0x8,
		/// <summary>
		/// Do not execute the search heuristics. 
		/// </summary>																																																																																																																																																																																																														
		SLR_NOSEARCH = 0x10,
		/// <summary>
		/// Do not use distributed link tracking. 
		/// </summary>
		SLR_NOTRACK = 0x20,
		/// <summary>
		/// If the link object has changed, update its path and list 
		/// of identifiers. If SLR_UPDATE is set, you do not need to 
		/// call IPersistFile::IsDirty to determine whether or not 
		/// the link object has changed. 
		/// </summary>
		SLR_UPDATE = 0x4
	}

	public enum LinkDisplayMode : uint
	{
		Normal = EShowWindowFlags.SW_NORMAL,
		Minimized = EShowWindowFlags.SW_SHOWMINNOACTIVE,
		Maximized = EShowWindowFlags.SW_MAXIMIZE
	}

	public class ShellLink : IDisposable
	{
		private IShellLinkW link;

		/// <summary>
		/// Creates an instance of the Shell Link object.
		/// </summary>
		public ShellLink()
		{
			link = (IShellLinkW)new CShellLink();
			HotKey = Keys.None;
			DisplayMode = LinkDisplayMode.Normal;
		}

		/// <summary>
		/// Creates an instance of a Shell Link object
		/// from the specified link file
		/// </summary>
		/// <param name="linkFile">The Shortcut file to open</param>
		public ShellLink(string linkFile) : this()
		{
			Open(linkFile);
		}

		/// <summary>
		/// Call dispose just in case it hasn't happened yet
		/// </summary>
		~ShellLink()
		{
			Dispose();
		}

		/// <summary>
		/// Dispose the object, releasing the COM ShellLink object
		/// </summary>
		public void Dispose()
		{
			if (link != null ) 
			{
				Marshal.ReleaseComObject(link);
				link = null;
			}
		}

		public string ShortcutFile { get; set; }

		/// <summary>
		/// Gets the path to the file containing the icon for this shortcut.
		/// </summary>
		public string IconPath { get; set; }

		/// <summary>
		/// Gets the index of this icon within the icon path's resources
		/// </summary>
		public int IconIndex { get; set; }

		/// <summary>
		/// Gets/sets the fully qualified path to the link's target
		/// </summary>
		public string Target { get; set; }

		/// <summary>
		/// Gets/sets the Working Directory for the Link
		/// </summary>
		public string WorkingDirectory { get; set; }

		/// <summary>
		/// Gets/sets the description of the link
		/// </summary>
		public string Description { get; set; }

		/// <summary>
		/// Gets/sets any command line arguments associated with the link
		/// </summary>
		public string Arguments { get; set; }

		/// <summary>
		/// Gets/sets the initial display mode when the shortcut is
		/// run
		/// </summary>
		public LinkDisplayMode DisplayMode { get; set;}

		/// <summary>
		/// Gets/sets the HotKey to start the shortcut (if any)
		/// </summary>
		public Keys HotKey { get; set; }

		/// <summary>
		/// Saves the shortcut to ShortCutFile.
		/// </summary>
		public void Save()
		{
			Save(ShortcutFile);
		}

		/// <summary>
		/// Saves the shortcut to the specified file
		/// </summary>
		/// <param name="linkFile">The shortcut file (.lnk)</param>
		public void Save(string linkFile)
		{
			if (!String.IsNullOrEmpty(Arguments))
				link.SetArguments(Arguments);

			link.SetShowCmd((uint) DisplayMode);
			link.SetHotkey((short) HotKey);

			if (!String.IsNullOrEmpty(Description))
				link.SetDescription(Description);

			if (!String.IsNullOrEmpty(WorkingDirectory))
				link.SetWorkingDirectory(WorkingDirectory);

			if (!String.IsNullOrEmpty(Target))
				link.SetPath(Target);

			if (!String.IsNullOrEmpty(IconPath))
				link.SetIconLocation(IconPath, IconIndex);

			ShortcutFile = linkFile;
			((IPersistFile)link).Save(ShortcutFile, true);
		}

		/// <summary>
		/// Loads a shortcut from the specified file
		/// </summary>
		/// <param name="linkFile">The shortcut file (.lnk) to load</param>
		public void Open(string linkFile)
		{
			Open(linkFile, 
			     IntPtr.Zero, 
			     (EShellLinkResolveFlags.SLR_ANY_MATCH | EShellLinkResolveFlags.SLR_NO_UI),
			     1);
		}
		
		/// <summary>
		/// Loads a shortcut from the specified file, and allows flags controlling
		/// the UI behaviour if the shortcut's target isn't found to be set.
		/// </summary>
		/// <param name="linkFile">The shortcut file (.lnk) to load</param>
		/// <param name="hWnd">The window handle of the application's UI, if any</param>
		/// <param name="resolveFlags">Flags controlling resolution behaviour</param>
		public void Open(string linkFile,
		                 IntPtr hWnd,
		                 EShellLinkResolveFlags resolveFlags)
		{
			Open(linkFile, 
			     hWnd, 
			     resolveFlags, 
			     1);
		}

		/// <summary>
		/// Loads a shortcut from the specified file, and allows flags controlling
		/// the UI behaviour if the shortcut's target isn't found to be set.  If
		/// no SLR_NO_UI is specified, you can also specify a timeout.
		/// </summary>
		/// <param name="linkFile">The shortcut file (.lnk) to load</param>
		/// <param name="hWnd">The window handle of the application's UI, if any</param>
		/// <param name="resolveFlags">Flags controlling resolution behaviour</param>
		/// <param name="timeOut">Timeout if SLR_NO_UI is specified, in ms.</param>
		public void Open(string linkFile,
		                 IntPtr hWnd,
		                 EShellLinkResolveFlags resolveFlags,
		                 ushort timeOut)
		{
			uint flags;

			if ((resolveFlags & EShellLinkResolveFlags.SLR_NO_UI) == EShellLinkResolveFlags.SLR_NO_UI)
				flags = (uint)((int)resolveFlags | (timeOut << 16));
			else
				flags = (uint)resolveFlags;

			((IPersistFile)link).Load(linkFile, 0); //STGM_DIRECT)
			link.Resolve(hWnd, flags);
			ShortcutFile = linkFile;
		}
	}
}