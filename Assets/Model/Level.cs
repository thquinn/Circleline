using Assets.Code;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Model {
    public class Level {
        static float CURVE_WEIGHT = .33f;

        public LevelTile[,] tiles;
        public List<Train> trains;
        public Dictionary<Tuple<int, int>, Switch> switches;
        public Tuple<int, int> exit = new Tuple<int, int>(-1, -1);

        public Level(TextAsset text) {
            string[] chunks = Regex.Split(text.text, "\r\n\r\n|\n\n|\r\r");
            // Metadata.
            Tuple<int, int> playerCoor = new Tuple<int, int>(-1, -1);
            HashSet<Tuple<int, int>> reversedTrains = new HashSet<Tuple<int, int>>();
            Dictionary<Tuple<int, int>, SwitchInteraction> switchInteractionOverrides = new Dictionary<Tuple<int, int>, SwitchInteraction>();
            string[] lines = chunks.Length == 1 ? new string[] { } : Regex.Split(chunks[1], "\r\n|\n|\r");
            foreach (string line in lines) {
                string[] args = line.Split('\t');
                string[] tokens = args[1].Split(' ');
                if (args[0] == "P") {
                    playerCoor = new Tuple<int, int>(int.Parse(tokens[0]), int.Parse(tokens[1]));
                } else if (args[0] == "R") {
                    reversedTrains.Add(new Tuple<int, int>(int.Parse(tokens[0]), int.Parse(tokens[1])));
                } else if (args[0] == "SN") {
                    switchInteractionOverrides[new Tuple<int, int>(int.Parse(tokens[0]), int.Parse(tokens[1]))] = SwitchInteraction.None;
                }
            }
            Debug.Assert(playerCoor.Item1 != -1, string.Format("No player coordinate found in level '{0}'.", text.name));
            // Level.
            lines = Regex.Split(chunks[0], "\r\n|\n|\r");
            tiles = new LevelTile[lines[0].Length, lines.Length];
            Dictionary<Tuple<int, int>, int> trainsToPlace = new Dictionary<Tuple<int, int>, int>();
            // First pass to place track and train placeholders.
            for (int y = 0; y < tiles.GetLength(1); y++) {
                Debug.Assert(lines[y].Length == tiles.GetLength(0), string.Format("Level '{0}' has uneven line lengths.", text.name));
                for (int x = 0; x < tiles.GetLength(0); x++) {
                    char c = lines[y][x];
                    if (c == '.') {
                        tiles[x, y] = LevelTile.Ground;
                    } else if (c == '=') {
                        tiles[x, y] = LevelTile.Track;
                    } else if (c == 'f' || c == 'F' || c == 'l' || c == 'L' || c == 'r' || c == 'R') {
                        tiles[x, y] = LevelTile.Track;
                    } else if (c >= '1' && c <= '9') {
                        tiles[x, y] = LevelTile.Track;
                        int numCars = c - '0';
                        trainsToPlace[new Tuple<int, int>(x, y)] = numCars;
                    } else if (c == '$') {
                        tiles[x, y] = LevelTile.Track;
                        exit = new Tuple<int, int>(x, y);
                    } else {
                        throw new Exception(string.Format("Unexpected character '{0}' in level {1}.", lines[y][x], text.name));
                    }
                }
            }
            Debug.Assert(exit.Item1 != -1, string.Format("No exit found in level '{0}'.", text.name));
            // Second pass to place switches.
            switches = new Dictionary<Tuple<int, int>, Switch>();
            for (int y = 0; y < tiles.GetLength(1); y++) {
                for (int x = 0; x < tiles.GetLength(0); x++) {
                    char c = lines[y][x];
                    Tuple<int, int> coor = new Tuple<int, int>(x, y);
                    SwitchInteraction switchInteraction = switchInteractionOverrides.ContainsKey(coor) ? switchInteractionOverrides[coor] : SwitchInteraction.Click;
                    if (c == 'f' || c == 'F') {
                        switches[coor] = new Switch(this, SwitchType.Fork, coor, c == 'f' ? 0 : 1, switchInteraction);
                    } else if (c == 'l' || c == 'L') {
                        switches[coor] = new Switch(this, SwitchType.Left, coor, c == 'l' ? 0 : 1, switchInteraction);
                    } else if (c == 'r' || c == 'R') {
                        switches[coor] = new Switch(this, SwitchType.Right, coor, c == 'r' ? 0 : 1, switchInteraction);
                    }
                }
            }
            // Place each train starting from its head.
            trains = new List<Train>();
            foreach (var trainToPlace in trainsToPlace) {
                Tuple<int, int>[] neighbors = GetNeighbors(trainToPlace.Key);
                Tuple<int, int> firstCoor = reversedTrains.Contains(trainToPlace.Key) ? neighbors[0] : neighbors[1];
                Tuple<int, int> secondCoor = reversedTrains.Contains(trainToPlace.Key) ? neighbors[1] : neighbors[0];
                trains.Add(new Train(firstCoor, trainToPlace.Key, secondCoor, trainToPlace.Key.Equals(playerCoor) && trainToPlace.Value == 1));
                for (int i = 1; i < trainToPlace.Value; i++) {
                    Train lastTrain = trains[trains.Count - 1];
                    trains.Add(new Train(lastTrain.coor, lastTrain.nextCoor, GetNextCoor(lastTrain.nextCoor, lastTrain.coor), trainToPlace.Key.Equals(playerCoor) && i == trainToPlace.Value - 1 && trainToPlace.Value > 1));
                }
            }
        }

        public Tuple<int, int> GetNextCoor(Tuple<int, int> coor, Tuple<int, int> last) {
            Tuple<int, int>[] neighbors = GetNeighbors(coor);
            if (neighbors[0].Equals(last)) {
                return neighbors[1];
            }
            if (neighbors[1].Equals(last)) {
                return neighbors[0];
            }
            // We're approaching a switch from the off end.
            neighbors = switches[coor].GetOtherState();
            if (neighbors[0].Equals(last)) {
                return neighbors[1];
            }
            Debug.Assert(neighbors[1].Equals(last), string.Format("Something bad happened approaching the switch at {0},{1} from the off end.", coor.Item1, coor.Item2));
            return neighbors[0];
        }
        Tuple<int, int>[] GetNeighbors(Tuple<int, int> coor) {
            if (switches.ContainsKey(coor)) {
                return switches[coor].GetNeighbors();
            }
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
            if (coor.Equals(exit)) {
                int dx = coor.Item1 - coors[0].Item1;
                int dy = coor.Item2 - coors[0].Item2;
                coors.Add(new Tuple<int, int>(x + dx, y + dy));
            }
            Debug.Assert(coors.Count == 2, "Unexpected number of track neighbors.");
            return coors.ToArray();
        }

        public Tuple<Vector3, Quaternion> GetTransform(Tuple<int, int> coor, Tuple<int, int> last) {
            Vector3 position;
            float rotation;
            Tuple<int, int> next = GetNextCoor(coor, last);
            Tuple<int, int> delta1 = new Tuple<int, int>(coor.Item1 - last.Item1, coor.Item2 - last.Item2);
            Tuple<int, int> delta2 = new Tuple<int, int>(next.Item1 - coor.Item1, next.Item2 - coor.Item2);
            // Straight track.
            if (delta1.Equals(delta2)) {
                position = new Vector3(coor.Item1, 0, -coor.Item2);
                if (delta1.Item1 == -1) {
                    rotation = 0;
                } else if (delta1.Item1 == 1) {
                    rotation = 180;
                } else if (delta1.Item2 == -1) {
                    rotation = 90;
                } else {
                    rotation = 270;
                }
            } else {
                float x = (coor.Item1 + last.Item1 * CURVE_WEIGHT + next.Item1 * CURVE_WEIGHT) / (CURVE_WEIGHT * 2 + 1);
                float y = (coor.Item2 + last.Item2 * CURVE_WEIGHT + next.Item2 * CURVE_WEIGHT) / (CURVE_WEIGHT * 2 + 1);
                position = position = new Vector3(x, 0, -y);
                int sx = delta1.Item1 + delta2.Item1;
                int sy = delta1.Item2 + delta2.Item2;
                if (sx == -1 && sy == -1) {
                    rotation = 45;
                } else if (sx == 1 && sy == -1) {
                    rotation = 135;
                } else if (sx == -1 && sy == 1) {
                    rotation = 315;
                } else {
                    rotation = 225;
                }
            }
            return new Tuple<Vector3, Quaternion>(position, Quaternion.Euler(0, rotation, 0));
        }
    }

    public enum LevelTile {
        Ground, Track
    }

    public class Switch {
        public SwitchType type;
        public Tuple<int, int> coor;
        public int state;
        Tuple<int, int>[][] states;
        public SwitchInteraction interaction;

        public Switch(Level level, SwitchType type, Tuple<int, int> coor, int state, SwitchInteraction interaction) {
            this.type = type;
            this.coor = coor;
            this.state = state;
            this.interaction = interaction;
            int x = coor.Item1;
            int y = coor.Item2;
            bool trackLeft = x > 0 && level.tiles[x - 1, y] == LevelTile.Track;
            bool trackUp = y > 0 && level.tiles[x, y - 1] == LevelTile.Track;
            bool trackRight = x < level.tiles.GetLength(0) - 1 && level.tiles[x + 1, y] == LevelTile.Track;
            bool trackDown = y < level.tiles.GetLength(1) - 1 && level.tiles[x, y + 1] == LevelTile.Track;
            Debug.Assert(Util.CountTrue(trackLeft, trackUp, trackRight, trackDown) == 3, string.Format("Switch at {0},{1} isn't at a proper intersection.", coor.Item1, coor.Item2));
            Tuple<int, int> left = new Tuple<int, int>(coor.Item1 - 1, coor.Item2);
            Tuple<int, int> up = new Tuple<int, int>(coor.Item1, coor.Item2 - 1);
            Tuple<int, int> right = new Tuple<int, int>(coor.Item1 + 1, coor.Item2);
            Tuple<int, int> down = new Tuple<int, int>(coor.Item1, coor.Item2 + 1);
            Tuple<int, int>[] exits;
            if (!trackLeft) {
                exits = new Tuple<int, int>[] { up, right, down };
            } else if (!trackUp) {
                exits = new Tuple<int, int>[] { right, down, left };
            } else if (!trackRight) {
                exits = new Tuple<int, int>[] { down, left, up };
            } else {
                exits = new Tuple<int, int>[] { left, up, right };
            }
            // Set states.
            if (type == SwitchType.Fork) {
                states = new Tuple<int, int>[][] {
                    new Tuple<int, int>[]{ exits[1], exits[2] },
                    new Tuple<int, int>[]{ exits[1], exits[0] },
                };
            } else if (type == SwitchType.Left) {
                states = new Tuple<int, int>[][] {
                    new Tuple<int, int>[]{ exits[0], exits[2] },
                    new Tuple<int, int>[]{ exits[0], exits[1] },
                };
            } else {
                states = new Tuple<int, int>[][] {
                    new Tuple<int, int>[]{ exits[2], exits[0] },
                    new Tuple<int, int>[]{ exits[2], exits[1] },
                };
            }
        }
        public Tuple<int, int>[] GetNeighbors() {
            return states[state];
        }
        public Tuple<int, int>[] GetOtherState() {
            return states[(state + 1) % 2];
        }

        public void Flip() {
            state = (state + 1) % 2;
        }
    }

    public enum SwitchType {
        Left, Right, Fork
    }
    public enum SwitchInteraction {
        None, Click
    }
}
