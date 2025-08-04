using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class RadialKnobController : MonoBehaviour, IDragHandler, IEndDragHandler
{
    public RectTransform knobHandle;
    public Transform sphere;
    public float radius = 3f;
    public int snapSteps = 12;
    public AudioClip snapSound;

    private float currentAngle = 0f;
    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 dir = eventData.position - (Vector2)knobHandle.position;
        currentAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        currentAngle = (currentAngle + 360f) % 360f;

        knobHandle.localRotation = Quaternion.Euler(0, 0, -currentAngle);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        float snapAngle = 360f / snapSteps;
        currentAngle = Mathf.Round(currentAngle / snapAngle) * snapAngle;

        knobHandle.localRotation = Quaternion.Euler(0, 0, -currentAngle);
        PlaySnapSound();
        MoveSphere(currentAngle);
    }

    void MoveSphere(float angle)
    {
        float rad = angle * Mathf.Deg2Rad;
        Vector3 target = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * radius;
        StartCoroutine(SmoothMoveSphere(target));
    }

    IEnumerator SmoothMoveSphere(Vector3 target)
    {
        float duration = 0.3f;
        float elapsed = 0f;
        Vector3 start = sphere.position;

        while (elapsed < duration)
        {
            sphere.position = Vector3.Lerp(start, target, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        sphere.position = target;
    }

    void PlaySnapSound()
    {
        if (snapSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(snapSound);
        }
    }
}
