using UnityEngine;
using LD60.Settings;
using LD60.Space;

namespace LD60.Signals
{
    public class ForeignSignalDirector : MonoBehaviour
    {
        [SerializeField] private Orrery _orrery;
        [SerializeField] private Material _dotMaterial;

        private SignalSO _settings;
        private ActPeriod _act;
        private ActSpacecraftSO[] _senders = new ActSpacecraftSO[0];
        private float[] _countdowns = new float[0];
        private ForeignSignal[] _lastSent = new ForeignSignal[0];

        private void Start()
        {
            if (Core.Config == null)
            {
                Debug.LogError($"{nameof(ForeignSignalDirector)}: {nameof(Core)}.{nameof(Core.Config)} is null in {nameof(Start)}. " +
                               "Required to read the signal settings and the current act. " +
                               "Fix: add a Core object to the scene and assign the CoreSO asset to its Config slot.");
                enabled = false;
                return;
            }

            _settings = Core.Config.Signal;

            if (_settings == null)
            {
                Debug.LogError($"{nameof(ForeignSignalDirector)}: {nameof(SignalSO)} is null in {nameof(Start)}. " +
                               "Required to know how foreign transmissions look and how far they travel. " +
                               "Fix: assign a Signal asset to the Signal slot on the CoreSO asset.");
                enabled = false;
                return;
            }

            if (_orrery == null)
            {
                Debug.LogError($"{nameof(ForeignSignalDirector)}: {nameof(_orrery)} is null in {nameof(Start)}. " +
                               "Required to find where each sender and each receiver currently is. " +
                               "Fix: assign the Orrery object to the Orrery slot on this object.");
                enabled = false;
            }
        }

        private void Update()
        {
            if (!ReferenceEquals(Core.CurrentAct, _act))
                RebuildSenders();

            if (!Core.IsGameMode)
                return;

            for (int i = 0; i < _senders.Length; i++)
            {
                _countdowns[i] -= Time.deltaTime;

                if (_countdowns[i] > 0f)
                    continue;

                _countdowns[i] = _senders[i].TransmitPeriodSeconds;

                if (_lastSent[i] != null)
                    Destroy(_lastSent[i].gameObject);

                _lastSent[i] = Transmit(_senders[i]);
            }
        }

        private void RebuildSenders()
        {
            _act = Core.CurrentAct;

            for (int i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);

            int count = 0;

            if (_act != null)
            {
                for (int i = 0; i < _act.Spacecraft.Length; i++)
                    if (IsSender(_act.Spacecraft[i]))
                        count++;
            }

            _senders = new ActSpacecraftSO[count];
            _countdowns = new float[count];
            _lastSent = new ForeignSignal[count];

            if (_act == null)
                return;

            int slot = 0;

            for (int i = 0; i < _act.Spacecraft.Length; i++)
            {
                if (!IsSender(_act.Spacecraft[i]))
                    continue;

                _senders[slot] = _act.Spacecraft[i];
                _countdowns[slot] = _act.Spacecraft[i].TransmitPeriodSeconds * Random.value;
                slot++;
            }
        }

        private static bool IsSender(ActSpacecraftSO entry)
        {
            return entry != null
                && entry.Spacecraft != null
                && entry.TransmitTo != null
                && entry.TransmitPeriodSeconds > 0f;
        }

        private ForeignSignal Transmit(ActSpacecraftSO entry)
        {
            Vector2 origin;

            if (!TryGetPosition(entry.Body, entry, out origin))
                return null;

            Vector2 aimPoint;

            if (!TryGetLeadPoint(entry, origin, out aimPoint))
                return null;

            float clearance = entry.Body == null
                ? 0f
                : _orrery.GetHitRadiusOf(entry.Body) + _settings.LandedSurfaceOffset;

            GameObject spawned = new GameObject(entry.Spacecraft.DisplayName + " Transmission");
            spawned.transform.SetParent(transform, false);

            ForeignSignal signal = spawned.AddComponent<ForeignSignal>();
            signal.Launch(_settings, entry, _orrery, _dotMaterial, origin, aimPoint, entry.Body, clearance);
            return signal;
        }

        private bool TryGetLeadPoint(ActSpacecraftSO entry, Vector2 origin, out Vector2 aimPoint)
        {
            if (!TryGetPosition(entry.TransmitTo, null, out aimPoint))
                return false;

            ActPeriod act = Core.CurrentAct;

            if (act == null || entry.Speed <= 0f)
                return true;

            for (int i = 0; i < 4; i++)
            {
                float flightSeconds = (aimPoint - origin).magnitude / entry.Speed;
                Vector2 predicted;

                if (!_orrery.TryPredictBodyPosition(entry.TransmitTo, flightSeconds * act.DaysPerSecond, out predicted))
                    return true;

                aimPoint = predicted;
            }

            return true;
        }

        private bool TryGetPosition(CelestialBodySO body, ActSpacecraftSO freeFlyer, out Vector2 position)
        {
            position = Vector2.zero;

            if (body != null)
            {
                Transform found = _orrery.FindBodyTransform(body);

                if (found == null)
                {
                    Debug.LogError($"{nameof(ForeignSignalDirector)}: body '{body.DisplayName}' is not in the orrery in {nameof(TryGetPosition)}. " +
                                   "Required to know where a transmission starts or ends. " +
                                   "Fix: add that body to the Bodies list on the SolarSystem asset.");
                    return false;
                }

                position = found.position;
                return true;
            }

            if (freeFlyer == null)
                return false;

            float radius = Core.Config.SolarSystem.DisplayRadiusForAu(freeFlyer.DistanceAu);
            float radians = freeFlyer.AngleDegrees * Mathf.Deg2Rad;
            position = new Vector2(Mathf.Cos(radians) * radius, Mathf.Sin(radians) * radius);
            return true;
        }
    }
}
