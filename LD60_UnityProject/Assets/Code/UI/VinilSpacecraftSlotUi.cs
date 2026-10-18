using LD60;
using LD60.Dialogue;
using UnityEngine;
using UnityEngine.UI;

public class VinilSpacecraftSlotUi : MonoBehaviour
{
    private const string SlotNamePrefix = "VinilSpacecraftSlot";

    [SerializeField]
    private RectTransform questionMarkRectTransform;

    [SerializeField]
    private Image spacecraftIconImage;

    [SerializeField]
    private SpacecraftSO relatedSpacecraft;

    public void Refresh()
    {
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

        if (changed && gameObject.scene.IsValid())
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }
#endif
}
