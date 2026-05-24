using UnityEngine;
using System.Collections;

public class HandController : MonoBehaviour
{
    [Header("Sprites")]
    [SerializeField] private Sprite openSprite;
    [SerializeField] private Sprite closedSprite;

    [Header("Componentes")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;
    [SerializeField] private Collider2D handCollider;

    [Header("Efectos")]
    [SerializeField] private float stretchAmount = 1.5f;
    [SerializeField] private float stretchDuration = 0.3f;

    [Header("Estado")]
    [SerializeField] private bool isOpen = true;
    [SerializeField] private bool hasPlayer = false;
    [SerializeField] private GameObject grabbedPlayer;

    public bool IsOpen => isOpen;
    public bool HasPlayer => hasPlayer;
    public GameObject GrabbedPlayer => grabbedPlayer;

    private Vector3 originalScale;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        if (animator == null)
            animator = GetComponent<Animator>();
        if (handCollider == null)
            handCollider = GetComponent<Collider2D>();
        
        originalScale = transform.localScale;
        
        if (handCollider != null)
            handCollider.enabled = false;
    }

    private void OnEnable()
    {
        if (handCollider != null)
            handCollider.enabled = true;
    }

    private void OnDisable()
    {
        if (handCollider != null)
            handCollider.enabled = false;
    }

    public void OpenHand()
    {
        isOpen = true;
        if (spriteRenderer != null && openSprite != null)
            spriteRenderer.sprite = openSprite;
        if (animator != null)
            animator.SetTrigger("Open");
        if (handCollider != null)
            handCollider.enabled = true;
    }

    public void CloseHand()
    {
        isOpen = false;
        if (spriteRenderer != null && closedSprite != null)
            spriteRenderer.sprite = closedSprite;
        if (animator != null)
            animator.SetTrigger("Close");
        if (handCollider != null)
            handCollider.enabled = false;
    }

    public void GrabPlayer(GameObject player)
    {
        if (player == null) return;

        CloseHand();
        hasPlayer = true;
        grabbedPlayer = player;
        player.transform.SetParent(transform);
        player.SetActive(false);
    }

    public void ReleasePlayer(Vector3 newPosition)
    {
        if (grabbedPlayer == null) return;

        OpenHand();
        grabbedPlayer.transform.SetParent(null);
        grabbedPlayer.transform.position = newPosition;
        grabbedPlayer.SetActive(true);
        hasPlayer = false;
        grabbedPlayer = null;
    }

    public IEnumerator StretchTowards(Vector3 target)
    {
        Vector3 direction = (target - transform.position).normalized;
        Vector3 stretchedScale = originalScale;
        
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            stretchedScale.x *= stretchAmount;
        }
        else
        {
            stretchedScale.y *= stretchAmount;
        }

        for (float t = 0; t < stretchDuration; t += Time.deltaTime)
        {
            transform.localScale = Vector3.Lerp(originalScale, stretchedScale, t / stretchDuration);
            yield return null;
        }
    }

    public IEnumerator ReturnToOriginalScale()
    {
        for (float t = 0; t < stretchDuration; t += Time.deltaTime)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, originalScale, t / stretchDuration);
            yield return null;
        }
        transform.localScale = originalScale;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player1") || other.CompareTag("Player2"))
        {
            if (isOpen && !hasPlayer)
            {
                GrabPlayer(other.gameObject);
            }
        }
    }
}