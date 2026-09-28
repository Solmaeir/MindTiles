using UnityEngine;
using UnityEngine.InputSystem; // Yeni Input System

namespace Legacy2048
{
public class InputController : MonoBehaviour
{
    private GameObject selectedObject = null;
    private Vector3 startPointerWorldPos; // Adını değiştirmek daha anlaşılır olur
    private bool dragging = false;
    public static InputController Instance { get; private set; }

    public float moveDistance = 1f;
    public float moveSpeed = 5f;

    private Vector3 targetPosition;
    private bool isMoving = false;
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }
    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.inputLocked) return;


        // Pointer.current null değilse devam et
        if (Pointer.current == null) return;

        // Fare yerine Pointer'ın basılma durumunu kontrol et
        if (Pointer.current.press.wasPressedThisFrame)
        {
            Vector3 pointerWorldPos = GetPointerWorldPos();
            Vector2 pointer2D = new Vector2(pointerWorldPos.x, pointerWorldPos.y);
            RaycastHit2D hit = Physics2D.Raycast(pointer2D, Vector2.zero);

            if (hit.collider != null && hit.collider.CompareTag("Block"))
            {
                selectedObject = hit.collider.gameObject;
                startPointerWorldPos = pointerWorldPos;
                dragging = true;
            }
        }

        // Fare yerine Pointer'ın bırakılma durumunu kontrol et
        if (Pointer.current.press.wasReleasedThisFrame && dragging && selectedObject != null)
        {
            Vector3 endPointerWorldPos = GetPointerWorldPos();
            Vector3 dragVector = endPointerWorldPos - startPointerWorldPos;

            Vector3 moveDir = Vector3.zero;

            if (Mathf.Abs(dragVector.x) > Mathf.Abs(dragVector.y))
                moveDir = (dragVector.x > 0) ? Vector3.right : Vector3.left;
            else
                moveDir = (dragVector.y > 0) ? Vector3.up : Vector3.down;

            targetPosition = selectedObject.transform.position + moveDir * moveDistance;
            isMoving = true;
            dragging = false;
        }

        if (isMoving && selectedObject != null)
        {
            selectedObject.transform.position = Vector3.MoveTowards(
                selectedObject.transform.position,
                targetPosition,
                moveSpeed * Time.deltaTime
            );

            if (Vector3.Distance(selectedObject.transform.position, targetPosition) < 0.01f)
            {
                selectedObject.transform.position = targetPosition;
                isMoving = false;

                var highlight = selectedObject.GetComponent<BlockHighlight>();
                if (highlight != null)
                {
                    highlight.CheckIfCorrect();
                }

                selectedObject = null;
            }
        }
    }

    Vector3 GetPointerWorldPos()
    {
        // Fare yerine Pointer'ın pozisyonunu oku
        Vector3 screenPos = Pointer.current.position.ReadValue();
        screenPos.z = Camera.main.nearClipPlane;
        return Camera.main.ScreenToWorldPoint(screenPos);
    }
}
}