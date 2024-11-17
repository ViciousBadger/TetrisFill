// See https://aka.ms/new-console-template for more information
using System;
using System.Collections.Generic;
using System.Linq;
using TetrisFill;

Console.WriteLine("Hello, World!");

bool[,] fuck =
{
    { true, true },
    { true, true }
};
var chunkRegistry = new List<Chunk>(
    [
        new Chunk
        {
            Symbol = 'A',
            Biome = Biome.Red,
            Shape = new[,]
            {
                { true }
            }
        },
        new Chunk
        {
            Symbol = 'B',
            Biome = Biome.Green,
            Shape = new[,]
            {
                { true }
            }
        },
        new Chunk
        {
            Symbol = 'C',
            Biome = Biome.Blue,
            Shape = new[,]
            {
                { true }
            }
        },
        new Chunk
        {
            Symbol = 'D',
            Biome = Biome.Red,
            Shape = new[,]
            {
                { true, true }
            }
        },
        new Chunk
        {
            Symbol = 'E',
            Biome = Biome.Blue,
            Shape = new[,]
            {
                { true, true },
                { true, true }
            }
        },
        new Chunk
        {
            Symbol = 'F',
            Biome = Biome.Green,
            Shape = new[,]
            {
                { true, true },
                { true, false }
            }
        },
        new Chunk
        {
            Symbol = 'G',
            Biome = Biome.Blue,
            Shape = new[,]
            {
                { true, true, true },
            }
        },
    ]
);

var map = new Map();
Random rand = new Random();

foreach (var placeX in Enumerable.Range(0, 8))
{
    foreach (var placeZ in Enumerable.Range(0, 8))
    {
        var placeTarget = new Coord2D(placeX, placeZ);
        var fits = map.GetFittingPlacementsForAllChunks(chunkRegistry, placeTarget);
        if (fits.Count > 0)
        {
            var selected = fits[rand.Next() % fits.Count];
            //var selected = fits[0];
            Console.WriteLine(
                string.Format(
                    "Trying to place chunk {0} at {1} with offset {2}",
                    selected.Chunk.Symbol,
                    placeTarget,
                    selected.TopLeftCorner
                )
            );
            map.Place(selected);
        }
    }
}

Console.Clear();
foreach (var placement in map.PlacedChunks)
{
    foreach (var coord in placement.EachTileInWorldSpace())
    {
        var targetX = coord.X + Console.WindowWidth / 2;
        var targetY = coord.Z + Console.WindowHeight / 2;
        if (
            targetX >= 0
            && targetX < Console.WindowWidth
            && targetY >= 0
            && targetY < Console.WindowHeight
        )
        {
            Console.SetCursorPosition(targetX, targetY);
            Console.ForegroundColor = placement.Chunk.Biome switch
            {
                Biome.Red => ConsoleColor.Red,
                Biome.Blue => ConsoleColor.Blue,
                Biome.Green => ConsoleColor.Green,
                _ => ConsoleColor.White
            };
            Console.Write(placement.Chunk.Symbol);
        }
    }
}

namespace TetrisFill
{
    public readonly record struct Coord2D(int X, int Z);

    public enum Biome
    {
        Red,
        Blue,
        Green
    }

    public record Chunk
    {
        public required char Symbol { get; init; }
        public required Biome Biome { get; init; }
        public required bool[,] Shape { get; init; }

        public int XSize => Shape.GetLength(0);
        public int ZSize => Shape.GetLength(1);

        public IEnumerable<Coord2D> EachTile()
        {
            return Enumerable
                .Range(0, XSize)
                .Select(x =>
                    Enumerable
                        .Range(0, ZSize)
                        .Where(z => Shape[x, z])
                        .Select(z =>
                        {
                            return new Coord2D(x, z);
                        })
                )
                .SelectMany(list => list);
        }
    }

    public readonly record struct ChunkPlacement
    {
        public required Chunk Chunk { get; init; }
        public required Coord2D TopLeftCorner { get; init; }

        public IEnumerable<Coord2D> EachTileInWorldSpace()
        {
            var topLeftCornerCopy = TopLeftCorner;
            return Chunk
                .EachTile()
                .Select(coord => new Coord2D(
                    topLeftCornerCopy.X + coord.X,
                    topLeftCornerCopy.Z + coord.Z
                ));
        }
    }

    public class Map
    {
        public List<ChunkPlacement> PlacedChunks { get; } = [];
        public HashSet<Coord2D> OccupiedTiles { get; } = [];

        public bool IsOccupied(Coord2D coord)
        {
            return OccupiedTiles.Contains(coord);
        }

        public void SetOccupied(Coord2D coord)
        {
            OccupiedTiles.Add(coord);
        }

        public void Place(ChunkPlacement placedChunk)
        {
            var tilesToOccupy = placedChunk.EachTileInWorldSpace().ToList();
            foreach (var coord in tilesToOccupy)
            {
                if (IsOccupied(coord))
                {
                    throw new InvalidOperationException(
                        string.Format("FAIL: The coordinate {0} is already occupied!!", coord)
                    );
                }
            }
            foreach (var coord in tilesToOccupy)
            {
                SetOccupied(coord);
            }
            PlacedChunks.Add(placedChunk);
            // foreach (var x in Enumerable.Range(0, placedChunk.Chunk.XSize) {

            // }
            // PlacedChunks.Add()
        }

        public List<ChunkPlacement> GetFittingPlacementsForAllChunks(
            List<Chunk> allChunks,
            Coord2D coord
        )
        {
            return allChunks
                .Select(chunk => GetFittingPlacementsForChunk(chunk, coord))
                .SelectMany(list => list)
                .ToList();
        }

        public List<ChunkPlacement> GetFittingPlacementsForChunk(Chunk chunk, Coord2D center)
        {
            return chunk
                .EachTile()
                .Select(offset => new ChunkPlacement
                {
                    Chunk = chunk,
                    TopLeftCorner = new(center.X + offset.X, center.Z + offset.Z),
                })
                .Where(TestFits)
                .ToList();
        }

        public bool TestFits(ChunkPlacement option)
        {
            return option.EachTileInWorldSpace().All(coord => !IsOccupied(coord));
        }
    }
}
