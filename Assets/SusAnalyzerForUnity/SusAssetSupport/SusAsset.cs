using UnityEngine;

namespace Tea.Safu
{
    public class SusAsset : ScriptableObject
    {
        [SerializeField] private string rawText;
        public string RawText { get => rawText; set => rawText = value; }
    }
}
