using OP2MissionEditor.Dialogs;
using OP2MissionEditor.Dialogs.Generic;
using OP2MissionEditor.Systems;
using OP2MissionEditor.Systems.Map;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace OP2MissionEditor.Scenes
{
	public class MainController : MonoBehaviour
	{
		private const string k_UpdateCheckUrl = "https://update.outpostuniverse.org/OP2MissionEditor.htm";
		private const string k_DownloadUrl = "https://github.com/leviathan400/OP2MissionEditor/releases";

		[SerializeField] private MapRenderer m_MapRenderer		= default;

		private ProgressDialog m_ProgressDialog					= default;


		private void Awake()
		{
			// Make sure game directory is valid
			if (!string.IsNullOrEmpty(UserPrefs.gameDirectory))
			{
				// If directory does not exist, reset it so we can ask again.
				if (!Directory.Exists(UserPrefs.gameDirectory))
					UserPrefs.gameDirectory = null;
			}

			ConsoleLog.Initialize();
			TextureManager.Initialize();

			// Register events
			m_MapRenderer.onMapRefreshProgressCB += OnMapRefreshProgress;
			m_MapRenderer.onMapRefreshedCB += OnMapRefreshed;

			UserData.current.CreateNew();

			// If game directory hasn't been set, Open "Locate Outpost2" dialog to force user to select one
			if (string.IsNullOrEmpty(UserPrefs.gameDirectory))
				PreferencesDialog.Create();

			Debug.Log("Editor initialized.");

			StartCoroutine(CheckForUpdate());
		}

		private IEnumerator CheckForUpdate()
		{
			using (UnityWebRequest req = UnityWebRequest.Get(k_UpdateCheckUrl))
			{
				req.timeout = 5;
				yield return req.SendWebRequest();

				if (req.result != UnityWebRequest.Result.Success)
				{
					Debug.LogWarning("Update check failed: " + req.error + " (" + k_UpdateCheckUrl + ")");
					yield break;
				}

				string serverVersion = req.downloadHandler.text?.Trim();
				string localVersion = Application.version.Replace(".", "");

				if (!int.TryParse(serverVersion, out int serverNum) || !int.TryParse(localVersion, out int localNum))
					yield break;

				if (serverNum <= localNum)
					yield break;

				ConfirmDialog.Create(
					(didConfirm) =>
					{
						if (!didConfirm) return;
						Application.OpenURL(k_DownloadUrl);
						Application.Quit();
					},
					"Update Available",
					"A new version is available.\n\nInstalled: " + Application.version + "\nLatest: " + FormatServerVersion(serverVersion) + "\n\nDo you want to open the downloads page?",
					"Yes",
					"No"
				);
			}
		}

		/// <summary>
		/// Converts the server's compact version string ("055") into a dotted form ("0.5.5").
		/// Each digit becomes its own version segment.
		/// </summary>
		private static string FormatServerVersion(string raw)
		{
			if (string.IsNullOrEmpty(raw))
				return raw;
			return string.Join(".", raw.ToCharArray());
		}

		private void OnMapRefreshProgress(MapRenderer mapRenderer, string state, float progress)
		{
			if (m_ProgressDialog == null)
				m_ProgressDialog = ProgressDialog.Create(state);

			m_ProgressDialog.SetTitle(state);
			m_ProgressDialog.SetProgress(progress);
		}

		private void OnMapRefreshed(MapRenderer mapRenderer)
		{
			if (m_ProgressDialog != null)
				m_ProgressDialog.Close();

			m_ProgressDialog = null;
		}

		private void OnDestroy()
		{
			// Unregister events
			m_MapRenderer.onMapRefreshProgressCB -= OnMapRefreshProgress;
			m_MapRenderer.onMapRefreshedCB -= OnMapRefreshed;

			UserData.current.Dispose();

			TextureManager.Release();
		}
	}
}
