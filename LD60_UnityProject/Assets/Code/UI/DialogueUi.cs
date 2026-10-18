using System;
using LD60;
using LD60.Dialogue;
using NodeGraphAvr;
using NodeGraphAvr.Dialogue;
using UnityEngine;
using UnityEngine.InputSystem;

public class DialogueUi : MonoBehaviour
{
    [SerializeField]
    private DialogueReplicUi dialoguePlayerReplicUiPrefab;

    [SerializeField]
    private DialogueReplicUi dialogueNpcReplicUiPrefab;

    [SerializeField]
    private RectTransform dialoguePlayerReplicUiRoot;

    [SerializeField]
    private RectTransform dialogueNpcReplicUiRoot;

    [SerializeField]
    private DialogueFlags _flags;

    private DialogueReplicUi _playerReplic;
    private DialogueReplicUi _npcReplic;
    private DialogueReplicUi _typingReplic;
    private DialogueRunner _runner;
    private DialogueGraph _instance;
    private ChoiceRequest _choice;
    private SayRequest _pendingSay;
    private ChoiceRequest _pendingSelection;
    private SpacecraftSO _player;
    private int _pendingSelectionIndex;
    private int _ignoreClickFrame = -1;
    private GameMode _modeBeforePlay;

    public event Action<bool> Finished;

    public bool IsRunning
    {
        get { return _runner != null && _runner.IsRunning; }
    }

    public bool IsWaitingForChoice
    {
        get { return _choice != null; }
    }

    public void Play(DialogueGraph graph)
    {
        if (graph == null)
        {
            Debug.LogError($"{nameof(DialogueUi)}: {nameof(graph)} is null in {nameof(Play)}. " +
                           "Required to know which conversation to run. " +
                           "Fix: pass an imported .ngdlg asset.", this);
            return;
        }

        if (_flags == null)
        {
            Debug.LogError($"{nameof(DialogueUi)}: {nameof(_flags)} is null in {nameof(Play)}. " +
                           "Required as the object whose flags dialogue conditions and actions read and write. " +
                           "Fix: assign the object carrying DialogueFlags to the Flags slot on this object.", this);
            return;
        }

        Stop();
        Clear();

        _modeBeforePlay = Core.Mode == GameMode.Dialogue ? GameMode.Game : Core.Mode;
        Core.SetMode(GameMode.Dialogue);
        _ignoreClickFrame = Time.frameCount;
        _player = ResolvePlayer(graph);
        _instance = Instantiate(graph);
        _instance.RebuildRuntimeState();

        _runner = new DialogueRunner();
        _runner.Init(_instance, new ActionContext(_flags.gameObject, new MonoBehaviourCoroutineHost(this), this));
        _runner.SayRequested += OnSayRequested;
        _runner.ChoiceRequested += OnChoiceRequested;
        _runner.Finished += OnFinished;
        _runner.Play();
    }

    public void Stop()
    {
        if (_runner == null)
            return;

        if (_runner.IsRunning)
            _runner.Stop(false);

        Release();
    }

    public void Clear()
    {
        _choice = null;
        _pendingSay = null;
        _pendingSelection = null;
        _typingReplic = null;

        DialogueReplicUi player = ResolveReplic(true);
        if (player != null)
            player.Hide();

        DialogueReplicUi npc = ResolveReplic(false);
        if (npc != null)
            npc.Hide();
    }

    public void SelectOption(int index)
    {
        if (_choice == null)
            return;

        if (index < 0 || index >= _choice.Options.Count)
        {
            Debug.LogError($"{nameof(DialogueUi)}: option {index} does not exist in {nameof(SelectOption)}; " +
                           $"the current choice offers {_choice.Options.Count}. The click is ignored. " +
                           "Fix: only options drawn by the current choice may report a pick.", this);
            return;
        }

        ChoiceRequest request = _choice;
        _choice = null;
        _ignoreClickFrame = Time.frameCount;

        DialogueReplicUi replic = ResolveReplic(true);
        if (replic == null)
        {
            request.Select(index);
            return;
        }

        HideOtherCaret(true);
        replic.Show(_player == null ? null : _player.DialoguePortrait, request.Options[index]);
        replic.BeginTyping();

        _typingReplic = replic;
        _pendingSelection = request;
        _pendingSelectionIndex = index;
    }

    private void Update()
    {
        if (!IsRunning || !Core.IsDialogueMode || _choice != null || Time.frameCount == _ignoreClickFrame)
            return;

        if (!ClickRequested())
            return;

        if (_typingReplic != null && _typingReplic.IsTyping)
        {
            _typingReplic.CompleteTyping();
            return;
        }

        Continue();
    }

    private void OnDestroy()
    {
        Stop();
    }

    private static bool ClickRequested()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null)
            return false;

        return mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame;
    }

    private void Continue()
    {
        if (_pendingSay != null)
        {
            SayRequest request = _pendingSay;
            _pendingSay = null;
            _typingReplic = null;
            request.Advance();
            return;
        }

        if (_pendingSelection == null)
            return;

        ChoiceRequest selection = _pendingSelection;
        int index = _pendingSelectionIndex;
        _pendingSelection = null;
        _typingReplic = null;
        selection.Select(index);
    }

    private void OnSayRequested(SayRequest request)
    {
        SpacecraftSO craft = request.Actor as SpacecraftSO;
        bool isPlayer = craft != null && craft.IsPlayer;

        DialogueReplicUi replic = ResolveReplic(isPlayer);
        if (replic == null)
        {
            request.Advance();
            return;
        }

        HideOtherCaret(isPlayer);
        replic.Show(craft == null ? null : craft.DialoguePortrait, request.Text);
        replic.BeginTyping();

        _typingReplic = replic;
        _pendingSay = request;
    }

    private void OnChoiceRequested(ChoiceRequest request)
    {
        _choice = request;
        _typingReplic = null;

        DialogueReplicUi replic = ResolveReplic(true);
        if (replic == null)
            return;

        HideOtherCaret(true);
        replic.ShowOptions(this, request.Options, _player == null ? null : _player.DialoguePortrait);
    }

    private void HideReplics()
    {
        if (_playerReplic != null)
            _playerReplic.Hide();

        if (_npcReplic != null)
            _npcReplic.Hide();
    }

    private void HideOtherCaret(bool player)
    {
        DialogueReplicUi other = player ? _npcReplic : _playerReplic;
        if (other != null)
            other.HideCaret();
    }

    private void OnFinished(bool success)
    {
        _choice = null;
        _pendingSay = null;
        _pendingSelection = null;
        _typingReplic = null;
        Release();

        if (Finished != null)
            Finished.Invoke(success);
    }

    private DialogueReplicUi ResolveReplic(bool player)
    {
        if (player && _playerReplic != null)
            return _playerReplic;

        if (!player && _npcReplic != null)
            return _npcReplic;

        RectTransform root = player ? dialoguePlayerReplicUiRoot : dialogueNpcReplicUiRoot;
        if (root == null)
        {
            Debug.LogError($"{nameof(DialogueUi)}: the {(player ? "player" : "npc")} replic root is empty in {nameof(ResolveReplic)}, " +
                           "so that side of the conversation has nowhere to be drawn. " +
                           "Fix: assign both replic roots on this object.", this);
            return null;
        }

        DialogueReplicUi replic = root.GetComponentInChildren<DialogueReplicUi>(true);
        if (replic == null)
        {
            DialogueReplicUi prefab = player ? dialoguePlayerReplicUiPrefab : dialogueNpcReplicUiPrefab;
            if (prefab == null)
            {
                Debug.LogError($"{nameof(DialogueUi)}: the {(player ? "player" : "npc")} replic root holds no replic and its prefab slot is empty " +
                               $"in {nameof(ResolveReplic)}, so that side of the conversation cannot be built. " +
                               "Fix: put a replic under the root in the scene, or assign the prefab on this object.", this);
                return null;
            }

            replic = Instantiate(prefab, root, false);
            replic.RectTransform.anchoredPosition = Vector2.zero;
        }

        if (player)
            _playerReplic = replic;
        else
            _npcReplic = replic;

        return replic;
    }

    private void Release()
    {
        Core.SetMode(_modeBeforePlay);
        HideReplics();

        if (_runner != null)
        {
            _runner.SayRequested -= OnSayRequested;
            _runner.ChoiceRequested -= OnChoiceRequested;
            _runner.Finished -= OnFinished;
            _runner = null;
        }

        if (_instance != null)
        {
            Destroy(_instance);
            _instance = null;
        }
    }

    private SpacecraftSO ResolvePlayer(DialogueGraph graph)
    {
        for (int i = 0; i < graph.Actors.Count; i++)
        {
            SpacecraftSO craft = graph.Actors[i] as SpacecraftSO;
            if (craft != null && craft.IsPlayer)
                return craft;
        }

        return null;
    }
}
