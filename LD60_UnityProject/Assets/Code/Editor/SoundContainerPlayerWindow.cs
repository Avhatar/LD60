using LD60.Settings;
using LD60.Sound;
using UnityEditor;
using UnityEngine;

namespace LD60.EditorTools
{
    public class SoundContainerPlayerWindow : EditorWindow
    {
        private const string SelectionPrefsKey = "LD60.SoundContainerPlayer.ContainerGuid";
        private const string PreviewObjectName = "SoundContainerPreview";
        private const float MinRepeatInterval = 0.05f;
        private const float MaxRepeatInterval = 5f;

        private SoundContainerSO _container;
        private GameObject _previewObject;
        private AudioSource _previewSource;
        private AudioListener _previewListener;
        private AudioListener _sceneListener;

        private bool _followProjectSelection;
        private bool _autoRepeat;
        private float _repeatInterval = 0.5f;
        private double _nextRepeatTime;
        private float _pitchMultiplier = 1f;
        private bool _force2D = true;

        private string _rolledClipName = string.Empty;
        private int _rolledClipIndex = -1;
        private float _rolledVolume;
        private float _rolledPitch;
        private Vector2 _scroll;

        [MenuItem("Tools/Sound Container Player")]
        public static void ShowWindow()
        {
            SoundContainerPlayerWindow window = GetWindow<SoundContainerPlayerWindow>("Sound Container Player");
            window.minSize = new Vector2(340f, 420f);
        }

        private void OnEnable()
        {
            RestoreContainerFromPrefs();
            EditorApplication.update += OnEditorUpdate;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            _autoRepeat = false;
            DestroyPreview();
        }

        private void OnSelectionChange()
        {
            if (!_followProjectSelection)
                return;

            SoundContainerSO selected = Selection.activeObject as SoundContainerSO;
            if (selected != null && selected != _container)
                SetContainer(selected);

            Repaint();
        }

        private void OnGUI()
        {
            DrawContainerField();

            if (_container == null)
            {
                EditorGUILayout.HelpBox("No SoundContainer assigned. Pick an asset above, or enable \"Follow Project selection\" and click a SoundContainer in the Project window.", MessageType.Info);
                return;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            DrawContainerIssues();
            DrawContainerSettings();
            DrawRollRanges();
            DrawCallerSimulation();
            DrawTransport();
            DrawLiveState();
            DrawClipList();
            DrawListenerStatus();

            EditorGUILayout.EndScrollView();
        }

        private void DrawContainerField()
        {
            EditorGUILayout.Space(4f);
            using (new EditorGUILayout.HorizontalScope())
            {
                SoundContainerSO picked = EditorGUILayout.ObjectField("Container", _container, typeof(SoundContainerSO), false) as SoundContainerSO;
                if (picked != _container)
                    SetContainer(picked);

                using (new EditorGUI.DisabledScope(_container == null))
                {
                    if (GUILayout.Button("Select", GUILayout.Width(56f)) && _container != null)
                    {
                        Selection.activeObject = _container;
                        EditorGUIUtility.PingObject(_container);
                    }
                }
            }

            _followProjectSelection = EditorGUILayout.ToggleLeft("Follow Project selection", _followProjectSelection);
            EditorGUILayout.Space(4f);
        }

        private void DrawContainerIssues()
        {
            if (_container.AudioClips == null || _container.AudioClips.Count == 0)
            {
                EditorGUILayout.HelpBox("This container has no clips. AudioController would log an error and play nothing.", MessageType.Error);
                return;
            }

            int nullIndex = GetFirstNullClipIndex();
            if (nullIndex >= 0)
                EditorGUILayout.HelpBox($"Clip slot {nullIndex} is empty. Runtime playback would either play silence or log an error. Fix: remove the empty slot from the asset.", MessageType.Error);
        }

        private void DrawContainerSettings()
        {
            EditorGUILayout.LabelField("Asset settings (read-only)", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.Slider("Volume", _container.Volume, 0f, 1f);
                EditorGUILayout.FloatField("Volume Variance", _container.VolumeVariance);
                EditorGUILayout.Slider("Pitch", _container.Pitch, -3f, 3f);
                EditorGUILayout.FloatField("Pitch Variance", _container.PitchVariance);
                EditorGUILayout.Slider("Spatial Blend", _container.SpatialBlend, 0f, 1f);
                EditorGUILayout.Slider("Stereo Pan", _container.StereoPan, -1f, 1f);
                EditorGUILayout.Toggle("Loop", _container.Loop);
                EditorGUILayout.Toggle("Random Sound", _container.RandomSound);
            }

            EditorGUILayout.HelpBox("Edit these values on the asset itself in the Inspector — this window only listens.", MessageType.None);
            EditorGUILayout.Space(4f);
        }

        private void DrawRollRanges()
        {
            EditorGUILayout.LabelField("Roll ranges", EditorStyles.boldLabel);

            float volumeSpread = _container.VolumeVariance > 0f ? _container.VolumeVariance : 0f;
            float pitchSpread = _container.PitchVariance > 0f ? _container.PitchVariance : 0f;
            float rawVolumeMin = _container.Volume - volumeSpread;
            float rawVolumeMax = _container.Volume + volumeSpread;
            float rawPitchMin = _container.Pitch - pitchSpread;
            float rawPitchMax = _container.Pitch + pitchSpread;

            EditorGUILayout.LabelField("Volume", $"{Mathf.Clamp01(rawVolumeMin):0.###} .. {Mathf.Clamp01(rawVolumeMax):0.###}");
            EditorGUILayout.LabelField("Pitch", $"{Mathf.Clamp(rawPitchMin, -3f, 3f):0.###} .. {Mathf.Clamp(rawPitchMax, -3f, 3f):0.###}");

            if (rawVolumeMax > 1f || rawVolumeMin < 0f)
                EditorGUILayout.HelpBox("Volume variance pushes rolls outside [0..1]; AudioSource clamps them, so part of the range is flattened.", MessageType.Warning);

            if (rawPitchMin <= 0f && rawPitchMax >= 0f)
                EditorGUILayout.HelpBox("Pitch range crosses zero: some rolls will stall or reverse playback, and the tail fade's length maths becomes unreliable.", MessageType.Warning);
            else if (rawPitchMax > 3f || rawPitchMin < -3f)
                EditorGUILayout.HelpBox("Pitch variance exceeds the AudioSource limit of [-3..3]; extreme rolls are clamped.", MessageType.Warning);

            EditorGUILayout.Space(4f);
        }

        private void DrawCallerSimulation()
        {
            EditorGUILayout.LabelField("Caller simulation", EditorStyles.boldLabel);
            _pitchMultiplier = EditorGUILayout.Slider("Pitch Multiplier", _pitchMultiplier, 0.1f, 3f);
            _force2D = EditorGUILayout.ToggleLeft("Force 2D (as AudioController.Play2D)", _force2D);
            EditorGUILayout.HelpBox("Play2D forces spatial blend to 0; Play3D raises it to at least " + AudioController.Min3DSpatialBlend.ToString("0.##") + " and applies the 3D defaults from AudioSO. Both take a pitch multiplier. The runtime tail fade is not reproduced here.", MessageType.None);
            EditorGUILayout.Space(4f);
        }

        private void DrawTransport()
        {
            bool playable = IsPlayable();

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!playable))
                {
                    if (GUILayout.Button("Play", GUILayout.Height(28f)))
                        Play();
                }

                using (new EditorGUI.DisabledScope(!IsPreviewPlaying()))
                {
                    if (GUILayout.Button("Stop", GUILayout.Height(28f)))
                        Stop();
                }
            }

            using (new EditorGUI.DisabledScope(!playable))
            {
                bool repeat = EditorGUILayout.ToggleLeft("Auto repeat (re-rolls variance every shot)", _autoRepeat);
                if (repeat != _autoRepeat)
                {
                    _autoRepeat = repeat;
                    if (_autoRepeat)
                        _nextRepeatTime = 0d;
                }

                using (new EditorGUI.DisabledScope(!_autoRepeat))
                    _repeatInterval = EditorGUILayout.Slider("Interval", _repeatInterval, MinRepeatInterval, MaxRepeatInterval);
            }

            EditorGUILayout.Space(4f);
        }

        private void DrawLiveState()
        {
            EditorGUILayout.LabelField("Last roll", EditorStyles.boldLabel);

            if (string.IsNullOrEmpty(_rolledClipName))
            {
                EditorGUILayout.LabelField("Nothing played yet.");
                EditorGUILayout.Space(4f);
                return;
            }

            EditorGUILayout.LabelField("Clip", $"[{_rolledClipIndex}] {_rolledClipName}");
            EditorGUILayout.LabelField("Applied Volume", _rolledVolume.ToString("0.###"));
            EditorGUILayout.LabelField("Applied Pitch", _rolledPitch.ToString("0.###"));

            if (_previewSource != null && _previewSource.clip != null)
            {
                float length = Mathf.Abs(_rolledPitch) > 0.0001f ? _previewSource.clip.length / Mathf.Abs(_rolledPitch) : _previewSource.clip.length;
                EditorGUILayout.LabelField("Effective Length", $"{length:0.###} s (clip {_previewSource.clip.length:0.###} s)");

                float progress = _previewSource.clip.length > 0f ? _previewSource.time / _previewSource.clip.length : 0f;
                Rect barRect = EditorGUILayout.GetControlRect(false, 16f);
                EditorGUI.ProgressBar(barRect, progress, IsPreviewPlaying() ? "Playing" : "Stopped");
            }

            EditorGUILayout.Space(4f);
        }

        private void DrawClipList()
        {
            if (_container.AudioClips == null || _container.AudioClips.Count == 0)
                return;

            EditorGUILayout.LabelField($"Clips ({_container.AudioClips.Count})", EditorStyles.boldLabel);
            if (!_container.RandomSound && _container.AudioClips.Count > 1)
                EditorGUILayout.HelpBox("Random Sound is off, so runtime code always takes clip 0 and the rest never play.", MessageType.Warning);

            using (new EditorGUI.DisabledScope(true))
            {
                for (int i = 0; i < _container.AudioClips.Count; i++)
                {
                    string label = i == _rolledClipIndex ? $"{i} ◄ last" : i.ToString();
                    EditorGUILayout.ObjectField(label, _container.AudioClips[i], typeof(AudioClip), false);
                }
            }

            EditorGUILayout.Space(4f);
        }

        private void DrawListenerStatus()
        {
            EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);

            if (_previewListener != null)
            {
                EditorGUILayout.HelpBox("No active AudioListener was found in the open scenes, so the preview uses a temporary listener of its own. Panning and spatial blend are evaluated at the preview position.", MessageType.Info);
                return;
            }

            if (_sceneListener != null)
            {
                EditorGUILayout.LabelField("Listener", _sceneListener.gameObject.name);
                EditorGUILayout.HelpBox("The preview source is placed on the scene AudioListener, so a 3D container is heard at full range. If you hear nothing, enable the speaker toggle in the Scene View toolbar.", MessageType.Info);
                return;
            }

            EditorGUILayout.HelpBox("Press Play to create the preview source and resolve an AudioListener.", MessageType.None);
        }

        private void SetContainer(SoundContainerSO container)
        {
            Stop();
            _autoRepeat = false;
            _container = container;
            _rolledClipName = string.Empty;
            _rolledClipIndex = -1;

            if (container == null)
            {
                EditorPrefs.DeleteKey(SelectionPrefsKey);
                return;
            }

            string path = AssetDatabase.GetAssetPath(container);
            if (!string.IsNullOrEmpty(path))
                EditorPrefs.SetString(SelectionPrefsKey, AssetDatabase.AssetPathToGUID(path));
        }

        private void RestoreContainerFromPrefs()
        {
            if (_container != null)
                return;

            string guid = EditorPrefs.GetString(SelectionPrefsKey, string.Empty);
            if (string.IsNullOrEmpty(guid))
                return;

            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path))
                return;

            _container = AssetDatabase.LoadAssetAtPath<SoundContainerSO>(path);
        }

        private void Play()
        {
            if (_container == null)
            {
                Debug.LogError($"{nameof(SoundContainerPlayerWindow)}: {nameof(_container)} is null in {nameof(Play)}. " +
                               "A SoundContainer is required to audition a sound. Fix: assign a container in the window's Container field.");
                return;
            }

            if (_container.AudioClips == null || _container.AudioClips.Count == 0)
            {
                Debug.LogError($"{nameof(SoundContainerPlayerWindow)}: container '{_container.name}' has no clips in {nameof(Play)}. " +
                               "At least one AudioClip is required, exactly as AudioController.Play2D demands at runtime. Fix: add clips to the SoundContainer asset.");
                return;
            }

            int nullIndex = GetFirstNullClipIndex();
            if (nullIndex >= 0)
            {
                Debug.LogError($"{nameof(SoundContainerPlayerWindow)}: container '{_container.name}' has an empty clip slot at index {nullIndex} in {nameof(Play)}. " +
                               "An empty slot would play silence and can be picked by the random selection. Fix: remove the empty slot from the SoundContainer asset.");
                return;
            }

            if (!EnsurePreview())
                return;

            PlacePreviewOnListener();

            AudioController.ApplyContainer(_previewSource, _container);
            if (_force2D)
                _previewSource.spatialBlend = 0f;
            else if (_previewSource.spatialBlend < AudioController.Min3DSpatialBlend)
                _previewSource.spatialBlend = AudioController.Min3DSpatialBlend;

            _previewSource.pitch *= _pitchMultiplier;
            _previewSource.clip = AudioController.PickClip(_container);
            _previewSource.Play();

            _rolledVolume = _previewSource.volume;
            _rolledPitch = _previewSource.pitch;
            _rolledClipName = _previewSource.clip != null ? _previewSource.clip.name : string.Empty;
            _rolledClipIndex = GetClipIndex(_previewSource.clip);
            Repaint();
        }

        private void Stop()
        {
            if (_previewSource == null)
                return;

            _previewSource.Stop();
            Repaint();
        }

        private bool EnsurePreview()
        {
            if (_previewSource != null)
                return true;

            DestroyPreview();

            _previewObject = new GameObject(PreviewObjectName);
            _previewObject.hideFlags = HideFlags.HideAndDontSave;
            _previewSource = _previewObject.AddComponent<AudioSource>();
            _previewSource.playOnAwake = false;

            _sceneListener = FindFirstObjectByType<AudioListener>();
            if (_sceneListener == null)
                _previewListener = _previewObject.AddComponent<AudioListener>();

            if (_previewSource == null)
            {
                Debug.LogError($"{nameof(SoundContainerPlayerWindow)}: failed to create the preview {nameof(AudioSource)} in {nameof(EnsurePreview)}. " +
                               "The window needs a real AudioSource to reproduce runtime playback. Fix: reopen the window, and check the console for scene errors.");
                return false;
            }

            return true;
        }

        private void PlacePreviewOnListener()
        {
            if (_previewObject == null)
                return;

            if (_sceneListener == null && _previewListener == null)
                _sceneListener = FindFirstObjectByType<AudioListener>();

            if (_sceneListener != null)
                _previewObject.transform.position = _sceneListener.transform.position;
        }

        private void DestroyPreview()
        {
            _previewSource = null;
            _previewListener = null;
            _sceneListener = null;

            if (_previewObject == null)
                return;

            DestroyImmediate(_previewObject);
            _previewObject = null;
        }

        private void OnEditorUpdate()
        {
            if (_autoRepeat && IsPlayable())
            {
                double now = EditorApplication.timeSinceStartup;
                if (now >= _nextRepeatTime)
                {
                    Play();
                    _nextRepeatTime = now + Mathf.Max(MinRepeatInterval, _repeatInterval);
                }
            }

            if (IsPreviewPlaying() || _autoRepeat)
                Repaint();
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode && state != PlayModeStateChange.ExitingPlayMode)
                return;

            _autoRepeat = false;
            DestroyPreview();
        }

        private bool IsPlayable()
        {
            if (_container == null)
                return false;
            if (_container.AudioClips == null || _container.AudioClips.Count == 0)
                return false;

            return GetFirstNullClipIndex() < 0;
        }

        private bool IsPreviewPlaying()
        {
            return _previewSource != null && _previewSource.isPlaying;
        }

        private int GetFirstNullClipIndex()
        {
            if (_container == null || _container.AudioClips == null)
                return -1;

            for (int i = 0; i < _container.AudioClips.Count; i++)
            {
                if (_container.AudioClips[i] == null)
                    return i;
            }

            return -1;
        }

        private int GetClipIndex(AudioClip clip)
        {
            if (clip == null || _container == null || _container.AudioClips == null)
                return -1;

            for (int i = 0; i < _container.AudioClips.Count; i++)
            {
                if (_container.AudioClips[i] == clip)
                    return i;
            }

            return -1;
        }
    }
}
