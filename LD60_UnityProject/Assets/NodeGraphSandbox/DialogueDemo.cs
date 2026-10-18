using NodeGraphAvr.Dialogue;
using UnityEngine;

namespace NodeGraphAvr.Sandbox
{
    public class DialogueDemo : MonoBehaviour
    {
        [Tooltip("The dialogue asset to play. The demo instantiates a runtime copy on Start Dialogue; the asset itself is never mutated.")]
        [SerializeField] private DialogueGraph _graph;

        [Tooltip("Which scene object plays which actor. Nodes naming a bound actor run their actions on it; unbound actors fall back to this object.")]
        [SerializeField] private System.Collections.Generic.List<ActorAgentBinding> _actorAgents = new System.Collections.Generic.List<ActorAgentBinding>();

        private DialogueGraph _instance;
        private DialogueRunner _runner;
        private SayRequest _say;
        private ChoiceRequest _choice;
        private string _result = "";

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(20f, 20f, 420f, Screen.height - 40f));
            if (_runner == null || !_runner.IsRunning)
                DrawIdle();
            else
                DrawRunning();
            GUILayout.EndArea();
        }

        private void DrawIdle()
        {
            if (!string.IsNullOrEmpty(_result))
                GUILayout.Label(_result);

            if (_graph == null)
            {
                GUILayout.Label("Assign a dialogue graph asset to play.");
                return;
            }

            if (GUILayout.Button("Start Dialogue", GUILayout.Height(32f)))
                StartDialogue();
        }

        private void StartDialogue()
        {
            if (_instance == null)
            {
                _instance = Instantiate(_graph);
                _instance.RebuildRuntimeState();
                _runner = new DialogueRunner();
                _runner.Init(_instance, new ActionContext(gameObject, new MonoBehaviourCoroutineHost(this), this));
                _runner.BindActors(_actorAgents);
                _runner.SayRequested += OnSayRequested;
                _runner.ChoiceRequested += OnChoiceRequested;
                _runner.Finished += OnFinished;
            }

            _say = null;
            _choice = null;
            _result = "";
            _runner.Play();
        }

        private void DrawRunning()
        {
            if (_say != null)
            {
                var speaker = _say.Actor != null ? _say.Actor.DisplayName : "???";
                GUILayout.Label(speaker + ":", GUI.skin.box);
                GUILayout.Label(_say.Text);
                if (GUILayout.Button("Continue", GUILayout.Height(28f)))
                {
                    var request = _say;
                    _say = null;
                    request.Advance();
                }
            }
            else if (_choice != null)
            {
                GUILayout.Label("Choose:", GUI.skin.box);
                for (var i = 0; i < _choice.Options.Count; i++)
                {
                    if (GUILayout.Button(_choice.Options[i].Value, GUILayout.Height(28f)))
                    {
                        var request = _choice;
                        _choice = null;
                        request.Select(i);
                    }
                }
            }
            else
            {
                GUILayout.Label("Waiting for the dialogue...");
            }

            GUILayout.Space(8f);
            if (GUILayout.Button("Stop", GUILayout.Height(22f)))
                _runner.Stop(false);
        }

        private void OnSayRequested(SayRequest request)
        {
            _say = request;
            _choice = null;
        }

        private void OnChoiceRequested(ChoiceRequest request)
        {
            _choice = request;
            _say = null;
        }

        private void OnFinished(bool success)
        {
            _say = null;
            _choice = null;
            _result = success ? "Dialogue finished: success." : "Dialogue finished: failure.";
        }
    }
}
