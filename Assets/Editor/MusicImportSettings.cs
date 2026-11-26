using UnityEditor;
using UnityEngine;

// Configura a importação dos arquivos de música durante o processamento de áudio no Unity Editor.
public sealed class MusicImportSettings : AssetPostprocessor
{
    // Aplica as opções de importação antes de o Editor converter o recurso sonoro.
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
