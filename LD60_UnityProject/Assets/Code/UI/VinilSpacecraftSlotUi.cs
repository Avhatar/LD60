using LD60;
using LD60.Dialogue;
using LD60.UI;
using NodeGraphAvr.Dialogue;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class VinilSpacecraftSlotUi : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private const string SlotNamePrefix = "VinilSpacecraftSlot";

    [SerializeField]
    private RectTransform questionMarkRectTransform;

    [SerializeField]
    private Image spacecraftIconImage;

    [SerializeField]
    private SpacecraftSO relatedSpacecraft;

    [SerializeField]
    private LocalizedText spacecraftNameLabel;

    [SerializeField]
    private Outline _hoverOutline;

    private DialogueUi _dialogueUi;
    private int _infoIndex;

    public void OnPointerEnter(PointerEventData eventData)
    {
        ShowOutline(CanOpenInfo());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ShowOutline(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (!CanOpenInfo())
            return;

        DialogueGraph[] entries = relatedSpacecraft.InfoDialogues;

        if (_infoIndex >= entries.Length)
            _infoIndex = 0;

        DialogueGraph entry = entries[_infoIndex];
        _infoIndex = (_infoIndex + 1) % entries.Length;

        if (entry == null)
        {
            Debug.LogError($"{nameof(VinilSpacecraftSlotUi)}: an element of {nameof(SpacecraftSO.InfoDialogues)} is null on '{relatedSpacecraft.name}' in {nameof(OnPointerClick)}. " +
                           "Required because the record slot walks that list entry by entry when its portrait is clicked. " +
                           $"Fix: fill the empty element on the '{relatedSpacecraft.name}' spacecraft asset, or shorten the list.", this);
            return;
        }

        ShowOutline(false);
        _dialogueUi.Play(entry);
    }

    private bool CanOpenInfo()
    {
        if (relatedSpacecraft == null || _dialogueUi == null)
            return false;

        if (!Core.IsSpacecraftOnRecord(relatedSpacecraft))
            return false;

        if (_dialogueUi.IsRunning)
            return false;

        return relatedSpacecraft.InfoDialogues.Length > 0;
    }

    private void ShowOutline(bool shown)
    {
        if (_hoverOutline == null || _hoverOutline.enabled == shown)
            return;

        _hoverOutline.enabled = shown;
    }

    public void Refresh(DialogueUi dialogueUi)
    {
        _dialogueUi = dialogueUi;
        ShowOutline(false);

        if (_hoverOutline == null)
            Debug.LogError($"{nameof(VinilSpacecraftSlotUi)}: {nameof(_hoverOutline)} is null in {nameof(Refresh)} on '{name}'. " +
                           "Required to light the slot green while the cursor is over it, which is the only sign the portrait can be clicked. " +
                           "Fix: assign the Outline component of the slot's background Image in the Inspector.", this);

        if (questionMarkRectTransform == null)
        {
            Debug.LogError($"{nameof(VinilSpacecraftSlotUi)}: {nameof(questionMarkRectTransform)} is null in {nameof(Refresh)} on '{name}'. " +
                           "Required to show that this slot of the record is still empty. " +
                           "Fix: assign the Question child to that slot in the Inspector.", this);
            return;
        }

        if (spacecraftIconImage == null)
        {
            Debug.LogError($"{nameof(VinilSpacecraftSlotUi)}: {nameof(spacecraftIconImage)} is null in {nameof(Refresh)} on '{name}'. " +
                           "Required to draw the portrait once its spacecraft has been talked to. " +
                           "Fix: assign the MachineIcon child to that slot in the Inspector.", this);
            return;
        }

        if (relatedSpacecraft == null)
        {
            Debug.LogError($"{nameof(VinilSpacecraftSlotUi)}: {nameof(relatedSpacecraft)} is null in {nameof(Refresh)} on '{name}'. " +
                           "Required to know whose track this slot of the record holds, so it can never fill. " +
                           "Fix: assign a Spacecraft asset to that slot in the Inspector.", this);
            return;
        }

        bool onRecord = Core.IsSpacecraftOnRecord(relatedSpacecraft);

        if (onRecord && spacecraftIconImage.sprite != relatedSpacecraft.DialoguePortrait)
            spacecraftIconImage.sprite = relatedSpacecraft.DialoguePortrait;

        if (spacecraftNameLabel != null)
            spacecraftNameLabel.SetText(relatedSpacecraft.LocalizedName);
        else
            Debug.LogError($"{nameof(VinilSpacecraftSlotUi)}: {nameof(spacecraftNameLabel)} is null in {nameof(Refresh)} on '{name}'. " +
                           "Required to write the spacecraft's name under its portrait once the slot is filled. " +
                           "Fix: assign the SpacecraftName child of MachineIcon to that slot in the Inspector.", this);

        questionMarkRectTransform.gameObject.SetActive(!onRecord);
        spacecraftIconImage.gameObject.SetActive(onRecord);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying)
            return;

        UnityEditor.EditorApplication.delayCall -= ApplyRelatedSpacecraft;
        UnityEditor.EditorApplication.delayCall += ApplyRelatedSpacecraft;
    }

    public void ApplyRelatedSpacecraft()
    {
        if (this == null || Application.isPlaying)
            return;

        if (spacecraftIconImage == null)
        {
            Debug.LogError($"{nameof(VinilSpacecraftSlotUi)}: {nameof(spacecraftIconImage)} is null in {nameof(ApplyRelatedSpacecraft)} on '{name}'. " +
                           "Required to draw the portrait of the spacecraft this record slot stands for. " +
                           "Fix: assign the slot's icon Image in the Inspector.", this);
            return;
        }

        if (_hoverOutline == null)
        {
            _hoverOutline = GetComponentInChildren<Outline>(true);

            if (_hoverOutline != null)
                UnityEditor.EditorUtility.SetDirty(this);
        }

        if (spacecraftNameLabel == null)
        {
            spacecraftNameLabel = GetComponentInChildren<LocalizedText>(true);

            if (spacecraftNameLabel != null)
                UnityEditor.EditorUtility.SetDirty(this);
        }

        Sprite portrait = null;
        string slotName = SlotNamePrefix;

        if (relatedSpacecraft != null)
        {
            portrait = relatedSpacecraft.DialoguePortrait;

            if (portrait == null)
                Debug.LogError($"{nameof(VinilSpacecraftSlotUi)}: {nameof(SpacecraftSO.DialoguePortrait)} is null on '{relatedSpacecraft.name}' in {nameof(ApplyRelatedSpacecraft)} on '{name}'. " +
                               "Required because the record slot shows that portrait once the spacecraft has been talked to. " +
                               $"Fix: assign Dialogue Portrait on the '{relatedSpacecraft.name}' spacecraft asset.", this);

            string displayName = relatedSpacecraft.DisplayName;

            if (string.IsNullOrEmpty(displayName))
                displayName = relatedSpacecraft.name;

            slotName = SlotNamePrefix + "_" + displayName;
        }

        bool changed = false;

        if (spacecraftIconImage.sprite != portrait)
        {
            spacecraftIconImage.sprite = portrait;
            UnityEditor.EditorUtility.SetDirty(spacecraftIconImage);
            changed = true;
        }

        if (gameObject.name != slotName)
        {
            gameObject.name = slotName;
            UnityEditor.EditorUtility.SetDirty(gameObject);
            changed = true;
        }

        if (spacecraftNameLabel != null && relatedSpacecraft != null)
        {
            TMP_Text nameText = spacecraftNameLabel.GetComponent<TMP_Text>();

            if (nameText != null && nameText.text != relatedSpacecraft.LocalizedName.Value)
            {
                spacecraftNameLabel.SetText(relatedSpacecraft.LocalizedName);
                UnityEditor.EditorUtility.SetDirty(spacecraftNameLabel);
                UnityEditor.EditorUtility.SetDirty(nameText);
                changed = true;
            }
        }

        if (changed && gameObject.scene.IsValid())
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }
#endif
}
