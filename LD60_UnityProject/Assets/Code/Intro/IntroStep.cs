using System;
using NodeGraphAvr.Dialogue;
using UnityEngine;

namespace LD60.Intro
{
    [Serializable]
    public class IntroStep
    {
        [field: SerializeField] public string Name { get; private set; } = "";
        [field: SerializeField] public IntroStepKind Kind { get; private set; } = IntroStepKind.Wait;
        [field: SerializeField] public float Seconds { get; private set; } = 1f;
        [field: SerializeField] public Sprite Picture { get; private set; }
        [field: SerializeField] public DialogueGraph Dialogue { get; private set; }
    }
}
