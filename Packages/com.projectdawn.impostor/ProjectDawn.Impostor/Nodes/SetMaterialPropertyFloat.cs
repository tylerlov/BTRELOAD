
using GraphProcessor;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectDawn.Impostor
{
    [NodeMenuItem("Material/Set Material Property Float")]
    public class SetMaterialPropertyFloat : ImpostorNode
    {
        public override string name => "Set Material Property Float";

        [SerializeField, Input]
        public string Name;
        [Input]
        public float Value;
        [Input]
        public Impostor In;
        [Output]
        public Impostor Out;

        protected override void Process()
        {
            In.Material.SetFloat(Name, Value);
            Out = In;
        }
    }
}