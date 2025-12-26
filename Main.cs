using System;
using System.IO;
using CustomSounds.Patches;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace CustomSounds;

[BepInPlugin(Constants.Guid, Constants.Name, Constants.Version)]
public class Main : BaseUnityPlugin
{
    public static Main? Instance;
    public static AudioClip? customTagSound;
    public static AudioClip? customRoundEndSound;

    static string soundFolder = @"C:\Program Files (x86)\Steam\steamapps\common\Gorilla Tag\BepInEx\CustomSounds\";

    static ConfigEntry<string> _tagSoundPath = null!;
    static ConfigEntry<string> _roundEndSoundPath = null!;

    public static float roomJoinedTime;
    
    public static string TagSoundPath
    {
        get => _tagSoundPath.Value;
        set => _tagSoundPath.Value = value;
    }
    
    public static string RoundEndSoundPath
    {
        get => _roundEndSoundPath.Value;
        set => _roundEndSoundPath.Value = value;
    }
    
    private bool CheckSoundExists(string path) => File.Exists(path);
    private void OnJoined() => roomJoinedTime = Time.time;

    private void CreateSoundsFolder()
    {
        {
            if (!Directory.Exists(soundFolder))
            {
                Directory.CreateDirectory(soundFolder);
                Logger.LogDebug("Created Custom Sounds Folder");
            }
            else
            {
                Logger.LogDebug("Custom Sounds Folder Already Exists");
            }
        }
    }

    private void CreateTutorialFile()
    {
        const string content = @"This is the super awesome tutorial for Custom Sounds by @uhclash!
=================================================================

Here's how to add a new sound to the sounds folder:
1. Download your sound file.
2. Make sure the sound file is either a .mp3, .wav, or .ogg.
3. Then move your sound file to the following folder: ""C:\Program Files (x86)\Steam\steamapps\common\Gorilla Tag\BepInEx\CustomSounds"", or your ""BepInEx\CustomSounds"" folder.

Now, here's how to select the sound file for the mod:
1. Open up the following config file: ""C:\Program Files (x86)\Steam\steamapps\common\Gorilla Tag\BepInEx\config\com.uhclash.gorillatag.CustomSounds.cfg""
2. Under the [Sound Paths] section there is two configs, ""Tag Sound Path"" and ""Round End Sound Path"".
3. There will be a default path, so add the file name of the sound in the ""BepInEx\CustomSounds"" folder.
4. Press CTRL + S to save the config.

Then you can go ahead and play, the Custom Sounds won't play if it hasn't been atleast 1 seconds since joining the lobby to avoid issues, but from then on it should work!

=================================================================

Thank you for using my mod,
Clash.";
        
        File.WriteAllText(Path.Combine(soundFolder, "README.txt"), content);
    }

    private AudioType DetectAudioType(string path)
    {
        string extension = Path.GetExtension(path).ToLower();

        AudioType audioType = extension switch
        {
            ".wav" => AudioType.WAV,
            ".mp3" => AudioType.MPEG,
            ".ogg" => AudioType.OGGVORBIS,
            _ => AudioType.UNKNOWN
        };

        if (audioType == AudioType.UNKNOWN)
        {
            Logger.LogError($"Sound file {path} is not a supported audio type! (Must be .wav, .mp3, or .ogg)");
        }
        
        return audioType;
    }

    private async Task<AudioClip?> LoadAudioClipAsync(string path, AudioType audioType)
    {
        if (!CheckSoundExists(path)) Logger.LogInfo($"{path} does not exist!");
        
        string fixedPath = path.Replace("\\", "/");
        using UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip($"file://{fixedPath}", audioType);
        
        var operation = request.SendWebRequest();
        while (!operation.isDone) await Task.Yield();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.Log($"{Path.GetFileName(path)} failed to load!");
            return null;
        }
        
        return DownloadHandlerAudioClip.GetContent(request);
    }

    private void Awake()
    {
        _tagSoundPath = Config.Bind("Sound Paths", "Tag Sound Path", soundFolder, new ConfigDescription("What is the path to the tag sound file? (Must be .wav or .mp3)"));
        _roundEndSoundPath = Config.Bind("Sound Paths", "Round End Sound Path", soundFolder, new ConfigDescription("What is the path to the round end sound file? (Must be .wav or .mp3)"));
        
        GorillaTagger.OnPlayerSpawned(() => NetworkSystem.Instance.OnJoinedRoomEvent.Add(OnJoined));
        
        if (TagSoundPath == soundFolder || RoundEndSoundPath == soundFolder)
        {
            Logger.LogError("The file path for one of the sounds has not been set, please set the file path for both sounds.");
        }
        
        CreateSoundsFolder();
        CreateTutorialFile();
    }

    private async void Start()
    {
        Instance ??= this;
        
        Logger.LogInfo("Custom Sounds Started!");
        
        customTagSound = await LoadAudioClipAsync(TagSoundPath, DetectAudioType(TagSoundPath));
        customRoundEndSound = await LoadAudioClipAsync(RoundEndSoundPath, DetectAudioType(RoundEndSoundPath));
        
        HarmonyPatches.Patch();
    }

    public void OnEnable()
    {
        HarmonyPatches.Patch();
        Logger.LogDebug("Custom Sounds Patched!");
    }

    public void OnDisable()
    {
        HarmonyPatches.Unpatch();
        Logger.LogDebug("Custom Sounds Unpatched!");
    }
}
