using System;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using Zorro.Core;
using Random = UnityEngine.Random;

namespace NekogiriMod
{
    [BepInPlugin("kirigiri.peak.nekogirioffline", "NekogiriPeakOffline", "1.0.0.0")]
    public class NekogiriMod : BaseUnityPlugin
    {
        private static ManualLogSource _logger;

        private void Awake()
        {
            _logger = Logger;

            _logger.LogInfo(@"
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
            _logger.LogInfo("NekogiriPeakOffline has loaded!");

            var harmony = new Harmony("kirigiri.peak.nekogirioffline");
            harmony.PatchAll();

            _logger.LogInfo("Made with <3 By Kirigiri \nhttps://discord.gg/TBs8Te5nwn");
        }

        [HarmonyPatch(typeof(CloudAPI), nameof(CloudAPI.CheckVersion))]
        public class CloudAPICheckVersionPatch
        {
            private static int? _cachedLevelIndex;

            [HarmonyPrefix]
            public static bool Prefix(Action<LoginResponse> response)
            {
                _logger.LogInfo("[NekogiriPeak] Patching CloudAPI.CheckVersion");

                var buildVersion = new BuildVersion(Application.version);

                if (_cachedLevelIndex == null) _cachedLevelIndex = Random.Range(1, 1001);

                var loginResponse = new LoginResponse
                {
                    VersionOkay = true,
                    HoursUntilLevel = 24,
                    MinutesUntilLevel = 0,
                    SecondsUntilLevel = 0,
                    LevelIndex = _cachedLevelIndex.Value,
                    Message = buildVersion.BuildName == "beta"
                        ? "Thanks for testing the PEAK beta. Watch out for bugs! (the current beta is the same as the live game, check back later for a new beta!)"
                        : "new patch :) have fun with the flying disc~  ALSO BACKPACK FIX??"
                };

                response?.Invoke(loginResponse);
                return false;
            }
        }

        [HarmonyPatch(typeof(NetworkConnector), nameof(NetworkConnector.ConnectToPhoton))]
        public class NetworkConnectorPatch
        {
            [HarmonyPrefix]
            public static bool Prefix()
            {
                _logger.LogInfo("[NekogiriPeak] Patching NetworkConnector.ConnectToPhoton");

                PhotonNetwork.OfflineMode = true;
                var version = new BuildVersion(Application.version);
                PhotonNetwork.AutomaticallySyncScene = true;
                PhotonNetwork.GameVersion = version.ToString();
                PhotonNetwork.PhotonServerSettings.AppSettings.AppVersion = version.ToMatchmaking();

                var method = typeof(NetworkConnector).GetMethod("PrepareSteamAuthTicket",
                    BindingFlags.NonPublic | BindingFlags.Static);
                if (method != null)
                    method.Invoke(null, new object[]
                    {
                        new Action(() =>
                        {
                            PhotonNetwork.ConnectUsingSettings();
                            _logger.LogInfo("Photon Start " + PhotonNetwork.NetworkClientState +
                                            " using app version: " + version.ToMatchmaking());
                        })
                    });
                else
                    _logger.LogError("[NekogiriPeak] Failed to find PrepareSteamAuthTicket via reflection");

                return false;
            }
        }

        [HarmonyPatch(typeof(PhotonNetwork), nameof(PhotonNetwork.ConnectUsingSettings), typeof(AppSettings),
            typeof(bool))]
        public class PhotonNetworkPatch
        {
            [HarmonyPrefix]
            public static bool Prefix(AppSettings appSettings, bool startInOfflineMode, ref bool __result)
            {
                _logger.LogInfo("[NekogiriPeak] Overriding PhotonNetwork.ConnectUsingSettings");

                __result = false;
                if (!startInOfflineMode && !PhotonNetwork.OfflineMode) return false;

                PhotonNetwork.OfflineMode = true;
                _logger.LogWarning("Kirigiri disabled the online mode, going offline !");
                __result = true;

                return false;
            }
        }
    }
}