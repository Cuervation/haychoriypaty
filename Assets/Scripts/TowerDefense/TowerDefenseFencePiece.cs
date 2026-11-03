using UnityEngine;

namespace HayChoriYPaty
{
    /// <summary>Metadata kept on each independent, visible fence piece.</summary>
    [DisallowMultipleComponent]
    public sealed class TowerDefenseFencePiece : MonoBehaviour
    {
        [SerializeField] private TowerDefenseFencePieceKind kind;
        [SerializeField] private int railIndex;
        [SerializeField] private int segmentIndex;
        public TowerDefenseFencePieceKind Kind => kind;
        public int RailIndex => railIndex;
        public int SegmentIndex => segmentIndex;

        public void Initialize(TowerDefenseFencePieceKind pieceKind, int rail, int index)
        {
            kind = pieceKind;
            railIndex = rail;
            segmentIndex = index;
        }
    }
}
