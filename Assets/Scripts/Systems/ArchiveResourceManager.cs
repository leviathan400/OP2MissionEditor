using OP2UtilityDotNet;
using System;
using System.Collections.Generic;
using System.IO;

namespace OP2MissionEditor.Systems
{
	/// <summary>
	/// Wraps one or more OP2UtilityDotNet.ResourceManager instances so resources can be resolved
	/// across multiple directories.
	///
	/// Supports both the legacy Outpost 2 install layout (single directory containing .vol/.clm
	/// archives and loose files) and the Outpost 2 1.4.1 / OPU layout where archives and assets
	/// are split across OPU/base, OPU/libs, OPU/maps, etc.
	/// </summary>
	public class ArchiveResourceManager : IDisposable
	{
		private readonly List<ResourceManager> m_Managers = new List<ResourceManager>();

		public ArchiveResourceManager(string gameDirectory)
		{
			foreach (string dir in EnumerateResourceDirectories(gameDirectory))
			{
				if (Directory.Exists(dir))
					m_Managers.Add(new ResourceManager(dir));
			}
		}

		/// <summary>
		/// Returns the resource directories to search for the given game directory, in priority
		/// order. If an OPU sub-folder is present (Outpost 2 1.4.1+), its known subdirectories
		/// are searched before the legacy game root.
		/// </summary>
		public static IEnumerable<string> EnumerateResourceDirectories(string gameDirectory)
		{
			if (string.IsNullOrEmpty(gameDirectory))
				yield break;

			string opuDir = Path.Combine(gameDirectory, "OPU");
			if (Directory.Exists(opuDir))
			{
				yield return Path.Combine(opuDir, "base");
				yield return Path.Combine(opuDir, "base", "tilesets");
				yield return Path.Combine(opuDir, "libs");
				yield return Path.Combine(opuDir, "maps");
				yield return opuDir;
			}

			yield return gameDirectory;
		}

		/// <summary>
		/// Returns a stream for the named resource by trying each underlying ResourceManager in
		/// priority order. Returns null if no manager resolves the resource.
		/// </summary>
		public Stream GetResourceStream(string resourceName, bool accessArchives)
		{
			foreach (ResourceManager mgr in m_Managers)
			{
				Stream stream = mgr.GetResourceStream(resourceName, accessArchives);
				if (stream != null)
					return stream;
			}
			return null;
		}

		public void Dispose()
		{
			foreach (ResourceManager mgr in m_Managers)
				mgr.Dispose();
			m_Managers.Clear();
		}
	}
}
