using LD60.Settings;
using LD60.Space;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LD60.Ending
{
    public class EndingTestLauncher : MonoBehaviour
    {
        [SerializeField] private Key _successKey = Key.F5;
        [SerializeField] private Key _powerFailureKey = Key.F6;
        [SerializeField] private Key _timeFailureKey = Key.F7;

        private void Update()
        {
            if (!Application.isEditor)
                return;

            if (Keyboard.current == null)
                return;

            if (Pressed(_successKey))
                ForceSuccess();
            else if (Pressed(_powerFailureKey))
                ForcePowerFailure();
            else if (Pressed(_timeFailureKey))
                ForceTimeFailure();
        }

        public void ForceSuccess()
        {
            ActPeriod act = Core.CurrentAct;

            if (act == null)
            {
                Debug.LogError($"{nameof(EndingTestLauncher)}: {nameof(Core.CurrentAct)} is null in {nameof(ForceSuccess)} on '{name}'. " +
                               "Required to know whose names have to land on the record for the run to count as won. " +
                               "Fix: assign the TimeScale asset to the Time Scale slot on the Core asset.", this);
                return;
            }

            ActSpacecraftSO[] senders = act.Spacecraft;
            int registered = 0;

            for (int i = 0; i < senders.Length; i++)
            {
                if (senders[i] == null || senders[i].Spacecraft == null)
                    continue;

                Core.RegisterSpacecraftContact(senders[i].Spacecraft);
                registered++;
            }

            Debug.Log($"{nameof(EndingTestLauncher)}: put {registered} spacecraft on the record, so the success ending should follow.", this);
        }

        public void ForcePowerFailure()
        {
            Core.TrySpendWatts(Core.Watts);
            Debug.Log($"{nameof(EndingTestLauncher)}: drained the transmitter to {Core.Watts} W, so the power ending should follow.", this);
        }

        public void ForceTimeFailure()
        {
            Core.SkipToActEnd();
            Debug.Log($"{nameof(EndingTestLauncher)}: moved the clock to {Core.CurrentDate:dd.MM.yyyy}, so the time ending should follow.", this);
        }

        private static bool Pressed(Key key)
        {
            return key != Key.None && Keyboard.current[key].wasPressedThisFrame;
        }
    }
}
