using UnityEngine;

namespace MindTiles.Grid
{
    // Attached to every tile prefab. Tracks the tile's grid identity so the
    // rest of the systems (drag, validation, highlight) never have to infer
    // position from world-space transforms.
    //
    // TileTypeId identifies which prefab this tile was spawned from (its
    // visual "kind"). Validation matches by type, not by instance identity,
    // since the player can't tell two identical-looking tiles apart.
    public class TileController : MonoBehaviour
    {
        public Vector2Int OriginalGridPos { get; private set; }
        public Vector2Int CurrentGridPos { get; private set; }
        public int TileTypeId { get; private set; }

        public void Initialize(Vector2Int originalGridPos, int tileTypeId)
        {
            OriginalGridPos = originalGridPos;
            CurrentGridPos = originalGridPos;
            TileTypeId = tileTypeId;
        }

        public void SetCurrentGridPos(Vector2Int gridPos)
        {
            CurrentGridPos = gridPos;
        }
    }
}
