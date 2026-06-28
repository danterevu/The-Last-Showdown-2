using UnityEngine;
using System.Collections.Generic;

public class CommentarySystem : MonoBehaviour
{
    public static CommentarySystem Instance;

    [Header("Panel de diálogo (único)")]
    [SerializeField] private DialoguePanel panel;

    private bool isReady = false;

    [System.Serializable]
    public class CommentPool
    {
        public CommentTrigger trigger;

        [Header("Sprite para este trigger")]
        public Sprite presenterSprite;

        [TextArea(2, 5)]
        public string[] comments;

        [Tooltip("Segundos que se muestra este comentario")]
        public float displayDuration = 4f;

        [Tooltip("Tiempo mínimo (segundos) entre disparos del mismo trigger")]
        public float cooldown = 8f;

        // Estado interno del shuffle bag
        [System.NonSerialized] public List<int> bag = new();
        [System.NonSerialized] public float lastFiredTime = -999f;
    }

    [Header("Comentarios por evento")]
    [SerializeField] private CommentPool[] pools;

    private Dictionary<CommentTrigger, CommentPool> lookup;

    private void Awake()
    {
        Instance = this;
        lookup = new Dictionary<CommentTrigger, CommentPool>();
        foreach (var pool in pools)
        {
            lookup[pool.trigger] = pool;
            RefillBag(pool);
        }
    }

    private void Start()
    {
        panel.HideImmediate();
    }

    public void SetReady()
    {
        isReady = true;
    }

    private void RefillBag(CommentPool pool)
    {
        pool.bag.Clear();
        for (int i = 0; i < pool.comments.Length; i++)
            pool.bag.Add(i);

        // Fisher-Yates shuffle
        for (int i = pool.bag.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (pool.bag[i], pool.bag[j]) = (pool.bag[j], pool.bag[i]);
        }
    }

    private int NextFromBag(CommentPool pool)
    {
        if (pool.bag.Count == 0)
            RefillBag(pool);

        int idx = pool.bag[pool.bag.Count - 1];
        pool.bag.RemoveAt(pool.bag.Count - 1);
        return idx;
    }

    public void TriggerComment(CommentTrigger trigger)
    {
        if (!isReady) return;

        // No-overlap: si el panel está activo, ignorar
        if (panel.IsActive) return;

        if (!lookup.TryGetValue(trigger, out var pool)) return;
        if (pool.comments == null || pool.comments.Length == 0) return;

        // Cooldown por trigger
        if (Time.time < pool.lastFiredTime + pool.cooldown) return;

        int idx = NextFromBag(pool);
        string comment = pool.comments[idx];

        panel.Show(comment, pool.presenterSprite, pool.displayDuration);
        pool.lastFiredTime = Time.time;
    }
}