using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using GorillaNetworking;
using System.IO;
using Logger = BepInEx.Logging.Logger;

namespace CustomSounds.Patches;

public static class HarmonyPatches
{
    private static Harmony? _harmonyInstance;
    public static ManualLogSource? logSource = Logger.CreateLogSource("CustomSounds");
    public static Harmony HarmonyInstance
    {
        get
        {
            _harmonyInstance ??= new Harmony(Constants.Guid);
            return _harmonyInstance;
        }
    }

    public static void Patch() => HarmonyInstance.PatchAll();
    public static void Unpatch() => HarmonyInstance.UnpatchSelf();

    [HarmonyPatch(typeof(VRRig), "PlayTagSoundLocal")]
    public class PlayTagSoundLocal
    {
        private static bool Prefix(VRRig __instance, int soundIndex)
        {
            logSource?.LogInfo("roomJoinedTime " + Main.roomJoinedTime);
            logSource?.LogInfo("Time.time " + Time.time);
            
            if (Time.time < Main.roomJoinedTime + 3f)
            {
                logSource?.LogDebug($"Tried to run custom sound early ({Time.time - (Main.roomJoinedTime + 3f)} since joining)");
                return true;
            }
            
            logSource?.LogInfo($"Sound Index Attempted: {soundIndex}");
            AudioClip? customSound = soundIndex switch
            {
                0 => Main.customTagSound,
                2 => Main.customRoundEndSound,
                _ => null
            };

            if (customSound == null) return true;
            
            logSource?.LogDebug($"Playing custom sound for sound index {soundIndex}");
            __instance.clipToPlay[soundIndex] = customSound;

            return true;
        }
    }
}