using UnityEngine;
using LD60.Settings;
using LD60.Space;

namespace LD60.Signals
{
    public class ForeignSignalDirector : MonoBehaviour
    {
        [SerializeField] private Orrery _orrery;
        [SerializeField] private Material _dotMaterial;
        [SerializeField] private SignalLauncher _launcher;
        [SerializeField] private DialogueUi _dialogueUi;

        private SignalSO _settings;
        private ActPeriod _act;
        private ActSpacecraftSO[] _senders = new ActSpacecraftSO[0];
        private float[] _countdowns = new float[0];
        private ForeignSignal[] _lastSent = new ForeignSignal[0];
        private int[] _captured = new int[0];
        private bool[] _discovered = new bool[0];
        private bool[][] _usedDialogues = new bool[0][];
        private bool[] _contacted = new bool[0];

        public int SenderCount
        {
            get { return _senders.Length; }
        }

        public ActSpacecraftSO GetSender(int index)
        {
            return _senders[index];
        }

        public bool IsDiscovered(int index)
        {
            return _discovered[index];
        }

        public int CapturedDots(int index)
        {
            return _captured[index];
        }

        public int RemainingDots(int index)
        {
            return Mathf.Max(0, _senders[index].DotsToCapture - _captured[index]);
        }

        public bool TryGetSenderPosition(int index, out Vector2 position)
        {
            return TryGetPosition(_senders[index].Body, _senders[index], out position);
        }

        public bool IsContacted(int index)
        {
            return _contacted[index];
        }

        public bool IsAwaitingContact(int index)
        {
            return IsUnlocked(index) && !_contacted[index];
        }

        public Transform SenderObject(int index)
        {
            ActSpacecraftSO entry = _senders[index];

            return entry.Body != null
                ? _orrery.FindBodyTransform(entry.Body)
                : _orrery.FindSpacecraftTransform(entry.Spacecraft);
        }

        public float SenderObjectRadius(int index)
        {
            ActSpacecraftSO entry = _senders[index];

            return entry.Body != null
                ? _orrery.GetHitRadiusOf(entry.Body)
                : entry.Spacecraft.DisplayDiameter * 0.5f;
        }

        public bool IsUnlocked(int index)
        {
            return _captured[index] >= _senders[index].DotsToCapture;
        }

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

            CaptureDots();
            CheckContactShots();

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
            _captured = new int[count];
            _discovered = new bool[count];
            _usedDialogues = new bool[count][];
            _contacted = new bool[count];

            if (_act == null)
                return;

            int slot = 0;

            for (int i = 0; i < _act.Spacecraft.Length; i++)
            {
                if (!IsSender(_act.Spacecraft[i]))
                    continue;

                _senders[slot] = _act.Spacecraft[i];
                _usedDialogues[slot] = new bool[_act.Spacecraft[i].InterceptDialogues.Length];
                _countdowns[slot] = _act.Spacecraft[i].TransmitPeriodSeconds * Random.value;
                slot++;
            }
        }

        private void CaptureDots()
        {
            if (_launcher == null)
                return;

            Signal ours = _launcher.ActiveSignal;

            if (ours == null)
                return;

            float radius = _settings.CaptureRadius;
            float radiusSquared = radius * radius;

            for (int t = 0; t < transform.childCount; t++)
            {
                ForeignSignal theirs = transform.GetChild(t).GetComponent<ForeignSignal>();

                if (theirs == null)
                    continue;

                int sender = IndexOfSender(theirs.Source);

                if (sender < 0)
                    continue;

                for (int d = 0; d < theirs.DotCount; d++)
                {
                    if (!theirs.IsDotCapturable(d))
                        continue;

                    int ourDot = FindOurDotNear(ours, theirs.DotPosition(d), radiusSquared);

                    if (ourDot < 0)
                        continue;

                    ours.MarkDotSpent(ourDot);
                    theirs.ConsumeDot(d);
                    RegisterCapture(sender, theirs);
                }
            }
        }

        private void CheckContactShots()
        {
            if (_launcher == null)
                return;

            Signal ours = _launcher.ActiveSignal;

            if (ours == null)
                return;

            for (int i = 0; i < _senders.Length; i++)
            {
                if (!IsAwaitingContact(i))
                    continue;

                Transform target = SenderObject(i);

                if (target == null)
                    continue;

                for (int d = 0; d < ours.DotCount; d++)
                {
                    if (ours.DotLandedOn(d) != target)
                        continue;

                    _contacted[i] = true;
                    break;
                }
            }
        }

        private void RegisterCapture(int sender, ForeignSignal theirs)
        {
            if (IsUnlocked(sender))
            {
                if (theirs.Answered)
                    return;

                theirs.MarkAnswered();
                PlayInterceptDialogue(sender, false);
                return;
            }

            _captured[sender]++;
            _discovered[sender] = true;

            bool justUnlocked = IsUnlocked(sender);

            if (justUnlocked)
            {
                PlaySound(_settings.ContactSound);
                theirs.ApplyBaseLook(true);
            }

            if (theirs.Answered)
                return;

            theirs.MarkAnswered();

            if (!justUnlocked)
                PlaySound(_settings.ReplySound);

            PlayInterceptDialogue(sender, true);
        }

        private void PlayInterceptDialogue(int sender, bool allowRepeats)
        {
            if (_dialogueUi == null || _dialogueUi.IsRunning)
                return;

            NodeGraphAvr.Dialogue.DialogueGraph graph = PickUnusedDialogue(sender, allowRepeats);

            if (graph == null)
                return;

            _dialogueUi.Play(graph);
        }

        private NodeGraphAvr.Dialogue.DialogueGraph PickUnusedDialogue(int sender, bool allowRepeats)
        {
            NodeGraphAvr.Dialogue.DialogueGraph[] all = _senders[sender].InterceptDialogues;

            if (all.Length == 0)
                return null;

            bool[] used = _usedDialogues[sender];

            if (used.Length != all.Length)
            {
                used = new bool[all.Length];
                _usedDialogues[sender] = used;
            }

            int unused = 0;

            for (int i = 0; i < all.Length; i++)
            {
                if (!used[i] && all[i] != null)
                    unused++;
            }

            if (unused == 0)
            {
                if (!allowRepeats)
                    return null;

                for (int i = 0; i < used.Length; i++)
                    used[i] = false;

                unused = all.Length;
            }

            int wanted = Random.Range(0, unused);

            for (int i = 0; i < all.Length; i++)
            {
                if (used[i] || all[i] == null)
                    continue;

                if (wanted > 0)
                {
                    wanted--;
                    continue;
                }

                used[i] = true;
                return all[i];
            }

            return null;
        }

        private static void PlaySound(LD60.Settings.SoundContainerSO sound)
        {
            if (sound == null || LD60.Sound.AudioController.Instance == null)
                return;

            LD60.Sound.AudioController.Instance.Play2D(sound);
        }

        private static int FindOurDotNear(Signal ours, Vector2 point, float radiusSquared)
        {
            for (int i = 0; i < ours.DotCount; i++)
            {
                if (!ours.IsDotInFlight(i))
                    continue;

                if ((ours.DotPosition(i) - point).sqrMagnitude <= radiusSquared)
                    return i;
            }

            return -1;
        }

        private int IndexOfSender(ActSpacecraftSO entry)
        {
            for (int i = 0; i < _senders.Length; i++)
            {
                if (_senders[i] == entry)
                    return i;
            }

            return -1;
        }

        private static bool IsSender(ActSpacecraftSO entry)
        {
            return entry != null
                && entry.Spacecraft != null
                && entry.TransmitTo != null
                && !entry.TransmitsOntoItself
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

            Transform senderTransform = entry.Body != null
                ? _orrery.FindBodyTransform(entry.Body)
                : _orrery.FindSpacecraftTransform(entry.Spacecraft);

            float senderRadius = entry.Body != null
                ? _orrery.GetHitRadiusOf(entry.Body)
                : entry.Spacecraft.DisplayDiameter * 0.5f;

            float clearance = senderRadius + _settings.LandedSurfaceOffset;

            GameObject spawned = new GameObject(entry.Spacecraft.LocalizedName.Value + " Transmission");
            spawned.transform.SetParent(transform, false);

            ForeignSignal signal = spawned.AddComponent<ForeignSignal>();
            int senderIndex = IndexOfSender(entry);
            signal.Launch(_settings, entry, _orrery, _dotMaterial, origin, aimPoint, senderTransform, clearance,
                          senderIndex >= 0 && IsUnlocked(senderIndex));

            PlaySound(_settings.ForeignSound);
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
                    Debug.LogError($"{nameof(ForeignSignalDirector)}: body '{body.name}' is not in the orrery in {nameof(TryGetPosition)}. " +
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
