using System.Collections;
using LD60.Settings;
using UnityEngine;
using Random = UnityEngine.Random;

namespace LD60.Sound
{
    [DefaultExecutionOrder(-500)]
    public class AudioController : MonoBehaviour
    {
        public const float Min3DSpatialBlend = 0.9f;

        private const string MasterVolumeKey = "LD60.MasterVolume";
        private const string MusicVolumeKey = "LD60.MusicVolume";

        public static AudioController Instance { get; private set; }

        public int MasterVolumePercent { get; private set; }

        public int MusicVolumePercent { get; private set; }

        public int MaxVolumePercent
        {
            get { return _settings == null ? 0 : _settings.MaxVolumePercent; }
        }

        private AudioSO _settings;
        private AudioSource[] _pool;
        private Transform[] _followTargets;
        private float[] _startTimes;
        private Coroutine[] _fadeRoutines;
        private AudioSource _musicSource;
        private float _musicBaseVolume = 1f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError($"{nameof(AudioController)}: another instance is already active. " +
                               $"Duplicate on '{name}' will be disabled to keep {nameof(Instance)} single-valued. " +
                               "Fix: ensure only one AudioController GameObject exists per scene.");
                enabled = false;
                return;
            }

            if (Core.Config == null)
            {
                Debug.LogError($"{nameof(AudioController)}: {nameof(Core)}.{nameof(Core.Config)} is not published yet when '{name}' Awake runs. " +
                               "AudioController cannot resolve its AudioSO. " +
                               "Fix: ensure a Core component with a CoreSO assigned exists in the scene, " +
                               "and that Core's execution order is earlier than AudioController's.");
                enabled = false;
                return;
            }

            _settings = Core.Config.Audio;
            if (_settings == null)
            {
                Debug.LogError($"{nameof(AudioController)}: {nameof(Core.Config.Audio)} is null on '{name}'. " +
                               "An AudioSO is required to size the 2D source pool. " +
                               "Fix: assign an AudioSO asset to the Audio slot on the CoreSO asset.");
                enabled = false;
                return;
            }

            int poolSize = _settings.Pool2DSize;
            _pool = new AudioSource[poolSize];
            _followTargets = new Transform[poolSize];
            _startTimes = new float[poolSize];
            _fadeRoutines = new Coroutine[poolSize];
            for (int i = 0; i < poolSize; i++)
            {
                GameObject go = new GameObject("2D Source " + i);
                go.transform.SetParent(transform, false);
                AudioSource src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 0f;
                _pool[i] = src;
            }

            GameObject musicGo = new GameObject("Music Source");
            musicGo.transform.SetParent(transform, false);
            _musicSource = musicGo.AddComponent<AudioSource>();
            _musicSource.playOnAwake = false;
            _musicSource.spatialBlend = 0f;

            MasterVolumePercent = ReadStoredVolume(MasterVolumeKey);
            MusicVolumePercent = ReadStoredVolume(MusicVolumeKey);
            ApplyMasterVolume();

            Instance = this;

            if (_settings.BackgroundMusic != null)
                StartBackgroundMusic(_settings.BackgroundMusic);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void Play2D(SoundContainerSO sound, float pitchMultiplier = 1f)
        {
            PlayInternal(sound, pitchMultiplier, spatial: false, Vector3.zero, follow: null, nameof(Play2D));
        }

        public void Play3D(SoundContainerSO sound, Vector3 position, float pitchMultiplier = 1f)
        {
            PlayInternal(sound, pitchMultiplier, spatial: true, position, follow: null, nameof(Play3D));
        }

        public void Play3D(SoundContainerSO sound, Transform follow, float pitchMultiplier = 1f)
        {
            if (follow == null)
            {
                Debug.LogError($"{nameof(AudioController)}.{nameof(Play3D)}: follow Transform is null. " +
                               "Fix: pass a non-null Transform, or use the Vector3 overload for pinned positions.");
                return;
            }

            PlayInternal(sound, pitchMultiplier, spatial: true, follow.position, follow, nameof(Play3D));
        }

        public void StopAll()
        {
            if (_pool == null)
                return;

            for (int i = 0; i < _pool.Length; i++)
            {
                if (_fadeRoutines[i] != null)
                {
                    StopCoroutine(_fadeRoutines[i]);
                    _fadeRoutines[i] = null;
                }

                _followTargets[i] = null;
                _pool[i].Stop();
            }
        }

        private void StartBackgroundMusic(SoundContainerSO music)
        {
            if (music.AudioClips == null || music.AudioClips.Count == 0)
            {
                Debug.LogError($"{nameof(AudioController)}.{nameof(StartBackgroundMusic)}: '{music.name}' has no clips in AudioClips. " +
                               "Background music will not play. " +
                               "Fix: assign at least one AudioClip on the BackgroundMusic SoundContainer asset.");
                return;
            }

            ApplyContainer(_musicSource, music);
            _musicBaseVolume = _musicSource.volume;
            ApplyMusicVolume();
            _musicSource.clip = PickClip(music);
            _musicSource.Play();
        }

        public void AdjustMasterVolume(int direction)
        {
            if (_settings == null)
            {
                Debug.LogError($"{nameof(AudioController)}.{nameof(AdjustMasterVolume)}: {nameof(_settings)} is null on '{name}'. " +
                               "Required to know the volume step and the maximum. " +
                               "Fix: assign an AudioSO asset to the Audio slot on the CoreSO asset.", this);
                return;
            }

            SetMasterVolume(MasterVolumePercent + direction * _settings.VolumeStepPercent);
        }

        public void AdjustMusicVolume(int direction)
        {
            if (_settings == null)
            {
                Debug.LogError($"{nameof(AudioController)}.{nameof(AdjustMusicVolume)}: {nameof(_settings)} is null on '{name}'. " +
                               "Required to know the volume step and the maximum. " +
                               "Fix: assign an AudioSO asset to the Audio slot on the CoreSO asset.", this);
                return;
            }

            SetMusicVolume(MusicVolumePercent + direction * _settings.VolumeStepPercent);
        }

        public void SetMasterVolume(int percent)
        {
            int clamped = ClampVolume(percent);

            if (clamped == MasterVolumePercent)
                return;

            MasterVolumePercent = clamped;
            PlayerPrefs.SetInt(MasterVolumeKey, clamped);
            PlayerPrefs.Save();
            ApplyMasterVolume();
        }

        public void SetMusicVolume(int percent)
        {
            int clamped = ClampVolume(percent);

            if (clamped == MusicVolumePercent)
                return;

            MusicVolumePercent = clamped;
            PlayerPrefs.SetInt(MusicVolumeKey, clamped);
            PlayerPrefs.Save();
            ApplyMusicVolume();
        }

        private int ReadStoredVolume(string key)
        {
            return ClampVolume(PlayerPrefs.GetInt(key, _settings.MaxVolumePercent));
        }

        private int ClampVolume(int percent)
        {
            if (percent < 0)
                return 0;

            if (percent > _settings.MaxVolumePercent)
                return _settings.MaxVolumePercent;

            return percent;
        }

        private void ApplyMasterVolume()
        {
            AudioListener.volume = MasterVolumePercent / (float)_settings.MaxVolumePercent;
        }

        private void ApplyMusicVolume()
        {
            if (_musicSource == null)
                return;

            _musicSource.volume = _musicBaseVolume * (MusicVolumePercent / (float)_settings.MaxVolumePercent);
        }

        private void PlayInternal(SoundContainerSO sound, float pitchMultiplier, bool spatial, Vector3 position, Transform follow, string callerName)
        {
            if (sound == null)
            {
                Debug.LogError($"{nameof(AudioController)}.{callerName}: sound is null. " +
                               "Fix: pass a non-null SoundContainerSO.");
                return;
            }

            if (sound.AudioClips == null || sound.AudioClips.Count == 0)
            {
                Debug.LogError($"{nameof(AudioController)}.{callerName}: '{sound.name}' has no clips in AudioClips. " +
                               "Fix: assign at least one AudioClip on the SoundContainer asset.");
                return;
            }

            int index = PickPoolIndex();
            AudioSource src = _pool[index];

            if (_fadeRoutines[index] != null)
            {
                StopCoroutine(_fadeRoutines[index]);
                _fadeRoutines[index] = null;
            }

            _followTargets[index] = null;
            src.transform.localPosition = Vector3.zero;

            ApplyContainer(src, sound);
            src.pitch *= pitchMultiplier;
            src.clip = PickClip(sound);

            if (spatial)
                ApplySpatial(index, src, sound, position, follow);
            else
                src.spatialBlend = 0f;

            src.Play();
            _startTimes[index] = Time.unscaledTime;

            float fade = _settings.TailFadeSeconds;
            if (!sound.Loop && fade > 0f)
                _fadeRoutines[index] = StartCoroutine(TailFadeRoutine(index, src, fade));
        }

        private void ApplySpatial(int index, AudioSource src, SoundContainerSO sound, Vector3 position, Transform follow)
        {
            float blend = sound.SpatialBlend;
            if (blend < Min3DSpatialBlend)
                blend = Min3DSpatialBlend;

            src.spatialBlend = blend;
            src.rolloffMode = _settings.Default3DRolloff;
            src.minDistance = _settings.Default3DMinDistance;
            src.maxDistance = _settings.Default3DMaxDistance;
            src.dopplerLevel = 0f;

            if (follow != null)
            {
                _followTargets[index] = follow;
                src.transform.position = follow.position;
            }
            else
            {
                src.transform.position = position;
            }
        }

        private void LateUpdate()
        {
            if (_pool == null)
                return;

            for (int i = 0; i < _pool.Length; i++)
            {
                Transform follow = _followTargets[i];
                if (follow == null)
                    continue;

                AudioSource src = _pool[i];
                if (!src.isPlaying)
                {
                    _followTargets[i] = null;
                    continue;
                }

                src.transform.position = follow.position;
            }
        }

        private int PickPoolIndex()
        {
            for (int i = 0; i < _pool.Length; i++)
            {
                if (!_pool[i].isPlaying)
                    return i;
            }

            int oldest = 0;
            float oldestTime = _startTimes[0];
            for (int i = 1; i < _startTimes.Length; i++)
            {
                if (_startTimes[i] < oldestTime)
                {
                    oldestTime = _startTimes[i];
                    oldest = i;
                }
            }

            return oldest;
        }

        private IEnumerator TailFadeRoutine(int index, AudioSource src, float fade)
        {
            float pitch = Mathf.Max(0.01f, Mathf.Abs(src.pitch));
            float totalPlay = src.clip.length / pitch;
            float wait = totalPlay - fade;
            if (wait > 0f)
                yield return new WaitForSecondsRealtime(wait);

            float startVol = src.volume;
            float t = 0f;
            while (t < fade && src.isPlaying)
            {
                t += Time.unscaledDeltaTime;
                float k = t / fade;
                if (k > 1f)
                    k = 1f;

                src.volume = Mathf.Lerp(startVol, 0f, k);
                yield return null;
            }

            if (src.isPlaying)
                src.volume = 0f;

            _fadeRoutines[index] = null;
        }

        public static void ApplyContainer(AudioSource src, SoundContainerSO sound)
        {
            src.loop = sound.Loop;
            src.volume = sound.Volume + (sound.VolumeVariance > 0f ? Random.Range(-sound.VolumeVariance, sound.VolumeVariance) : 0f);
            src.pitch = sound.Pitch + (sound.PitchVariance > 0f ? Random.Range(-sound.PitchVariance, sound.PitchVariance) : 0f);
            src.spatialBlend = sound.SpatialBlend;
            src.panStereo = sound.StereoPan;
        }

        public static AudioClip PickClip(SoundContainerSO sound)
        {
            if (sound.RandomSound && sound.AudioClips.Count > 1)
                return sound.AudioClips[Random.Range(0, sound.AudioClips.Count)];

            return sound.AudioClips[0];
        }
    }
}
