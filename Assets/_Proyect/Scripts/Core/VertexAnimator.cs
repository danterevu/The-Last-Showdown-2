using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class VertexAnimator : BaseMeshEffect
{
    [Header("Animation Settings")]
    [SerializeField] private float stretchAmount = 20f;
    [SerializeField] private float animationDuration = 2f;

    private float time;
    private Image image;

    protected override void Awake()
    {
        base.Awake();
        image = GetComponent<Image>();
    }

    void Update()
    {
        if (IsActive())
        {
            time += Time.deltaTime;
            graphic.SetVerticesDirty();
        }
    }

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive())
            return;

        float normalizedTime = (time % animationDuration) / animationDuration;
        float stretch = Mathf.Sin(normalizedTime * Mathf.PI * 2f) * stretchAmount;

        UIVertex vertex = new UIVertex();

        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);

            if (i == 0 || i == 1)
            {
                vertex.position.y += stretch;
            }

            vh.SetUIVertex(vertex, i);
        }
    }
}
