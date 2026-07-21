using System;
using System.IO;
using Immersive.Audio.Authoring;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.Audio.Editor
{
    public readonly struct AudioQaGeneratedClips
    {
        public AudioQaGeneratedClips(AudioClip sfxClip, AudioClip bgmClip)
        {
            SfxClip = sfxClip;
            BgmClip = bgmClip;
        }

        public AudioClip SfxClip { get; }

        public AudioClip BgmClip { get; }

        public bool IsValid => SfxClip != null && BgmClip != null;
    }

    public static class AudioQaGeneratedClipRepair
    {
        private const string Root = "Assets/ImmersiveFrameworkQA/Audio";
        private const string ScriptableObjects = Root + "/ScriptableObjects";
        private const string Clips = Root + "/AudioClips";
        private const string SfxClipPath = Clips + "/QA_Sfx_Beep.wav";
        private const string BgmClipPath = Clips + "/QA_Bgm_Tone.wav";

        [MenuItem("Immersive Framework/QA/Setup/Audio/Repair Generated Audio Clips")]
        public static void RepairGeneratedAudioClips()
        {
            EnsureGeneratedClipsAndAssignments();
        }

        public static AudioQaGeneratedClips EnsureGeneratedClipsAndAssignments()
        {
            EnsureFolder("Assets", "ImmersiveFrameworkQA");
            EnsureFolder("Assets/ImmersiveFrameworkQA", "Audio");
            EnsureFolder(Root, "AudioClips");
            EnsureFolder(Root, "ScriptableObjects");

            WriteToneWav(SfxClipPath, 880f, 0.25f, 0.35f);
            WriteToneWav(BgmClipPath, 220f, 2.0f, 0.18f);

            AssetDatabase.ImportAsset(SfxClipPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(BgmClipPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

            AudioClip sfxClip = AssetDatabase.LoadAssetAtPath<AudioClip>(SfxClipPath);
            AudioClip bgmClip = AssetDatabase.LoadAssetAtPath<AudioClip>(BgmClipPath);

            if (sfxClip == null || bgmClip == null)
            {
                Debug.LogError($"[AUDIO_QA] Clip repair failed. sfxClip='{(sfxClip != null)}' bgmClip='{(bgmClip != null)}'.");
                return new AudioQaGeneratedClips(sfxClip, bgmClip);
            }

            int sfxAssigned = 0;
            int bgmAssigned = 0;
            int intentionallyMissing = 0;
            int scanned = 0;

            string[] guids = AssetDatabase.FindAssets("t:AudioCueAsset", new[] { ScriptableObjects });
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                AudioCueAsset cue = AssetDatabase.LoadAssetAtPath<AudioCueAsset>(assetPath);
                if (cue == null)
                {
                    continue;
                }

                scanned++;
                string name = cue.name ?? string.Empty;
                bool isMissingClipCue = Contains(name, "missingclip") || Contains(name, "missing_clip") || Contains(name, "missing-clip");

                SerializedObject serialized = new SerializedObject(cue);
                SerializedProperty clipProperty = FindClipProperty(serialized);
                if (clipProperty == null)
                {
                    Debug.LogWarning($"[AUDIO_QA] Clip repair skipped cue with no serializable clip property. cue='{cue.name}' path='{assetPath}'.");
                    continue;
                }

                if (isMissingClipCue)
                {
                    clipProperty.objectReferenceValue = null;
                    intentionallyMissing++;
                }
                else if (cue is AudioBgmCueAsset)
                {
                    clipProperty.objectReferenceValue = bgmClip;
                    bgmAssigned++;
                }
                else if (cue is AudioSfxCueAsset)
                {
                    clipProperty.objectReferenceValue = sfxClip;
                    sfxAssigned++;
                }
                else
                {
                    Debug.LogWarning($"[AUDIO_QA] Clip repair found unsupported cue type. cue='{cue.name}' type='{cue.GetType().Name}' path='{assetPath}'.");
                    continue;
                }

                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(cue);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                $"[AUDIO_QA] Generated clip repair completed. scanned='{scanned}' sfxAssigned='{sfxAssigned}' bgmAssigned='{bgmAssigned}' intentionallyMissing='{intentionallyMissing}' sfxClip='{SfxClipPath}' bgmClip='{BgmClipPath}'.");

            return new AudioQaGeneratedClips(sfxClip, bgmClip);
        }

        private static SerializedProperty FindClipProperty(SerializedObject serialized)
        {
            string[] candidates =
            {
                "clip",
                "audioClip",
                "AudioClip",
                "m_Clip"
            };

            foreach (string candidate in candidates)
            {
                SerializedProperty property = serialized.FindProperty(candidate);
                if (property != null && property.propertyType == SerializedPropertyType.ObjectReference)
                {
                    return property;
                }
            }

            SerializedProperty iterator = serialized.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (iterator.propertyType == SerializedPropertyType.ObjectReference &&
                    iterator.type == nameof(AudioClip))
                {
                    return serialized.FindProperty(iterator.propertyPath);
                }
            }

            return null;
        }

        private static bool Contains(string value, string expected)
        {
            return value.Replace(" ", string.Empty).IndexOf(expected, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static void WriteToneWav(string assetPath, float frequency, float seconds, float amplitude)
        {
            string fullPath = Path.GetFullPath(assetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? string.Empty);

            const int sampleRate = 44100;
            const short channels = 1;
            const short bitsPerSample = 16;
            int sampleCount = Mathf.Max(1, Mathf.RoundToInt(sampleRate * Mathf.Max(0.01f, seconds)));
            int byteRate = sampleRate * channels * bitsPerSample / 8;
            short blockAlign = (short)(channels * bitsPerSample / 8);
            int dataLength = sampleCount * blockAlign;

            using (FileStream stream = File.Open(fullPath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(36 + dataLength);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
                writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
                writer.Write(16);
                writer.Write((short)1);
                writer.Write(channels);
                writer.Write(sampleRate);
                writer.Write(byteRate);
                writer.Write(blockAlign);
                writer.Write(bitsPerSample);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                writer.Write(dataLength);

                for (int i = 0; i < sampleCount; i++)
                {
                    float t = i / (float)sampleRate;
                    float fade = Mathf.Clamp01(Mathf.Min(i / 512f, (sampleCount - i) / 512f));
                    float sample = Mathf.Sin(2f * Mathf.PI * frequency * t) * amplitude * fade;
                    short pcm = (short)Mathf.Clamp(sample * short.MaxValue, short.MinValue, short.MaxValue);
                    writer.Write(pcm);
                }
            }
        }
    }
}
