using UnityEditor;
using UnityEngine;

public sealed class MusicImportSettings : AssetPostprocessor
{
    private void OnPreprocessAudio()
    {
        if (!assetPath.StartsWith("Assets/Resources/Music/") || !assetPath.EndsWith(".wav")) return;
        var importer = (AudioImporter)assetImporter;
        importer.forceToMono = false;
        importer.loadInBackground = true;
        AudioImporterSampleSettings settings = importer.defaultSampleSettings;
        settings.preloadAudioData = false;
        settings.loadType = AudioClipLoadType.Streaming;
        settings.compressionFormat = AudioCompressionFormat.PCM;
        settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
        importer.defaultSampleSettings = settings;
    }
}
