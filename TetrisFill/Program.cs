// See https://aka.ms/new-console-template for more information
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TetrisFill;

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
        new Chunk
        {
            Symbol = 'H',
            Biome = Biome.Green,
            Shape = new[,]
            {
                { true, true, true },
                { true, false, true },
                { true, true, true },
            }
        },
        new Chunk
        {
            Symbol = 'I',
            Biome = Biome.Red,
            Shape = new[,]
            {
                { true, false },
                { true, true },
                { true, true },
            }
        },
    ]
);

var map = new Map();
var rand = new Random();

var biomeNoise = new FastNoiseLite(rand.Next());
biomeNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
biomeNoise.SetFrequency(0.08f);

Console.Clear();

var spiral = new SpiralWalker(new(0, 0), 14);
foreach (var placeTarget in spiral)
{
    // For this example we have only 3 biomes on a 1-dimensional axis,
    // so we can pick a biome the 'stupid' way with a few value comparisons.
    // This method is viable even with a more complex biome map, but if we
    // end up having more than 2 noise layers we should probably find another method.

    // Basically, zones where the noise is lower than -0.4 become blue,
    // and zones where it's above 0.4 become red. In the middle we are just green
    var biome = Biome.Green;
    var noiseSample = biomeNoise.GetNoise(placeTarget.X, placeTarget.Z);
    if (noiseSample > 0.4)
    {
        biome = Biome.Red;
    }
    else if (noiseSample < -0.4)
    {
        biome = Biome.Blue;
    }

    var fits = map.GetFittingPlacementsForChunks(
        chunkRegistry.Where(chunk => chunk.Biome == biome),
        placeTarget
    );
    if (fits.Count > 0)
    {
        var selected = fits[rand.Next() % fits.Count];
        map.Place(selected);
    }

    DrawSymbol(placeTarget, 'T', ConsoleColor.Magenta);
    Thread.Sleep(10);
    DrawMap();
    Thread.Sleep(10);
}

void DrawMap()
{
    foreach (var placement in map.PlacedChunks)
    {
        var color = placement.Chunk.Biome switch
        {
            Biome.Red => ConsoleColor.Red,
            Biome.Blue => ConsoleColor.Blue,
            Biome.Green => ConsoleColor.Green,
            _ => ConsoleColor.White
        };

        foreach (var coord in placement.EachTileInWorldSpace())
        {
            DrawSymbol(coord, placement.Chunk.Symbol, color);
        }
    }
}

void DrawSymbol(Coord2D pos, char symbol, ConsoleColor foreground)
{
    var targetX = pos.X + Console.WindowWidth / 2;
    var targetY = pos.Z + Console.WindowHeight / 2;
    if (
        targetX >= 0
        && targetX < Console.WindowWidth
        && targetY >= 0
        && targetY < Console.WindowHeight
    )
    {
        Console.SetCursorPosition(targetX, targetY);
        Console.ForegroundColor = foreground;
        Console.Write(symbol);
    }
}

Console.ReadLine();

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

        public int XSize => Shape.GetLength(1);
        public int ZSize => Shape.GetLength(0);

        public IEnumerable<Coord2D> EachTile()
        {
            return Enumerable
                .Range(0, XSize)
                .Select(x =>
                    Enumerable
                        .Range(0, ZSize)
                        .Where(z => Shape[z, x])
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

        public List<ChunkPlacement> GetFittingPlacementsForChunks(
            IEnumerable<Chunk> allChunks,
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
                    TopLeftCorner = new(center.X - offset.X, center.Z - offset.Z),
                })
                .Where(TestFits)
                .ToList();
        }

        public bool TestFits(ChunkPlacement option)
        {
            return option.EachTileInWorldSpace().All(coord => !IsOccupied(coord));
        }
    }

    public sealed class SpiralWalker(Coord2D center, int radius)
        : IEnumerable<Coord2D>,
            IEnumerator<Coord2D>
    {
        private readonly int _radius = radius;

        private int _x = center.X;
        private int _z = center.Z;

        private int _dx = 0;
        private int _dz = -1;

        private Coord2D _currentCoordinate;

        public Coord2D Current => _currentCoordinate;

        object IEnumerator.Current => _currentCoordinate;

        public void Dispose() { }

        public IEnumerator<Coord2D> GetEnumerator()
        {
            return this;
        }

        public bool MoveNext()
        {
            var done = _x < -_radius || _x > _radius || _z < -_radius || _x > _radius;

            _currentCoordinate = new Coord2D(_x, _z);

            if (_x == _z || (_x < 0 && _x == -_z) || (_x > 0 && _x == 1 - _z))
            {
                var t = _dx;
                _dx = -_dz;
                _dz = t;
            }

            _x += _dx;
            _z += _dz;

            return !done;
        }

        public void Reset()
        {
            _x = center.X;
            _z = center.Z;
            _dx = 0;
            _dz = -1;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}
