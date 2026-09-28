using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using MindTiles.Grid;
using MindTiles.Core;
using MindTiles.Validation;

namespace MindTiles.InputHandling
{
    // Eski "Swipe-to-Move" (Kaydır ve Yer Değiştir) mantığına dönüştürülmüş DragController
    public class DragController : MonoBehaviour
    {
        public static DragController Instance { get; private set; }

        [SerializeField] private GridManager gridManager;

        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 15f;
        [SerializeField] private float swipeThreshold = 0.5f; // Sadece tıklamayı elemek için minimum kaydırma mesafesi

        private TileController selectedTile;
        private Vector3 startPointerWorldPos;
        private bool isSwiping = false;
        private bool isAnimating = false;

        public int MoveCount { get; private set; }

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        public void ResetMoveCount()
        {
            MoveCount = 0;
        }

        private void Update()
        {
            // Oyun kilitliyse, veya bloklar hala kayma animasyonundaysa girdiyi yoksay
            if (GameManager.Instance != null && GameManager.Instance.inputLocked) return;
            if (isAnimating) return;
            if (Pointer.current == null) return;

            // Ekrana/Fereye basıldığı an
            if (Pointer.current.press.wasPressedThisFrame)
            {
                TryBeginSwipe();
            }

            // Ekrandan/Fareden parmak çekildiği an
            if (Pointer.current.press.wasReleasedThisFrame && isSwiping && selectedTile != null)
            {
                EndSwipe();
            }
        }

        private void TryBeginSwipe()
        {
            Vector3 pointerWorldPos = GetPointerWorldPos();
            RaycastHit2D hit = Physics2D.Raycast(pointerWorldPos, Vector2.zero);

            if (hit.collider != null && hit.collider.CompareTag("Tile"))
            {
                selectedTile = hit.collider.GetComponent<TileController>();
                if (selectedTile != null)
                {
                    startPointerWorldPos = pointerWorldPos;
                    isSwiping = true;
                }
            }
        }

        private void EndSwipe()
        {
            isSwiping = false;
            Vector3 endPointerWorldPos = GetPointerWorldPos();
            Vector3 dragVector = endPointerWorldPos - startPointerWorldPos;

            // Eğer oyuncu yeterince kaydırmadıysa (sadece tıkladıysa) işlemi iptal et
            if (dragVector.magnitude < swipeThreshold)
            {
                selectedTile = null;
                return;
            }

            Vector2Int moveDir = Vector2Int.zero;

            // Eski scriptindeki mantık: Hangi eksende daha çok kaydırıldıysa o yönü baz al
            if (Mathf.Abs(dragVector.x) > Mathf.Abs(dragVector.y))
                moveDir = (dragVector.x > 0) ? Vector2Int.right : Vector2Int.left;
            else
                moveDir = (dragVector.y > 0) ? Vector2Int.up : Vector2Int.down;

            Vector2Int originGridPos = selectedTile.CurrentGridPos;
            Vector2Int targetGridPos = originGridPos + moveDir;

            // Hedef pozisyon Grid (ızgara) sınırları içerisinde mi? (Dışarı sürüklenmeyi engeller)
            if (targetGridPos.x >= 0 && targetGridPos.x < gridManager.GridWidth &&
                targetGridPos.y >= 0 && targetGridPos.y < gridManager.GridHeight)
            {
                // Geçerli hamle, Swap ve Animasyon işlemini başlat
                StartCoroutine(SwapAndAnimate(selectedTile, originGridPos, targetGridPos));
            }
            else
            {
                // Sınır dışına itilmeye çalışıldıysa iptal et
                selectedTile = null;
            }
        }

        private IEnumerator SwapAndAnimate(TileController tile1, Vector2Int originPos, Vector2Int targetPos)
        {
            isAnimating = true;

            // 1. GridManager üzerinden mantıksal swap işlemini yap ve verileri güncelle
            TileController tile2 = gridManager.MoveTile(tile1, targetPos);
            MoveCount++;

            // Not: GridManager.MoveTile pozisyonları "anında" değiştirir. 
            // Eski scriptindeki animasyonu verebilmek için pozisyonları alıp bir süreliğine Lerp ile kaydıracağız.
            Vector3 tile1Start = gridManager.GetWorldPositionFromGridCoordinate(originPos);
            Vector3 tile1End = gridManager.GetWorldPositionFromGridCoordinate(targetPos);

            // Animasyon öncesi başlangıç konumuna geri çek
            tile1.transform.position = tile1Start;

            Vector3 tile2Start = tile1End;
            Vector3 tile2End = tile1Start;

            if (tile2 != null)
            {
                tile2.transform.position = tile2Start;
            }

            // 2. Blokların yeni yerlerine doğru pürüzsüzce kayması
            float t = 0;
            while (t < 1f)
            {
                t += Time.deltaTime * moveSpeed;
                float step = Mathf.Clamp01(t);

                tile1.transform.position = Vector3.Lerp(tile1Start, tile1End, step);
                if (tile2 != null)
                {
                    tile2.transform.position = Vector3.Lerp(tile2Start, tile2End, step);
                }

                yield return null;
            }

            // Tam oturduğundan emin olmak için kesin eşitleme
            tile1.transform.position = tile1End;
            if (tile2 != null) tile2.transform.position = tile2End;

            // 3. PatternValidator doğruluk kontrolü (Eski kodundaki Highlight mantığı)
            var validator = PatternValidator.Instance;
            if (validator != null)
            {
                tile1.GetComponent<TileHighlight>()?.SetCorrect(validator.IsPositionCorrect(targetPos));
                if (tile2 != null)
                {
                    tile2.GetComponent<TileHighlight>()?.SetCorrect(validator.IsPositionCorrect(originPos));
                }
            }

            selectedTile = null;
            isAnimating = false;
        }

        private Vector3 GetPointerWorldPos()
        {
            Vector3 screenPos = Pointer.current.position.ReadValue();
            screenPos.z = -Camera.main.transform.position.z;
            return Camera.main.ScreenToWorldPoint(screenPos);
        }
    }
}