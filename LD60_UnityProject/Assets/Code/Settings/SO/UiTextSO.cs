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

        [field: SerializeField] public LocalizedString SelfFrameLabel { get; private set; } =
            new LocalizedString("Я", "Me");

        [field: SerializeField] public LocalizedString UnknownFrameLabel { get; private set; } =
            new LocalizedString("???", "???");

        [field: SerializeField] public LocalizedString InterceptProgressFormat { get; private set; } =
            new LocalizedString("{0} / {1}", "{0} / {1}");

        [field: SerializeField] public LocalizedString MusicVolumeLabel { get; private set; } =
            new LocalizedString("МУЗЫКА", "MUSIC");

        [field: SerializeField] public LocalizedString SoundVolumeLabel { get; private set; } =
            new LocalizedString("ЗВУК", "SOUND");

        [field: SerializeField] public LocalizedString VolumeValueFormat { get; private set; } =
            new LocalizedString("{0}%", "{0}%");

        [field: SerializeField] public LocalizedString SkipHint { get; private set; } =
            new LocalizedString("Удерживайте Q, чтобы пропустить", "Hold Q to skip");

        [field: SerializeField] public LocalizedString FinishLabel { get; private set; } =
            new LocalizedString("ЗАВЕРШИТЬ", "FINISH");

        [field: SerializeField] public LocalizedString HelpText { get; private set; } =
            new LocalizedString(
                "УПРАВЛЕНИЕ\n\n" +
                "ЛКМ — навести антенну, ЛКМ ещё раз — отправить сигнал\n" +
                "ПКМ — отменить прицеливание\n" +
                "КОЛЕСО — ширина сигнала: шире — медленнее и плотнее, уже — быстрее и реже\n" +
                "V — пластинка: пойманные голоса, ЛКМ по портрету — послушать\n" +
                "ПРОБЕЛ или СКМ — пауза\n" +
                "Z — сменить язык\n" +
                "Q — это меню\n\n" +
                "Чтобы заговорить с аппаратом, надо поймать несколько его сигналов.\n" +
                "Заряд батареи — внизу слева, дата — внизу справа.",
                "CONTROLS\n\n" +
                "LMB - aim the antenna, LMB again - send the signal\n" +
                "RMB - cancel aiming\n" +
                "WHEEL - signal width: wider is slower and denser, narrower is faster and sparser\n" +
                "V - the record: caught voices, LMB on a portrait to listen\n" +
                "SPACE or MMB - pause\n" +
                "Z - switch language\n" +
                "Q - this menu\n\n" +
                "To talk to a craft you must catch several of its signals.\n" +
                "Battery charge is in the bottom left, the date in the bottom right.");

        [field: SerializeField] public LocalizedString AimReadoutFormat { get; private set; } =
            new LocalizedString(
                "{0:0.0} град   {1:0.0} ед/с   {3} точек   {2:0.0} Вт\nДержать ЛКМ — мощность, отпустить — пуск · ПКМ отмена",
                "{0:0.0} deg   {1:0.0} u/s   {3} dots   {2:0.0} W\nHold LMB for power, release to send · RMB cancel");
    }
}
