using System;
using LD60;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DefaultExecutionOrder(100)]
public class VinilRoot : MonoBehaviour
{
    [SerializeField]
    private Image vinilUiToggleButtonImage;
    [SerializeField]
    private RectTransform vinilBaseHuman;
    [SerializeField]
    private RectTransform vinilSpacecraft;
    [SerializeField]
    private Button changeVinilSideButton;
    [SerializeField]
    private VinilSpacecraftSlotUi[] allSpaceCraftsVinilSlots;
    [SerializeField]
    private DialogueUi _dialogueUi;

    [SerializeField]
    private Button finishButton;

    private Button _toggleButton;
    private bool _open;
    private bool _locked;
    private bool _spacecraftSideShown = true;

    public event Action FinishRequested;

    public void OpenFinal()
    {
        _locked = true;
        _spacecraftSideShown = true;
        Open();
    }

    public void CloseFinal()
    {
        _locked = false;
        _open = false;
        ApplySides();
    }

    private void Awake()
    {
        bool wired = Wired(vinilUiToggleButtonImage, nameof(vinilUiToggleButtonImage), "to let the player open and close the record")
                     & Wired(vinilBaseHuman, nameof(vinilBaseHuman), "to show the side the musicians are pressed on")
                     & Wired(vinilSpacecraft, nameof(vinilSpacecraft), "to show the side the run fills with spacecraft")
                     & Wired(changeVinilSideButton, nameof(changeVinilSideButton), "to let the player turn the record over")
                     & Wired(_dialogueUi, nameof(_dialogueUi), "to run the reference entry when the player clicks a portrait on the record");

        if (!wired)
        {
            enabled = false;
            return;
        }

        _toggleButton = vinilUiToggleButtonImage.GetComponent<Button>();

        if (_toggleButton == null)
        {
            Debug.LogError($"{nameof(VinilRoot)}: {nameof(Button)} is missing on '{vinilUiToggleButtonImage.name}' in {nameof(Awake)}. " +
                           "Required to turn the record icon into the control that opens and closes the record. " +
                           "Fix: add a Button component to that object.", this);
            enabled = false;
            return;
        }

        _toggleButton.onClick.AddListener(Toggle);
        changeVinilSideButton.onClick.AddListener(FlipSide);

        if (finishButton != null)
            finishButton.onClick.AddListener(RequestFinish);

        ApplySides();
    }

    private void Update()
    {
        if (_open)
        {
            if (!_locked && CloseRequested())
                Close();

            return;
        }

        if (Core.IsGameMode && OpenRequested())
            Open();
    }

    private void Toggle()
    {
        if (_locked)
            return;

        if (_open)
            Close();
        else if (Core.IsGameMode)
            Open();
    }

    private void RequestFinish()
    {
        if (!_locked)
            return;

        if (FinishRequested != null)
            FinishRequested.Invoke();
    }

    private void Open()
    {
        _open = true;
        ApplySides();
        RefreshSlots();

        if (!_locked)
            Core.SetMode(GameMode.Vinil);
    }

    private void Close()
    {
        _open = false;
        ApplySides();
        Core.SetMode(GameMode.Game);
    }

    private void FlipSide()
    {
        _spacecraftSideShown = !_spacecraftSideShown;
        ApplySides();
    }

    private void ApplySides()
    {
        vinilSpacecraft.gameObject.SetActive(_open && _spacecraftSideShown);
        vinilBaseHuman.gameObject.SetActive(_open && !_spacecraftSideShown);
        changeVinilSideButton.gameObject.SetActive(_open);

        if (vinilUiToggleButtonImage != null)
            vinilUiToggleButtonImage.gameObject.SetActive(!_locked);

        if (finishButton != null)
            finishButton.gameObject.SetActive(_open && _locked);
    }

    private void RefreshSlots()
    {
        if (allSpaceCraftsVinilSlots == null)
            return;

        for (int i = 0; i < allSpaceCraftsVinilSlots.Length; i++)
        {
            if (allSpaceCraftsVinilSlots[i] == null)
            {
                Debug.LogError($"{nameof(VinilRoot)}: {nameof(allSpaceCraftsVinilSlots)}[{i}] is null in {nameof(RefreshSlots)} on '{name}'. " +
                               "Required to fill that slot of the record with the portrait it earned. " +
                               "Fix: drop the missing slot object into that element, or shorten the list.", this);
                continue;
            }

            allSpaceCraftsVinilSlots[i].Refresh(_dialogueUi);
        }
    }

    private bool Wired(UnityEngine.Object reference, string fieldName, string purpose)
    {
        if (reference != null)
            return true;

        Debug.LogError($"{nameof(VinilRoot)}: {fieldName} is null in {nameof(Awake)} on '{name}'. " +
                       $"Required {purpose}. " +
                       "Fix: assign the matching child of VinilRoot to that slot on this object.", this);
        return false;
    }

    private static bool OpenRequested()
    {
        return Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame;
    }

    private static bool CloseRequested()
    {
        if (Keyboard.current == null)
            return false;

        return Keyboard.current.qKey.wasPressedThisFrame || Keyboard.current.vKey.wasPressedThisFrame;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        SyncSlots();
    }

    public void SyncSlots()
    {
        if (Application.isPlaying || vinilSpacecraft == null)
            return;

        VinilSpacecraftSlotUi[] found = vinilSpacecraft.GetComponentsInChildren<VinilSpacecraftSlotUi>(true);

        if (SameSlots(found))
            return;

        allSpaceCraftsVinilSlots = found;
        UnityEditor.EditorUtility.SetDirty(this);

        if (gameObject.scene.IsValid())
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }

    private bool SameSlots(VinilSpacecraftSlotUi[] found)
    {
        if (allSpaceCraftsVinilSlots == null || allSpaceCraftsVinilSlots.Length != found.Length)
            return false;

        for (int i = 0; i < found.Length; i++)
        {
            if (allSpaceCraftsVinilSlots[i] != found[i])
                return false;
        }

        return true;
    }
#endif
}
