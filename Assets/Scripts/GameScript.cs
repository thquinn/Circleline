using Assets.Code;
using Assets.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameScript : MonoBehaviour {
    static float SPEED = .015f;

    public GameObject prefabTrack, prefabTrain, prefabPlayerTrain;
    public Sprite spriteTrackTurn, spriteTrackSwitchFork, spriteTrackSwitchStraight, spriteTrackSwitchTurn;
    public TextAsset textLevel;
    public LayerMask layerMaskSwitch;

    public Camera cam;

    float t;
    Level level;
    Dictionary<Switch, SpriteRenderer> switchRenderers;
    Dictionary<Collider, Switch> switchColliders;
    Dictionary<Train, GameObject> trainObjects;
    List<Train> deadTrains;
    bool won = false;
    float wonTime;

    void Awake() {
        level = new Level(textLevel);
        ConstructLevel();
    }
    void ConstructLevel() {
        GameObject root = new GameObject("Level");
        switchRenderers = new Dictionary<Switch, SpriteRenderer>();
        switchColliders = new Dictionary<Collider, Switch>();
        // Place tracks.
        for (int y = 0; y < level.tiles.GetLength(1); y++) {
            for (int x = 0; x < level.tiles.GetLength(0); x++) {
                if (level.tiles[x, y] == LevelTile.Track) {
                    Tuple<int, int> coor = new Tuple<int, int>(x, y);
                    GameObject trackObject = Instantiate(prefabTrack, root.transform);
                    trackObject.transform.localPosition = new Vector3(x, 0, -y);
                    // Choose and rotate sprite.
                    SpriteRenderer trackRenderer = trackObject.GetComponent<SpriteRenderer>();
                    Collider trackCollider = trackObject.GetComponentInChildren<Collider>();
                    if (level.switches.ContainsKey(coor)) {
                        switchColliders[trackCollider] = level.switches[coor];
                    } else {
                        trackCollider.enabled = false;
                    }
                    bool trackLeft = x > 0 && level.tiles[x - 1, y] == LevelTile.Track;
                    bool trackUp = y > 0 && level.tiles[x, y - 1] == LevelTile.Track;
                    bool trackRight = x < level.tiles.GetLength(0) - 1 && level.tiles[x + 1, y] == LevelTile.Track;
                    bool trackDown = y < level.tiles.GetLength(1) - 1 && level.tiles[x, y + 1] == LevelTile.Track;
                    if (level.exit.Equals(coor)) {
                        int dx = 0, dy = 0;
                        if (trackLeft || trackRight) {
                            dx = trackLeft ? 1 : -1;
                            trackObject.transform.Rotate(0, 0, 90);
                        } else {
                            dy = trackUp ? 1 : -1;
                        }
                        // Draw additional track segments to the edge of the screen.
                        for (int i = 1; i <= 10; i++) {
                            GameObject extraTrack = Instantiate(prefabTrack, root.transform);
                            extraTrack.transform.localPosition = new Vector3(x + dx * i, 0, -y - dy * i);
                            extraTrack.transform.Rotate(0, 0, dx == 0 ? 0 : 90);
                        }
                    } else if (level.switches.ContainsKey(coor)) {
                        switchRenderers[level.switches[coor]] = trackRenderer;
                        if (!trackLeft) {
                            trackObject.transform.Rotate(0, 0, 90);
                        } else if (!trackDown) {
                            trackObject.transform.Rotate(0, 0, 180);
                        } else if (!trackRight) {
                            trackObject.transform.Rotate(0, 0, 270);
                        }
                    } else if (trackLeft && trackRight) {
                        trackObject.transform.Rotate(0, 0, 90);
                    } else if (trackRight && trackDown) {
                        trackRenderer.sprite = spriteTrackTurn;
                    } else if (trackRight && trackUp) {
                        trackRenderer.sprite = spriteTrackTurn;
                        trackObject.transform.Rotate(0, 0, 90);
                    } else if (trackLeft && trackUp) {
                        trackRenderer.sprite = spriteTrackTurn;
                        trackObject.transform.Rotate(0, 0, 180);
                    } else if (trackLeft && trackDown) {
                        trackRenderer.sprite = spriteTrackTurn;
                        trackObject.transform.Rotate(0, 0, 270);
                    }
                }
            }
        }
        // Place trains.
        trainObjects = new Dictionary<Train, GameObject>();
        foreach (Train train in level.trains) {
            trainObjects[train] = Instantiate(train.isPlayer ? prefabPlayerTrain : prefabTrain, root.transform);
        }
        deadTrains = new List<Train>();
        // Shift to center and zoom camera.
        root.transform.localPosition = new Vector3(-level.tiles.GetLength(0) / 2, .001f, level.tiles.GetLength(1) / 2);
        cam.orthographicSize = 1 + Mathf.Max(level.tiles.GetLength(0), level.tiles.GetLength(1)) / 4f;
    }

    // Update is called once per frame
    void Update()
    {
        UpdateMouse();
        UpdateSwitches();
        UpdateTrains();
    }
    void UpdateMouse() {
        if (Input.GetMouseButtonDown(0)) {
            Collider collider = Util.GetMouseCollider(layerMaskSwitch);
            if (collider == null || collider.gameObject.layer == 9) {
                return;
            }
            Switch sweetch = switchColliders[collider];
            foreach (Train train in trainObjects.Keys) {
                if (train.coor.Equals(sweetch.coor) || train.nextCoor.Equals(sweetch.coor)) {
                    return;
                }
            }
            sweetch.Flip();
        }
    }
    void UpdateSwitches() {
        foreach (var kvp in switchRenderers) {
            if (kvp.Key.type == SwitchType.Fork) {
                kvp.Value.sprite = spriteTrackSwitchFork;
                kvp.Value.flipX = kvp.Key.state == 1;
            } else if (kvp.Key.type == SwitchType.Left) {
                kvp.Value.sprite = kvp.Key.state == 0 ? spriteTrackSwitchStraight : spriteTrackSwitchTurn;
            } else if (kvp.Key.type == SwitchType.Right) {
                kvp.Value.sprite = kvp.Key.state == 0 ? spriteTrackSwitchStraight : spriteTrackSwitchTurn;
                kvp.Value.flipX = true;
            }
        }
    }
    void UpdateTrains() {
        t += SPEED;
        if (won) {
            wonTime += SPEED;
        }
        if (wonTime > 12) {
            UnityEditor.EditorApplication.isPlaying = false;
        }
        if (t >= 1) {
            t -= 1;
            for (int i = level.trains.Count - 1; i >= 0;  i--) {
                Train train = level.trains[i];
                if (train.nextCoor.Equals(level.exit)) {
                    deadTrains.Add(train);
                    level.trains.RemoveAt(i);
                    if (train.isPlayer) {
                        won = true;
                    }
                    continue;
                }
                train.Update(level.GetNextCoor(train.nextCoor, train.coor));
            }
        }
        foreach (Train train in level.trains) {
            GameObject trainObject = trainObjects[train];
            var oldTransform = level.GetTransform(train.coor, train.lastCoor);
            var newTransform = level.GetTransform(train.nextCoor, train.coor);
            float x = Util.EaseTrack(oldTransform.Item1.x, newTransform.Item1.x, t);
            float z = Util.EaseTrack(oldTransform.Item1.z, newTransform.Item1.z, t);
            trainObject.transform.localPosition = new Vector3(x, 0, z);
            trainObject.transform.localRotation = Quaternion.Lerp(oldTransform.Item2, newTransform.Item2, t);
        }
        foreach (Train train in deadTrains) {
            GameObject trainObject = trainObjects[train];
            float dx = train.nextCoor.Item1 - train.coor.Item1;
            float dz = train.nextCoor.Item2 - train.coor.Item2;
            trainObject.transform.Translate(dx * SPEED, 0, -dz * SPEED, Space.World);
        }
    }
}
