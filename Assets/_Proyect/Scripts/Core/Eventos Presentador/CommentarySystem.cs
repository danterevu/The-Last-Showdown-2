using UnityEngine;
using System.Collections.Generic;

public class CommentarySystem : MonoBehaviour
{
    public static CommentarySystem Instance;

    [Header("Paneles (Uno por cada cara del presentador)")]
    [SerializeField] private CommentatorPanel[] panels;

    [System.Serializable]
    public class CommentPool
    {
        public CommentTrigger trigger;

        [TextArea(2, 5)]
        public string[] comments;

        [Tooltip("Segundos que se muestra este tipo de comentario")]
        public float displayDuration = 4f;
    }

    [Header("Comentarios por evento")]
    [SerializeField] private CommentPool[] pools;

    private Dictionary<CommentTrigger, CommentPool> lookup;

    private void Awake()
    {
        Instance = this; // se actualiza por escena, no persiste

        lookup = new Dictionary<CommentTrigger, CommentPool>();
        foreach (var pool in pools)
            lookup[pool.trigger] = pool;
    }

    private void Start()
    {

        foreach (var panel in panels)
            panel.HideImmediate();
    }

    public void TriggerComment(CommentTrigger trigger)
    {
        // regla de no-overlap: si CUALQUIER panel está activo, no hacer nada
        foreach (var panel in panels)
            if (panel.IsActive) return;

        if (!lookup.TryGetValue(trigger, out var pool)) return;
        if (pool.comments == null || pool.comments.Length == 0) return;

        // elegir comentario random del pool
        string comment = pool.comments[Random.Range(0, pool.comments.Length)];

        // elegir panel random entre los disponibles
        CommentatorPanel chosen = panels[Random.Range(0, panels.Length)];
        chosen.Show(comment, pool.displayDuration);
    }
}