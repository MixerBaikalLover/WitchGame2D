using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class WorldManager : MonoBehaviour
{
    #region Vars

    // Public fields
    public Generator generator;
    public Transform playerTransform;
    public int targetFrameRate = 60;
    [Range(1,50)]
    public int viewRangeX;
    [Range(1,50)]
    public int viewRangeY;
    [Range(1, 1000)]
    public int tileCacheSize;
    [Header("Грид")]
    public Grid worldGrid;
    [Header("К чему крепить все объекты")]
    public Transform gameObjectsTransform;
    [Header("Слои грида")]
    public Tilemap GroundTilemap;
    public Tilemap SandTilemap;
    public Tilemap WaterTilemap;
    public Tilemap PlainsTilemap;
    public Tilemap TreeTilemap;
    [Header("Тайлы")]
    public TileBase fertileGrassTile;
    public TileBase forestGrassTile;
    public TileBase sandTile;
    public TileBase waterTile;
    public TileBase swampTile;
    public TileBase plainsGrassTile;
    
    // Private fields
    public WorldData worldData;
    private Tilemap[] _tilemapByEnumIndex;
    private TileBase[] _tilebaseByEnumIndex;
    
    // Tile cache
    public List<WorldTile> tileCache { private set; get; }
    public List<Vector3Int> loadedTiles { private set; get; }

    #endregion


    
    #region UnityMethods

    private void Awake()
    {
        int mapCenterX = generator.mapWidth / 2;
        int mapCenterY = generator.mapHeight / 2;
        worldGrid.transform.position = new Vector3(0f, 0f, 0);
        playerTransform.position = new Vector3(mapCenterX, mapCenterY, 0f);
        Application.targetFrameRate = targetFrameRate;
    }

    private void Start()
    {
        if (generator.GenerateOnStart)
        {
            Generate();
        }
    }

    private void Update()
    {
        Vector3Int playerPosition = Vector3Int.FloorToInt(playerTransform.position);
        for (int x = - viewRangeX; x <= viewRangeX; x++)
        {
            for (int y = - viewRangeY; y <= viewRangeY; y++)
            {
                int targetX = x + playerPosition.x;
                int targetY = y + playerPosition.y;
                // Проверка на выход за пределы карты
                if (targetX < 0 || targetX >= generator.mapWidth || targetY < 0 || targetY >= generator.mapHeight) continue;
                WorldTile tile = worldData.WorldTiles[targetX, targetY];
                // Если тайл еще не загружен
                if (tile.loaded && !tile.cached) continue;
                Vector3Int pos = new Vector3Int(targetX, targetY);
                DrawTile(worldData.WorldTiles, pos);
            }
        }


        List<Vector3Int> toRemove = new();
        loadedTiles.ForEach(tile =>
        {
            Vector3Int target = playerPosition - tile;
            if (Math.Abs(target.x) > viewRangeX + 1 || Math.Abs(target.y) > viewRangeY + 1)
            {
                toRemove.Add(tile);
            }
        });
        
        toRemove.ForEach(tile =>
        {
            EraseTile(worldData.WorldTiles[tile.x, tile.y]);
            loadedTiles.Remove(tile);
        });
        toRemove.Clear();
    }

    #endregion



    #region ClassMethods

    public void Generate()
    {
        InteractableObjects.InitCollection();
        tileCache = new List<WorldTile>();
        loadedTiles = new List<Vector3Int>();
        ClearAllTiles();
        InitTileIndexArrays();
        worldData = generator.GenerateWorld();
    }

    public void DrawAllTiles()
    {
        for (int x = 0; x < worldData.MapWidth; x++)
        {
            for (int y = 0; y < worldData.MapHeight; y++)
            {
                Vector3Int position = new Vector3Int(x, y, 0);
                loadedTiles.Add(position);
                DrawTile(worldData.WorldTiles, position);
            }
        }
    }

    private void DrawTile(WorldTile[,] tiles, Vector3Int position)
    {
        WorldTile tile = tiles[position.x, position.y];
        loadedTiles.Add(position);
        // Отрисовка тайлов
        // Проходит по всем слоям тайла кроме того на котором объекты
        for (int i = 0; i < tile.layers.Length; i++)
        {
            // Если на нем есть почва, ставит ее
            if (tile.layers[i] != SoilType.None)
                _tilemapByEnumIndex[i].SetTile(position, _tilebaseByEnumIndex[(int) tile.layers[i]]);
        }
        
        // Если кеширован, то включает его объект
        if (tile.cached)
        {
            tile.instantiatedObject.SetActive(true);
            tileCache.Remove(tile);
        }
        
        tile.InstantiateInteractable(gameObjectsTransform);
        
    }

    private void EraseTile(WorldTile tile)
    {
        // Проходит по всем слоям тайла кроме того на котором объекты
        for (int i = 0; i < tile.layers.Length; i++)
        {
            // Если на нем есть что-то, чистит тайл
            if (tile.layers[i] != SoilType.None)
                _tilemapByEnumIndex[i].SetTile(tile.position, null);
        }
        
        CacheTile(tile);
    }

    private void CacheTile(WorldTile tile)
    {
        tile.loaded = false;
        // Если на тайле нет объекта, то кешировать нечего
        if (tile.instantiatedObject is null) return;
        // Если размер кеша превышает заданный, удаляет из памяти самый первый
        if (tileCache.Count >= tileCacheSize) DropCachePeek();
        tile.instantiatedObject.SetActive(false);
        tile.cached = true;
        tileCache.Add(tile);
    }

    private void DropCachePeek()
    {
        WorldTile peek = tileCache.First();
        peek.loaded = false;
        loadedTiles.Remove(peek.position);
        peek.cached = false;
        tileCache.Remove(peek);
        TreeTilemap.SetTile(peek.position, null);
    }

    public void ClearAllTiles()
    {
        GroundTilemap.ClearAllTiles();
        WaterTilemap.ClearAllTiles();
        SandTilemap.ClearAllTiles();
        PlainsTilemap.ClearAllTiles();
        foreach (Transform GO in gameObjectsTransform)
        {
            DestroyImmediate(GO.gameObject);
        }
    }

    #endregion



    #region Utils

    private void InitTileIndexArrays()
    {
        _tilemapByEnumIndex = CreateTilemapByEnumIndexArray();
        _tilebaseByEnumIndex = CreateTilebaseByEnumIndexArray();
    }
    
    public Tilemap[] CreateTilemapByEnumIndexArray()
    {
        Tilemap[] tileMaps = new Tilemap[6];
        tileMaps[(int)GridLayer.Ground] = GroundTilemap;
        tileMaps[(int)GridLayer.Plains] = PlainsTilemap;
        tileMaps[(int)GridLayer.Sand] = SandTilemap;
        tileMaps[(int)GridLayer.Water] = WaterTilemap;
        return tileMaps;
    }
    
    public TileBase[] CreateTilebaseByEnumIndexArray()
    {
        TileBase[] tileBases = new TileBase[6];
        tileBases[(int)SoilType.Water] = waterTile;
        tileBases[(int)SoilType.Swamp] = swampTile;
        tileBases[(int)SoilType.Sand] = sandTile;
        tileBases[(int)SoilType.FertileGrass] = fertileGrassTile;
        tileBases[(int)SoilType.ForestGrass] = forestGrassTile;
        tileBases[(int)SoilType.PlainsGrass] = plainsGrassTile;
        return tileBases;

    }

    #endregion
}
