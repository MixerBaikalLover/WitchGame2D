using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class TileCache
{
    private List<WorldTile> _tiles = new();
    private int _maxSize;

    public int Size => _tiles.Count;

    public void SetMaxSize(int maxSize) { _maxSize = maxSize; }

    public TileCache(int maxSize)
    {
        _maxSize = maxSize;
    }

    public void Add(WorldTile tile)
    {
        _tiles.Add(tile);
        if (Size > _maxSize) DropPeek();
        tile.SetHidden(true);
        tile.loaded = false;
        tile.cached = true;
    }

    public void Remove(WorldTile tile)
    {
        if (_tiles.Contains(tile))
        {
            _tiles.Remove(tile);
            tile.cached = false;
        }
    }

    private void DropPeek()
    {
        WorldTile peek = _tiles.First();
        peek.loaded = false;
        peek.cached = false;
        peek.DestroyInteractable();
        _tiles.Remove(peek);
    }
}
