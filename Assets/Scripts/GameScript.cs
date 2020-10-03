using Assets.Code;
using Assets.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameScript : MonoBehaviour {
    public GameObject prefabTrack, prefabTrain;
    public Sprite spriteTrackStraight, spriteTrackTurn;
    public TextAsset textLevel;

    public Camera cam;

    float t;
    Level level;
    Dictionary<Train, GameObject> trainObjects;

    void Awake() {
        level = new Level(textLevel);
        ConstructLevel();
    }
    void ConstructLevel() {
        GameObject root = new GameObject("Level");
        // Place tracks.
        for (int y = 0; y < level.tiles.GetLength(1); y++) {
            for (int x = 0; x < level.tiles.GetLength(0); x++) {
                if (level.tiles[x, y] == LevelTile.Track) {
                    GameObject trackObject = Instantiate(prefabTrack, root.transform);
                    trackObject.transform.localPosition = new Vector3(x, 0, -y);
                    // Choose and rotate sprite.
                    SpriteRenderer trackRenderer = trackObject.GetComponent<SpriteRenderer>();
                    bool trackLeft = x > 0 && level.tiles[x - 1, y] == LevelTile.Track;
                    bool trackUp = y > 0 && level.tiles[x, y - 1] == LevelTile.Track;
                    bool trackRight = x < level.tiles.GetLength(0) - 1 && level.tiles[x + 1, y] == LevelTile.Track;
                    bool trackDown = y < level.tiles.GetLength(1) - 1 && level.tiles[x, y + 1] == LevelTile.Track;
                    if (trackLeft && trackRight) {
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
            trainObjects[train] = Instantiate(prefabTrain, root.transform);
        }
        // Shift to center and zoom camera.
        root.transform.localPosition = new Vector3(-level.tiles.GetLength(0) / 2, .001f, level.tiles.GetLength(1) / 2);
        cam.orthographicSize = 1 + Mathf.Max(level.tiles.GetLength(0), level.tiles.GetLength(1)) / 4f;
    }

    // Update is called once per frame
    void Update()
    {
        UpdateTrains();
    }
    void UpdateTrains() {
        t += .01f;
        if (t >= 1) {
            t -= 1;
            foreach (Train train in trainObjects.Keys) {
                var neighbors = level.NeighborCoors(train.nextCoor);
                foreach (Tuple<int, int> neighbor in neighbors) {
                    if (!neighbor.Equals(train.coor)) {
                        train.coor = train.nextCoor;
                        train.nextCoor = neighbor;
                        break;
                    }
                }
            }
        }
        foreach (var kvp in trainObjects) {
            var oldTransform = level.GetTransform(kvp.Key.coor);
            var newTransform = level.GetTransform(kvp.Key.nextCoor);
            float x = Util.EaseCurvedTrack(oldTransform.Item1.x, newTransform.Item1.x, t);
            float z = Util.EaseCurvedTrack(oldTransform.Item1.z, newTransform.Item1.z, t);
            float targetAngle = newTransform.Item2;
            if (targetAngle - oldTransform.Item2 < -90) {
                targetAngle += 180;
            } else if (targetAngle - oldTransform.Item2 > 90) {
                targetAngle -= 180;
            }
            kvp.Value.transform.localPosition = new Vector3(x, 0, z);
            kvp.Value.transform.localRotation = Quaternion.Euler(0, Mathf.Lerp(oldTransform.Item2, targetAngle, t), 0);
        }
    }
}
