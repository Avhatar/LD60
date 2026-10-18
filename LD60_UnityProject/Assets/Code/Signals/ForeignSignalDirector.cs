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
        private WattSO _watts;
        private ActPeriod _act;
        private ActSpacecraftSO[] _senders = new ActSpacecraftSO[0];
        private float[] _countdowns = new float[0];
        private int[] _captured = new int[0];
        private bool[] _discovered = new bool[0];
        private bool[][] _usedDialogues = new bool[0][];
        private bool[] _contacted = new bool[0];
        private float[] _dialogueTimers = new float[0];
        private ForeignSignal[] _dialoguePending = new ForeignSignal[0];
        private bool[] _seenEveryLine = new bool[0];
        private int[] _dialogueQueue = new int[0];
        private ForeignSignal[] _queuedSignals = new ForeignSignal[0];
        private bool[] _queuedContacts = new bool[0];
        private int _queueCount;

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

        public bool IsCraftContacted(LD60.Dialogue.SpacecraftSO craft)
        {
            if (craft == null)
                return false;

            for (int i = 0; i < _senders.Length; i++)
            {
                if (_senders[i] != null && _senders[i].Spacecraft == craft)
                    return _contacted[i];
            }

            return false;
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

        public bool HasNothingLeftToSay(int index)
        {
            return _seenEveryLine[index];
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

            _watts = Core.Config.Watt;

            if (_watts == null)
            {
                Debug.LogError($"{nameof(ForeignSignalDirector)}: {nameof(WattSO)} is null in {nameof(Start)}. " +
                               "Required to know how many watts a caught foreign dot hands back. " +
                               "Fix: assign a Watt asset to the Watt slot on the CoreSO asset.");
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
            CollideForeignDots();
            CheckContactShots();
            TickDialogueTimers();

            for (int i = 0; i < _senders.Length; i++)
            {
                if (!_senders[i].Transmits)
                    continue;

                _countdowns[i] -= Time.deltaTime;

                if (_countdowns[i] > 0f)
                    continue;

                _countdowns[i] = _senders[i].TransmitPeriodSeconds;
                Transmit(_senders[i]);
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
            _captured = new int[count];
            _discovered = new bool[count];
            _usedDialogues = new bool[count][];
            _contacted = new bool[count];
            _dialogueTimers = new float[count];
            _dialoguePending = new ForeignSignal[count];
            _seenEveryLine = new bool[count];
            _dialogueQueue = new int[count * 2];
            _queuedSignals = new ForeignSignal[count * 2];
            _queuedContacts = new bool[count * 2];
            _queueCount = 0;

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

                if (_act.Spacecraft[i].KnownFromStart)
                {
                    _captured[slot] = _act.Spacecraft[i].DotsToCapture;
                    _discovered[slot] = true;
                }

                slot++;
            }
        }

        private void CaptureDots()
        {
            if (_launcher == null)
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

                    for (int s = 0; s < _launcher.LiveSignalCount; s++)
                    {
                        Signal ours = _launcher.LiveSignal(s);

                        if (ours == null)
                            continue;

                        int ourDot = FindOurDotNear(ours, theirs.DotPosition(d), radiusSquared);

                        if (ourDot < 0)
                            continue;

                        ours.MarkDotSpent(ourDot);
                        theirs.ConsumeDot(d);
                        RegisterCapture(sender, theirs, ours);
                        break;
                    }
                }
            }
        }

        private void CollideForeignDots()
        {
            float radius = _settings.CaptureRadius;
            float radiusSquared = radius * radius;

            for (int a = 0; a < transform.childCount; a++)
            {
                ForeignSignal first = transform.GetChild(a).GetComponent<ForeignSignal>();

                if (first == null)
                    continue;

                for (int b = a + 1; b < transform.childCount; b++)
                {
                    ForeignSignal second = transform.GetChild(b).GetComponent<ForeignSignal>();

                    if (second == null)
                        continue;

                    TripOverlappingDots(first, second, radiusSquared);
                }
            }
        }

        private void TripOverlappingDots(ForeignSignal first, ForeignSignal second, float radiusSquared)
        {
            for (int i = 0; i < first.DotCount; i++)
            {
                if (!first.IsDotCapturable(i))
                    continue;

                Vector2 position = first.DotPosition(i);

                for (int j = 0; j < second.DotCount; j++)
                {
                    if (!second.IsDotCapturable(j))
                        continue;

                    if ((second.DotPosition(j) - position).sqrMagnitude > radiusSquared)
                        continue;

                    first.TripDot(i);
                    second.TripDot(j);
                    break;
                }
            }
        }

        private void CheckContactShots()
        {
            if (_launcher == null)
                return;

            for (int i = 0; i < _senders.Length; i++)
            {
                if (!IsAwaitingContact(i))
                    continue;

                Transform target = SenderObject(i);

                if (target == null)
                    continue;

                if (!AnyDotLandedOn(target))
                    continue;

                _contacted[i] = true;
                PlaySound(_settings.ContactShotSound);
                EnqueueDialogue(i, null, true);
            }
        }

        private bool AnyDotLandedOn(Transform target)
        {
            for (int s = 0; s < _launcher.LiveSignalCount; s++)
            {
                Signal ours = _launcher.LiveSignal(s);

                if (ours == null)
                    continue;

                for (int d = 0; d < ours.DotCount; d++)
                {
                    if (ours.DotLandedOn(d) == target)
                        return true;
                }
            }

            return false;
        }

        private bool StartContactDialogue(int sender)
        {
            NodeGraphAvr.Dialogue.DialogueGraph graph = _senders[sender].ContactDialogue;

            if (graph == null)
            {
                Debug.LogError($"{nameof(ForeignSignalDirector)}: {nameof(ActSpacecraftSO.ContactDialogue)} is null on '{_senders[sender].name}' in {nameof(StartContactDialogue)}. " +
                               "Required because contact with it has just been made and the direct conversation is what the whole interception builds to. " +
                               $"Fix: assign a dialogue asset to the Contact Dialogue slot on that {nameof(ActSpacecraftSO)} asset.");
                return false;
            }

            _dialogueUi.Play(graph);
            return true;
        }

        private void RegisterCapture(int sender, ForeignSignal theirs, Signal ours)
        {
            bool alreadyUnlocked = IsUnlocked(sender);

            if (!alreadyUnlocked)
            {
                _captured[sender]++;
                _discovered[sender] = true;
            }

            bool justUnlocked = !alreadyUnlocked && IsUnlocked(sender);

            if (justUnlocked)
            {
                PlaySound(_settings.ContactSound);
                theirs.ApplyBaseLook(true, _seenEveryLine[sender]);
            }

            bool windowOpen = _dialogueTimers[sender] > 0f;

            if (!windowOpen && !justUnlocked && !alreadyUnlocked)
                PlaySound(_settings.ReplySound);

            ours.SetCaptureLook(_seenEveryLine[sender]
                ? _settings.ExhaustedCaptureColor
                : _settings.ArrivedColor);

            _dialoguePending[sender] = theirs;
            _dialogueTimers[sender] = _settings.DialogueDebounceSeconds;
            Core.AddWatts(_watts.RefundPerCaughtDot);
        }

        private void TickDialogueTimers()
        {
            for (int i = 0; i < _dialogueTimers.Length; i++)
            {
                if (_dialogueTimers[i] <= 0f)
                    continue;

                _dialogueTimers[i] -= Time.deltaTime;

                if (_dialogueTimers[i] > 0f)
                    continue;

                _dialogueTimers[i] = 0f;
                EnqueueDialogue(i, _dialoguePending[i], false);
                _dialoguePending[i] = null;
            }

            PumpDialogueQueue();
        }

        private void EnqueueDialogue(int sender, ForeignSignal source, bool contact)
        {
            for (int i = 0; i < _queueCount; i++)
            {
                if (_dialogueQueue[i] != sender || _queuedContacts[i] != contact)
                    continue;

                _queuedSignals[i] = source;
                return;
            }

            if (_queueCount == _dialogueQueue.Length)
                return;

            _dialogueQueue[_queueCount] = sender;
            _queuedSignals[_queueCount] = source;
            _queuedContacts[_queueCount] = contact;
            _queueCount++;
        }

        private void PumpDialogueQueue()
        {
            while (_queueCount > 0)
            {
                if (_dialogueUi == null)
                {
                    Debug.LogError($"{nameof(ForeignSignalDirector)}: {nameof(_dialogueUi)} is null in {nameof(PumpDialogueQueue)}. " +
                                   "Required to run every line an interception or a contact earns, so they are all being dropped. " +
                                   "Fix: assign the Dialogue UI object to the Dialogue Ui slot on this object.");
                    _queueCount = 0;
                    return;
                }

                if (_dialogueUi.IsRunning)
                    return;

                int sender = _dialogueQueue[0];
                ForeignSignal source = _queuedSignals[0];
                bool contact = _queuedContacts[0];
                DropQueueHead();

                bool started = contact
                    ? StartContactDialogue(sender)
                    : StartInterceptDialogue(sender, source);

                if (started)
                    return;
            }
        }

        private void DropQueueHead()
        {
            _queueCount--;

            for (int i = 0; i < _queueCount; i++)
            {
                _dialogueQueue[i] = _dialogueQueue[i + 1];
                _queuedSignals[i] = _queuedSignals[i + 1];
                _queuedContacts[i] = _queuedContacts[i + 1];
            }

            _queuedSignals[_queueCount] = null;
        }

        private bool StartInterceptDialogue(int sender, ForeignSignal source)
        {
            NodeGraphAvr.Dialogue.DialogueGraph graph = PickUnusedDialogue(sender, !IsUnlocked(sender));

            if (_seenEveryLine[sender] && source != null)
                source.ApplyBaseLook(IsUnlocked(sender), true);

            if (graph == null)
                return false;

            _dialogueUi.Play(graph);
            return true;
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
                _seenEveryLine[sender] = true;

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
                && (entry.Transmits || entry.KnownFromStart);
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
                          senderIndex >= 0 && IsUnlocked(senderIndex),
                          senderIndex >= 0 && _seenEveryLine[senderIndex]);

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

            double elapsed = _act == null ? 0d : Core.DaysSinceJ2000 - _act.StartDaysSinceJ2000;
            double span = _act == null ? 0d : _act.SpanDays;

            float radius = Core.Config.SolarSystem.DisplayRadiusForAu(freeFlyer.DistanceAuAt(elapsed, span));
            float radians = freeFlyer.AngleDegreesAt(elapsed) * Mathf.Deg2Rad;
            position = new Vector2(Mathf.Cos(radians) * radius, Mathf.Sin(radians) * radius);
            return true;
        }
    }
}
