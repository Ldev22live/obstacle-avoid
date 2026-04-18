using UnityEngine;
using UnityEngine.EventSystems;

public class ARObjectInteractable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Interaction Settings")]
    public bool allowDragging = true;
    public bool saveOnRelease = true;
    public float dragSmoothing = 10f;

    private ARWorldObject objectData;
    private ARWorldSpawner spawner;
    private bool isDragging = false;
    private Vector3 targetPosition;
    private Camera arCamera;

    public void Initialize(ARWorldObject data, ARWorldSpawner parentSpawner)
    {
        objectData = data;
        spawner = parentSpawner;
        arCamera = Camera.main;
    }

    void Update()
    {
        if (isDragging)
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * dragSmoothing);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!allowDragging) return;

        isDragging = true;
        Debug.Log($"[ARObjectInteractable] Started dragging {objectData.id}");
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || arCamera == null) return;

        Ray ray = arCamera.ScreenPointToRay(eventData.position);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, LayerMask.GetMask("ARPlane")))
        {
            targetPosition = hit.point;
        }
        else
        {
            Plane groundPlane = new Plane(Vector3.up, transform.position);
            if (groundPlane.Raycast(ray, out float distance))
            {
                targetPosition = ray.GetPoint(distance);
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        isDragging = false;
        Debug.Log($"[ARObjectInteractable] Ended dragging {objectData.id} at {transform.position}");

        if (saveOnRelease && spawner != null)
        {
            spawner.UpdateObjectPosition(objectData.id, transform.position);
        }
    }

    void OnDestroy()
    {
        isDragging = false;
    }
}
