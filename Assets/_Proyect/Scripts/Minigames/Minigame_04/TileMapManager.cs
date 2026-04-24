using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TileMapManager : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private GameObject tilePrefab;

    [Header("Grid Settings")]
    [SerializeField] private int columns = 10;
    [SerializeField] private int rows = 6;
    [SerializeField] private float tileSize = 1f;       // espacio entre tiles
    [SerializeField] private Vector2 gridOrigin;         // esquina inferior izquierda de la grilla

    [Header("Sprites")]
    [SerializeField] private Sprite spriteNormal;
    [SerializeField] private Sprite spriteCracked;
    [SerializeField] private Sprite spriteBroken;

    [Header("Tile Settings")]
    [SerializeField] private float breakDelay = 0.5f;

    [Header("Rebuild")]
    [SerializeField] private float rebuildDelay = 1f;   // segundos antes de reconstruir
    [SerializeField] private bool rebuildAnimated = true;

    [Header("References")]
    [SerializeField] private Transform tileGridParent;  // el TileGrid vacio de la hierarchy

    // lista interna de todos los tiles generados
    private List<HolographicTile> tiles = new List<HolographicTile>();

    private void Awake()
    {
        GenerateGrid();
    }

    
    //  GENERACION
    

    private void GenerateGrid()
    {
        tiles.Clear();

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                Vector2 pos = gridOrigin + new Vector2(col * tileSize, row * tileSize);
                SpawnTile(pos);
            }
        }
    }

    private void SpawnTile(Vector2 position)
    {
        GameObject obj = Instantiate(tilePrefab, position, Quaternion.identity, tileGridParent);
        HolographicTile tile = obj.GetComponent<HolographicTile>();
        tile.Initialize(spriteNormal, spriteCracked, spriteBroken, breakDelay);
        tiles.Add(tile);
    }

  
    //  REBUILD
   

    // HolographicPlatforms llama esto cuando alguien cae
    public void RequestRebuild()
    {
        StartCoroutine(RebuildRoutine());
    }

    private IEnumerator RebuildRoutine()
    {
        yield return new WaitForSeconds(rebuildDelay);

        if (rebuildAnimated)
            yield return StartCoroutine(RebuildAnimated());
        else
            RebuildInstant();
    }

    // todos los tiles se resetean a la vez
    private void RebuildInstant()
    {
        foreach (HolographicTile tile in tiles)
            tile.ResetTile();
    }

    // los tiles se reconstruyen fila por fila de abajo hacia arriba
    private IEnumerator RebuildAnimated()
    {
        // reconstruir fila por fila
        for (int row = 0; row < rows; row++)
        {
            // resetear todos los tiles de esta fila
            for (int col = 0; col < columns; col++)
            {
                int index = row * columns + col;
                if (index < tiles.Count)
                    tiles[index].ResetTile();
            }
            yield return new WaitForSeconds(0.05f); // pequeño delay entre filas
        }
    }

   
    //  UTILIDADES
   

    // devuelve cuantos tiles estan rotos (util para debug o logica futura)
    public int GetBrokenTileCount()
    {
        int count = 0;
        foreach (HolographicTile tile in tiles)
            if (tile.GetState() == HolographicTile.TileState.Broken)
                count++;
        return count;
    }

    // devuelve cuantos tiles siguen siendo suelo (normal o agrietado)
    public int GetSolidTileCount()
    {
        int count = 0;
        foreach (HolographicTile tile in tiles)
            if (tile.GetState() != HolographicTile.TileState.Broken)
                count++;
        return count;
    }

    // centrar la grilla en pantalla automaticamente
    // podes llamar esto desde el Inspector con un boton de contexto
    // o simplemente calcular el gridOrigin manualmente
    public Vector2 GetGridCenter()
    {
        float totalWidth = (columns - 1) * tileSize;
        float totalHeight = (rows - 1) * tileSize;
        return new Vector2(
            gridOrigin.x + totalWidth / 2f,
            gridOrigin.y + totalHeight / 2f
        );
    }

    public float GetRebuildDuration()
    {
        if (!rebuildAnimated) return rebuildDelay;

        // rebuildDelay + tiempo de animacion fila por fila
        float animTime = rows * 0.05f;
        return rebuildDelay + animTime;
    }
}