using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Model {
    public class Level {
        static float CURVE_WEIGHT = .35f;

        public LevelTile[,] tiles;
        public List<Train> trains;

        public Level(TextAsset text) {
            string[] lines = Regex.Split(text.text, "\r\n|\n|\r");
            tiles = new LevelTile[lines[0].Length, lines.Length];
            Dictionary<Tuple<int, int>, int> trainsToPlace = new Dictionary<Tuple<int, int>, int>();
            for (int y = 0; y < tiles.GetLength(1); y++) {
                for (int x = 0; x < tiles.GetLength(0); x++) {
                    char c = lines[y][x];
                    if (c == '.') {
                        tiles[x, y] = LevelTile.Ground;
                    } else if (c == '=') {
                        tiles[x, y] = LevelTile.Track;
                    } else if (c >= '1' && c <= '9') {
                        tiles[x, y] = LevelTile.Track;
                        int numCars = c - '0';
                        trainsToPlace[new Tuple<int, int>(x, y)] = numCars;
                    } else {
                        throw new Exception(string.Format("Unexpected character '{0}' in level {1}.", lines[y][x], text.name));
                    }
                }
            }
            // Place each train starting from its head.
            trains = new List<Train>();
            foreach (var trainToPlace in trainsToPlace) {
                Tuple<int, int>[] neighbors = NeighborCoors(trainToPlace.Key);
                trains.Add(new Train(trainToPlace.Key, neighbors[0]));
                for (int i = 1; i < trainToPlace.Value; i++) {
                    Train lastTrain = trains[trains.Count - 1];
                    neighbors = NeighborCoors(lastTrain.nextCoor);
                    foreach (Tuple<int, int> neighbor in neighbors) {
                        if (!neighbor.Equals(lastTrain.coor)) {
                            trains.Add(new Train(lastTrain.nextCoor, neighbor));
                            break;
                        }
                    }
                }
            }
        }

        public Tuple<int, int>[] NeighborCoors(Tuple<int, int> coor) {
            List<Tuple<int, int>> coors = new List<Tuple<int, int>>();
            int x = coor.Item1;
            int y = coor.Item2;
            if (x < tiles.GetLength(0) - 1 && tiles[x + 1, y] == LevelTile.Track) {
                coors.Add(new Tuple<int, int>(x + 1, y));
            }
            if (y < tiles.GetLength(1) - 1 && tiles[x, y + 1] == LevelTile.Track) {
                coors.Add(new Tuple<int, int>(x, y + 1));
            }
            if (x > 0 && tiles[x - 1, y] == LevelTile.Track) {
                coors.Add(new Tuple<int, int>(x - 1, y));
            }
            if (y > 0 && tiles[x, y - 1] == LevelTile.Track) {
                coors.Add(new Tuple<int, int>(x, y - 1));
            }
            Debug.Assert(coors.Count == 2, "Unexpected number of track neighbors.");
            return coors.ToArray();
        }

        public Tuple<Vector3, float> GetTransform(Tuple<int, int> coor) {
            Vector3 position;
            float rotation;
            Tuple<int, int>[] neighbors = NeighborCoors(coor);
            Tuple<int, int> delta1 = new Tuple<int, int>(coor.Item1 - neighbors[0].Item1, coor.Item2 - neighbors[0].Item2);
            Tuple<int, int> delta2 = new Tuple<int, int>(neighbors[1].Item1 - coor.Item1, neighbors[1].Item2 - coor.Item2);
            // Straight track.
            if (delta1.Equals(delta2)) {
                position = new Vector3(coor.Item1, 0, -coor.Item2);
                rotation = delta1.Item2 == 0 ? 0 : 90;
            } else {
                float x = (coor.Item1 + neighbors[0].Item1 * CURVE_WEIGHT + neighbors[1].Item1 * CURVE_WEIGHT) / (CURVE_WEIGHT * 2 + 1);
                float y = (coor.Item2 + neighbors[0].Item2 * CURVE_WEIGHT + neighbors[1].Item2 * CURVE_WEIGHT) / (CURVE_WEIGHT * 2 + 1);
                position = position = new Vector3(x, 0, -y);
                int ddx = delta2.Item1 - delta1.Item1;
                int ddy = delta2.Item2 - delta1.Item2;
                rotation = ddx == ddy ? -45 : 45;
            }
            return new Tuple<Vector3, float>(position, rotation);
        }
    }

    public enum LevelTile {
        Ground, Track
    }
}
