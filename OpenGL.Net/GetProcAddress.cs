
// Copyright (C) 2009-2015 Luca Piccioni
// 
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
// 
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301
// USA

// Macro utility for logging platform-relared function calls
#undef PLATFORM_LOG_ENABLED

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace OpenGL
{
	/// <summary>
	/// Abtract an interface for interoperating with dynamically loaded libraries.
	/// </summary>
	public static class GetProcAddress
	{
		#region Static Constructor

		/// <summary>
		/// Static constructor.
		/// </summary>
		static GetProcAddress()
		{
			switch (Environment.OSVersion.Platform) {
				case PlatformID.Win32NT:
				case PlatformID.Win32Windows:
				case PlatformID.Win32S:
				case PlatformID.WinCE:
					_GetProcAddress = new GetProcAddressWindows();
					break;
				case PlatformID.Unix:
					string unixName = DetectUnixKernel();

					// Distinguish between Unix and Mac OS X kernels.
					switch (unixName) {
						case "Unix":
						case "Linux":
							_GetProcAddress = new GetProcAddressX11();
							break;
						case "Darwin":
							_GetProcAddress = new GetProcAddressOSX();
							break;
						case null:
							throw new NotSupportedException(String.Format("Unix platform not detected"));
						default:
							throw new NotSupportedException(String.Format("Unix platform {0} not supported", unixName));
					}
					break;
				case PlatformID.MacOSX:
					_GetProcAddress = new GetProcAddressOSX();
					break;
				default:
					throw new NotSupportedException(String.Format("platform {0} not supported", Environment.OSVersion.Platform));
			}
		}

		#endregion

		#region Static Implementation of IGetProcAddress

		/// <summary>
		/// Add a path of a directory as additional path for searching libraries.
		/// </summary>
		/// <param name="libraryDirPath">
		/// A <see cref="String"/> that specify the absolute path of the directory where the libraries are loaded using
		/// <see cref="GetProcAddress(string, string)"/> method.
		/// </param>
		public static void AddLibraryDirectory(string libraryDirPath)
		{
			if (_GetProcAddress == null)
				throw new PlatformNotSupportedException();
			_GetProcAddress.AddLibraryDirectory(libraryDirPath);
		}

		/// <summary>
		/// Retrieves the entry point for a dynamically exported function from any valid assembly.
		/// </summary>
		/// <param name="path">
		/// A <see cref="String"/> that specifies the path of the assembly file.
		/// </param>
		/// <param name="function">
		/// The function string for the OpenGL function (i.e. "GetProcAddress")
		/// </param>
		/// <returns>
		/// An IntPtr contaning the address for the entry point, or IntPtr.Zero if the specified
		/// function is not dynamically exported.
		/// </returns>
		public static IntPtr GetAddress(string path, string function)
		{
			if (_GetProcAddress == null)
				throw new PlatformNotSupportedException();
			return (_GetProcAddress.GetProcAddress(path, function));
		}


		/// <summary>
		/// Retrieves the entry point for a dynamically exported OpenGL function.
		/// </summary>
		/// <param name="function">The function string for the OpenGL function (eg. "glNewList")</param>
		/// <returns>
		/// An IntPtr contaning the address for the entry point, or IntPtr.Zero if the specified
		/// OpenGL function is not dynamically exported.
		/// </returns>
		public static IntPtr GetOpenGLAddress(string function)
		{
			if (_GetProcAddress == null)
				throw new PlatformNotSupportedException();
			return (_GetProcAddress.GetOpenGLProcAddress(function));
		}

		/// <summary>
		/// Executes "uname" which returns a string representing the name of the
		/// underlying Unix kernel.
		/// </summary>
		/// <returns>
		/// "Unix", "Linux", "Darwin" or null.
		/// </returns>
		/// <remarks>
		/// Source code from "Mono: A Developer's Notebook"
		/// </remarks>
		private static string DetectUnixKernel()
		{
			ProcessStartInfo startInfo = new ProcessStartInfo();

			startInfo.Arguments = "-s";
			startInfo.RedirectStandardOutput = true;
			startInfo.RedirectStandardError = true;
			startInfo.UseShellExecute = false;

			foreach (string unameprog in new string[] { "/usr/bin/uname", "/bin/uname", "uname" }) {
				// Avoid exception handling
				if (File.Exists(unameprog) == false)
					continue;

				try {
					startInfo.FileName = unameprog;
					using (Process uname = Process.Start(startInfo)) {
						return uname.StandardOutput.ReadLine().Trim();
					}
				} catch (FileNotFoundException) {
					// The requested executable doesn't exist, try next one.
					continue;
				} catch (System.ComponentModel.Win32Exception) {
					continue;
				}
			}

			return (null);
		}

		/// <summary>
		/// Interface for loading external symbols.
		/// </summary>
		private static IGetProcAddress _GetProcAddress;

		#endregion
	}

	/// <summary>
	/// Interface implemented by those classes which are able to get function pointers from dynamically loaded libraries.
	/// </summary>
	interface IGetProcAddress
	{
		/// <summary>
		/// Add a path of a directory as additional path for searching libraries.
		/// </summary>
		/// <param name="libraryDirPath">
		/// A <see cref="String"/> that specify the absolute path of the directory where the libraries are loaded using
		/// <see cref="GetProcAddress(string, string)"/> method.
		/// </param>
		void AddLibraryDirectory(string libraryDirPath);

		/// <summary>
		/// Get a function pointer from a library, specified by path.
		/// </summary>
		/// <param name="library">
		/// A <see cref="String"/> that specifies the path of the library defining the function.
		/// </param>
		/// <param name="function">
		/// A <see cref="String"/> that specifies the function name.
		/// </param>
		/// <returns>
		/// It returns an <see cref="IntPtr"/> that specifies the address of the function <paramref name="function"/>.
		/// </returns>
		IntPtr GetProcAddress(string library, string function);

		/// <summary>
		/// Get a function pointer from a library, specified by handle.
		/// </summary>
		/// <param name="library">
		/// A <see cref="IntPtr"/> which represents an opaque handle to the library containing the function.
		/// </param>
		/// <param name="function">
		/// A <see cref="String"/> that specifies the function name.
		/// </param>
		/// <returns>
		/// It returns an <see cref="IntPtr"/> that specifies the address of the function <paramref name="function"/>.
		/// </returns>
		IntPtr GetProcAddress(IntPtr library, string function);

		/// <summary>
		/// Get a function pointer from the OpenGL driver.
		/// </summary>
		/// <param name="function">
		/// A <see cref="String"/> that specifies the function name.
		/// </param>
		/// <returns>
		/// It returns an <see cref="IntPtr"/> that specifies the address of the function <paramref name="function"/>.
		/// </returns>
		IntPtr GetOpenGLProcAddress(string function);
	}

	/// <summary>
	/// Class able to get function pointers on Windows platform.
	/// </summary>
	class GetProcAddressWindows : IGetProcAddress
	{
		#region Windows Platform Imports

		private enum LoadLibraryExFlags : uint
		{
			DONT_RESOLVE_DLL_REFERENCES =			0x00000001,

			LOAD_IGNORE_CODE_AUTHZ_LEVEL =			0x00000010,

			LOAD_LIBRARY_AS_DATAFILE =				0x00000002,

			LOAD_LIBRARY_AS_DATAFILE_EXCLUSIVE =	0x00000040,

			LOAD_LIBRARY_AS_IMAGE_RESOURCE =		0x00000020,

			LOAD_LIBRARY_SEARCH_APPLICATION_DIR =	0x00000200,

			LOAD_LIBRARY_SEARCH_DEFAULT_DIRS =		0x00001000,

			LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR =		0x00000100,

			LOAD_LIBRARY_SEARCH_SYSTEM32 =			0x00000800,

			LOAD_LIBRARY_SEARCH_USER_DIRS =			0x00000400,

			LOAD_WITH_ALTERED_SEARCH_PATH =			0x00000008,
		}

		unsafe static class UnsafeNativeMethods
		{
			[DllImport("Kernel32.dll", SetLastError = true)]
			public static extern IntPtr LoadLibrary(String lpFileName);

			[DllImport("Kernel32.dll", SetLastError = true)]
			public static extern IntPtr LoadLibraryEx(String lpFilename, IntPtr hFile, LoadLibraryExFlags dwFlags);

			[DllImport("Kernel32.dll", SetLastError = true)]
			public static extern void FreeLibrary(IntPtr hModule);

			[DllImport("Kernel32.dll", EntryPoint = "GetProcAddress", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
			public static extern IntPtr Win32GetProcAddress(IntPtr hModule, String lpProcName);

			[DllImport(Library, EntryPoint = "wglGetProcAddress", ExactSpelling = true, SetLastError = true)]
			public static extern IntPtr wglGetProcAddress(String lpszProc);
		}

		#endregion

		#region IGetProceAddress Implementation

		/// <summary>
		/// Add a path of a directory as additional path for searching libraries.
		/// </summary>
		/// <param name="libraryDirPath">
		/// A <see cref="String"/> that specify the absolute path of the directory where the libraries are loaded using
		/// <see cref="GetProcAddress(string, string)"/> method.
		/// </param>
		public void AddLibraryDirectory(string libraryDirPath)
		{
			string path = Environment.GetEnvironmentVariable("PATH");

			Environment.SetEnvironmentVariable("PATH", String.Format("{0};{1}", path, libraryDirPath), EnvironmentVariableTarget.Process);
		}

		/// <summary>
		/// Get a function pointer from a library, specified by path.
		/// </summary>
		/// <param name="library">
		/// A <see cref="String"/> that specifies the path of the library defining the function.
		/// </param>
		/// <param name="function">
		/// A <see cref="String"/> that specifies the function name.
		/// </param>
		/// <returns>
		/// It returns an <see cref="IntPtr"/> that specifies the address of the function <paramref name="function"/>.
		/// </returns>
		public IntPtr GetProcAddress(string library, string function)
		{
			IntPtr handle = GetLibraryHandle(library);

			return (GetProcAddress(handle, function));
		}

		/// <summary>
		/// Get a function pointer from a library, specified by handle.
		/// </summary>
		/// <param name="library">
		/// A <see cref="IntPtr"/> which represents an opaque handle to the library containing the function.
		/// </param>
		/// <param name="function">
		/// A <see cref="String"/> that specifies the function name.
		/// </param>
		/// <returns>
		/// It returns an <see cref="IntPtr"/> that specifies the address of the function <paramref name="function"/>.
		/// </returns>
		public IntPtr GetProcAddress(IntPtr library, string function)
		{
			if (library == IntPtr.Zero)
				throw new ArgumentNullException("library");
			if (function == null)
				throw new ArgumentNullException("function");

			IntPtr procAddress = UnsafeNativeMethods.Win32GetProcAddress(library, function);
#if PLATFORM_LOG_ENABLED
			KhronosApi.LogFunction("GetProcAddress(0x{0}, {1}) = 0x{2}", library.ToString("X8"), function, procAddress.ToString("X8"));
#endif

			return (procAddress);
		}

		/// <summary>
		/// Get a function pointer from the OpenGL driver.
		/// </summary>
		/// <param name="function">
		/// A <see cref="String"/> that specifies the function name.
		/// </param>
		/// <returns>
		/// It returns an <see cref="IntPtr"/> that specifies the address of the function <paramref name="function"/>.
		/// </returns>
		public IntPtr GetOpenGLProcAddress(string function)
		{
			IntPtr procAddress = UnsafeNativeMethods.wglGetProcAddress(function);
#if PLATFORM_LOG_ENABLED
			KhronosApi.LogFunction("wglGetProcAddress({0}) = 0x{1}", function, procAddress.ToString("X8"));
#endif

			if (procAddress == IntPtr.Zero)
				return (GetProcAddress(Library, function));
			else
				return (procAddress);
		}

		/// <summary>
		/// Get the handle relative to the specified library.
		/// </summary>
		/// <param name="libraryPath">
		/// A <see cref="String"/> that specify the path of the library to load.
		/// </param>
		/// <returns>
		/// It returns a <see cref="IntPtr"/> that represents the handle of the library loaded from <paramref name="libraryPath"/>.
		/// </returns>
		private IntPtr GetLibraryHandle(string libraryPath)
		{
			IntPtr libraryHandle;

			if (_LibraryHandles.TryGetValue(libraryPath, out libraryHandle) == false) {
				// Load library
				libraryHandle = UnsafeNativeMethods.LoadLibrary(libraryPath);
#if PLATFORM_LOG_ENABLED
				KhronosApi.LogFunction("LoadLibrary({0}) = 0x{1}", libraryPath, libraryHandle.ToString("X8"));
#endif

				_LibraryHandles.Add(libraryPath, libraryHandle);
			}

			if (libraryHandle == IntPtr.Zero)
				throw new InvalidOperationException(String.Format("unable to load library at {0}", libraryPath));

			return (libraryHandle);
		}

		/// <summary>
		/// The OpenGL library on Windows platform.
		/// </summary>
		private const string Library = "opengl32.dll";

		/// <summary>
		/// Currently loaded libraries.
		/// </summary>
		private static readonly Dictionary<string, IntPtr> _LibraryHandles = new Dictionary<string,IntPtr>();

		#endregion
	}

	/// <summary>
	/// Class able to get function pointers on X11 platform.
	/// </summary>
	class GetProcAddressX11 : IGetProcAddress
	{
		#region X11 Platform Imports

		unsafe static class UnsafeNativeMethods
		{
			public const int RTLD_NOW = 2;

			[DllImport("dl")]
			public static extern IntPtr dlopen([MarshalAs(UnmanagedType.LPTStr)] string filename, int flags);

			[DllImport("dl")]
			public static extern IntPtr dlsym(IntPtr handle, [MarshalAs(UnmanagedType.LPTStr)] string symbol);

			[DllImport(Library, EntryPoint = "glXGetProcAddress")]
			public static extern IntPtr glxGetProcAddress([MarshalAs(UnmanagedType.LPTStr)] string procName);
		}

		#endregion

		#region IGetProcAdress Imports

		/// <summary>
		/// Add a path of a directory as additional path for searching libraries.
		/// </summary>
		/// <param name="libraryDirPath">
		/// A <see cref="String"/> that specify the absolute path of the directory where the libraries are loaded using
		/// <see cref="GetProcAddress(string, string)"/> method.
		/// </param>
		public void AddLibraryDirectory(string libraryDirPath)
		{
			
		}

		/// <summary>
		/// Get a function pointer from a library, specified by path.
		/// </summary>
		/// <param name="library">
		/// A <see cref="String"/> that specifies the path of the library defining the function.
		/// </param>
		/// <param name="function">
		/// A <see cref="String"/> that specifies the function name.
		/// </param>
		/// <returns>
		/// It returns an <see cref="IntPtr"/> that specifies the address of the function <paramref name="function"/>.
		/// </returns>
		public IntPtr GetProcAddress(string library, string function)
		{
			IntPtr handle = GetLibraryHandle(library);

			return (GetProcAddress(handle, function));
		}

		/// <summary>
		/// Get a function pointer from a library, specified by handle.
		/// </summary>
		/// <param name="library">
		/// A <see cref="IntPtr"/> which represents an opaque handle to the library containing the function.
		/// </param>
		/// <param name="function">
		/// A <see cref="String"/> that specifies the function name.
		/// </param>
		/// <returns>
		/// It returns an <see cref="IntPtr"/> that specifies the address of the function <paramref name="function"/>.
		/// </returns>
		public IntPtr GetProcAddress(IntPtr library, string function)
		{
			if (library == IntPtr.Zero)
				throw new ArgumentNullException("library");
			if (function == null)
				throw new ArgumentNullException("function");

			return (UnsafeNativeMethods.dlsym(library, function));
		}

		/// <summary>
		/// Get a function pointer from the OpenGL driver.
		/// </summary>
		/// <param name="function">
		/// A <see cref="String"/> that specifies the function name.
		/// </param>
		/// <returns>
		/// It returns an <see cref="IntPtr"/> that specifies the address of the function <paramref name="function"/>.
		/// </returns>
		public IntPtr GetOpenGLProcAddress(string function)
		{
			return (UnsafeNativeMethods.glxGetProcAddress(function));
		}

		private IntPtr GetLibraryHandle(string libraryPath)
		{
			IntPtr libraryHandle;

			if (sLibraryHandles.TryGetValue(libraryPath, out libraryHandle) == false) {
				libraryHandle = UnsafeNativeMethods.dlopen(libraryPath, UnsafeNativeMethods.RTLD_NOW);
				sLibraryHandles.Add(libraryPath, libraryHandle);
			}

			if (libraryHandle == IntPtr.Zero)
				throw new InvalidOperationException(String.Format("unable to load library at {0}", libraryPath));

			return (libraryHandle);
		}

		/// <summary>
		/// The OpenGL library on Unix/Linux platforms.
		/// </summary>
		private const string Library = "GL.so";

		/// <summary>
		/// Currently loaded libraries.
		/// </summary>
		private static readonly Dictionary<string, IntPtr> sLibraryHandles = new Dictionary<string,IntPtr>();

		#endregion
	}

	/// <summary>
	/// Class able to get function pointers on OSX platform.
	/// </summary>
	class GetProcAddressOSX : IGetProcAddress
	{
		#region OSX Platform Imports

		unsafe static class UnsafeNativeMethods
		{
			[DllImport(Library, EntryPoint = "NSIsSymbolNameDefined")]
			public static extern bool NSIsSymbolNameDefined(string s);

			[DllImport(Library, EntryPoint = "NSLookupAndBindSymbol")]
			public static extern IntPtr NSLookupAndBindSymbol(string s);

			[DllImport(Library, EntryPoint = "NSAddressOfSymbol")]
			public static extern IntPtr NSAddressOfSymbol(IntPtr symbol);
		}

		#endregion

		#region IGetProcAddress Implementation

		/// <summary>
		/// Add a path of a directory as additional path for searching libraries.
		/// </summary>
		/// <param name="libraryDirPath">
		/// A <see cref="String"/> that specify the absolute path of the directory where the libraries are loaded using
		/// <see cref="GetProcAddress(string, string)"/> method.
		/// </param>
		public void AddLibraryDirectory(string libraryDirPath)
		{
			
		}

		/// <summary>
		/// Get a function pointer from a library, specified by path.
		/// </summary>
		/// <param name="library">
		/// A <see cref="String"/> that specifies the path of the library defining the function.
		/// </param>
		/// <param name="function">
		/// A <see cref="String"/> that specifies the function name.
		/// </param>
		/// <returns>
		/// It returns an <see cref="IntPtr"/> that specifies the address of the function <paramref name="function"/>.
		/// </returns>
		public IntPtr GetProcAddress(string library, string function)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Get a function pointer from a library, specified by handle.
		/// </summary>
		/// <param name="library">
		/// A <see cref="IntPtr"/> which represents an opaque handle to the library containing the function.
		/// </param>
		/// <param name="function">
		/// A <see cref="String"/> that specifies the function name.
		/// </param>
		/// <returns>
		/// It returns an <see cref="IntPtr"/> that specifies the address of the function <paramref name="function"/>.
		/// </returns>
		public IntPtr GetProcAddress(IntPtr library, string function)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Get a function pointer from the OpenGL driver.
		/// </summary>
		/// <param name="function">
		/// A <see cref="String"/> that specifies the function name.
		/// </param>
		/// <returns>
		/// It returns an <see cref="IntPtr"/> that specifies the address of the function <paramref name="function"/>.
		/// </returns>
		public IntPtr GetOpenGLProcAddress(string function)
		{
			string fname = "_" + function;
			if (!UnsafeNativeMethods.NSIsSymbolNameDefined(fname))
				return IntPtr.Zero;

			IntPtr symbol = UnsafeNativeMethods.NSLookupAndBindSymbol(fname);
			if (symbol != IntPtr.Zero)
				symbol = UnsafeNativeMethods.NSAddressOfSymbol(symbol);

			return (symbol);
		}

		/// <summary>
		/// The OpenGL library on OSX platform.
		/// </summary>
		private const string Library = "libdl.dylib";

		#endregion
	}
}
