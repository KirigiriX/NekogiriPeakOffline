using System;
using System.IO;
using BepInEx;
using HarmonyLib;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using Zorro.Core;
using UnityEngine.Networking;

namespace NekogiriMod
{
    [BepInPlugin("kirigiri.peak.nekogirioffline", "NekogiriPeakOffline", "1.0.0.0")]
    public class NekogiriMod : BaseUnityPlugin
    {
        private void Awake()
        {
            // Set up plugin logging
            Logger.LogInfo(@"
 ██ ▄█▀ ██▓ ██▀███   ██▓  ▄████  ██▓ ██▀███   ██▓
 ██▄█▒ ▓██▒▓██ ▒ ██▒▓██▒ ██▒ ▀█▒▓██▒▓██ ▒ ██▒▓██▒
▓███▄░ ▒██▒▓██ ░▄█ ▒▒██▒▒██░▄▄▄░▒██▒▓██ ░▄█ ▒▒██▒
▓██ █▄ ░██░▒██▀▀█▄  ░██░░▓█  ██▓░██░▒██▀▀█▄  ░██░
▒██▒ █▄░██░░██▓ ▒██▒░██░░▒▓███▀▒░██░░██▓ ▒██▒░██░
▒ ▒▒ ▓▒░▓  ░ ▒▓ ░▒▓░░▓   ░▒   ▒ ░▓  ░ ▒▓ ░▒▓░░▓  
░ ░▒ ▒░ ▒ ░  ░▒ ░ ▒░ ▒ ░  ░   ░  ▒ ░  ░▒ ░ ▒░ ▒ ░
░ ░░ ░  ▒ ░  ░░   ░  ▒ ░░ ░   ░  ▒ ░  ░░   ░  ▒ ░
░  ░    ░     ░      ░        ░  ░     ░      ░  
                                                 
");
            Logger.LogInfo("NekogiriPeakOffline has loaded!");

            var harmony = new Harmony("kirigiri.peak.nekogirioffline");
            harmony.PatchAll();

            // Optionally log that the patch has been applied
            Logger.LogInfo("Made with <3 By Kirigiri \nhttps://discord.gg/TBs8Te5nwn");
        }

        [HarmonyPatch(typeof(CloudAPI), nameof(CloudAPI.CheckVersion))]
        public class CloudAPICheckVersionPatch
        {
            private static int? _cachedLevelIndex = null;
            [HarmonyPrefix]
            public static bool Prefix(Action<LoginResponse> response)
            {
                Debug.Log("[NekogiriPeak] Patching CloudAPI.CheckVersion");

                if (_cachedLevelIndex == null)
                {
                    _cachedLevelIndex = new int?(global::UnityEngine.Random.Range(1, 1001));
                }
                LoginResponse loginResponse = new LoginResponse
                {
                    VersionOkay = true,
                    HoursUntilLevel = 24,
                    MinutesUntilLevel = 00,
                    SecondsUntilLevel = 00,
                    LevelIndex = _cachedLevelIndex.Value,
                    Message = "new patch :) have fun with the flying disc~  ALSO BACKPACK FIX??"
                };
                if (response != null)
                {
                    response(loginResponse);
                }

                return false;
            }
        }

        [HarmonyPatch(typeof(NetworkConnector), nameof(NetworkConnector.ConnectToPhoton))]
        public class NetworkConnectorPatch
        {
            [HarmonyPrefix]
            public static bool Prefix()
            {
                Debug.Log("[NekogiriPeak] Patching NetworkConnector.ConnectToPhoton");

                PhotonNetwork.OfflineMode = true;
                BuildVersion version = new BuildVersion(Application.version);
                PhotonNetwork.AutomaticallySyncScene = true;
                PhotonNetwork.GameVersion = version.ToString();
                PhotonNetwork.PhotonServerSettings.AppSettings.AppVersion = version.ToMatchmaking();

                // Use reflection to call private static method PrepareSteamAuthTicket
                var method = typeof(NetworkConnector).GetMethod("PrepareSteamAuthTicket", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                if (method != null)
                {
                    method.Invoke(null, new object[] {
                new Action(() =>
                {
                    PhotonNetwork.ConnectUsingSettings();
                    Debug.Log("Photon Start " + PhotonNetwork.NetworkClientState.ToString() +
                              " using app version: " + version.ToMatchmaking());
                })
            });
                }
                else
                {
                    Debug.LogError("[NekogiriPeak] Failed to find PrepareSteamAuthTicket via reflection");
                }

                return false;
            }
        }
        [HarmonyPatch(typeof(PhotonNetwork), nameof(PhotonNetwork.ConnectUsingSettings), new[] { typeof(AppSettings), typeof(bool) })]
        public class PhotonNetworkPatch
        {
            [HarmonyPrefix]
            public static bool Prefix(AppSettings appSettings, bool startInOfflineMode, ref bool __result)
            {
                Debug.Log("[NekogiriPeak] Overriding PhotonNetwork.ConnectUsingSettings");

                if (startInOfflineMode || PhotonNetwork.OfflineMode)
                {
                    PhotonNetwork.OfflineMode = true;
                    Debug.LogWarning("Kirigiri disabled the online mode, going offline !");
                    __result = true;
                    return false;
                }

                __result = false;
                return false;
            }
        }
    }
}
