using UnityEngine;

namespace LD60.Settings
{
    [CreateAssetMenu(fileName = "UiText", menuName = "LD60/UI Text")]
    public class UiTextSO : GameDataSO
    {
        [field: SerializeField] public LocalizedString NoCarrier { get; private set; } =
            new LocalizedString("НЕТ НЕСУЩЕЙ", "NO CARRIER");

        [field: SerializeField] public LocalizedString AimHint { get; private set; } =
            new LocalizedString("ЛКМ — прицел", "LMB - aim");

        [field: SerializeField] public LocalizedString Paused { get; private set; } =
            new LocalizedString("ПАУЗА", "PAUSED");

        [field: SerializeField] public LocalizedString HitPrefix { get; private set; } =
            new LocalizedString("ЦЕЛЬ: ", "HIT: ");

        [field: SerializeField] public LocalizedString AimReadoutFormat { get; private set; } =
            new LocalizedString(
                "{0:0.0} град   {1:0.0} ед/с   {3} точек   {2:0.0} Вт\nДержать ЛКМ — мощность, отпустить — пуск · ПКМ отмена",
                "{0:0.0} deg   {1:0.0} u/s   {3} dots   {2:0.0} W\nHold LMB for power, release to send · RMB cancel");
    }
}
