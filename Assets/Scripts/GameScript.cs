using Assets.Code;
using Assets.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class GameScript : MonoBehaviour {
    static float SPEED = .066f;

    public GameObject prefabTrack, prefabTrain, prefabPlayerTrain, prefabCollisionSign, prefabSwitchCircle, prefabTree;
    public Sprite spriteTrackTurn, spriteTrackSwitchFork, spriteTrackSwitchStraight, spriteTrackSwitchTurn, spriteNoCargo;
    public AudioMixer audioMixer;
    public AudioSource sfxSwitch;
    public TextAsset textLevel;
    public LayerMask layerMaskSwitch;

    public Camera cam;

    float t;
    float speed = SPEED;
    Level level;
    GameObject root;
    Dictionary<Switch, SpriteRenderer> switchRenderers;
    Dictionary<Collider, Switch> switchColliders;
    Dictionary<Switch, SpriteRenderer> switchCircleRenderers;
    Dictionary<Train, GameObject> trainObjects;
    List<Train> deadTrains;
    bool won = false, lost = false;
    float wonTime;

    void Awake() {
        Application.targetFrameRate = 60;
        level = new Level(textLevel);
        ConstructLevel();
    }
    void ConstructLevel() {
        root = new GameObject("Level");
        switchRenderers = new Dictionary<Switch, SpriteRenderer>();
        switchColliders = new Dictionary<Collider, Switch>();
        switchCircleRenderers = new Dictionary<Switch, SpriteRenderer>();
        HashSet<Tuple<int, int>> extraTrackCoors = new HashSet<Tuple<int, int>>();
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
                    if (level.switches.ContainsKey(coor) && level.switches[coor].interaction == SwitchInteraction.Click) {
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
                        for (int i = 1; i <= 20; i++) {
                            GameObject extraTrack = Instantiate(prefabTrack, root.transform);
                            int extraX = x + dx * i;
                            int extraY = y + dy * i;
                            extraTrack.transform.localPosition = new Vector3(extraX, 0, -extraY);
                            extraTrack.transform.Rotate(0, 0, dx == 0 ? 0 : 90);
                            extraTrackCoors.Add(new Tuple<int, int>(extraX, extraY));
                        }
                    } else if (level.switches.ContainsKey(coor)) {
                        Switch sweetch = level.switches[coor];
                        switchRenderers[sweetch] = trackRenderer;
                        if (!trackLeft) {
                            trackObject.transform.Rotate(0, 0, 90);
                        } else if (!trackDown) {
                            trackObject.transform.Rotate(0, 0, 180);
                        } else if (!trackRight) {
                            trackObject.transform.Rotate(0, 0, 270);
                        }
                        if (sweetch.interaction == SwitchInteraction.Click) {
                            GameObject switchCircle = Instantiate(prefabSwitchCircle, root.transform);
                            switchCircle.transform.localPosition = new Vector3(x, 0, -y);
                            switchCircleRenderers[sweetch] = switchCircle.transform.GetChild(0).GetComponent<SpriteRenderer>();
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
        // Place trees.
        int width = level.tiles.GetLength(0);
        int height = level.tiles.GetLength(1);
        int maxDim = Mathf.Max(width, height);
        HashSet<Tuple<int, int>> treeCoors = new HashSet<Tuple<int, int>>();
        UnityEngine.Random.State prevRandom = UnityEngine.Random.state;
        UnityEngine.Random.InitState(textLevel.name.GetHashCode());
        for (int i = 0; i < maxDim * maxDim / 2; i++) {
            int x = UnityEngine.Random.Range(-maxDim, maxDim * 2);
            int y = UnityEngine.Random.Range(-maxDim, maxDim * 2);
            Tuple<int, int> treeCoor = new Tuple<int, int>(x, y);
            if (treeCoors.Contains(treeCoor)) {
                continue;
            }
            if (x >= 0 && x < width && y >= 0 && y < height && level.tiles[x, y] != LevelTile.Ground) {
                continue;
            }
            if (extraTrackCoors.Contains(treeCoor)) {
                continue;
            }
            treeCoors.Add(treeCoor);
            Instantiate(prefabTree, root.transform).transform.localPosition = new Vector3(treeCoor.Item1, 0, -treeCoor.Item2);
        }
        UnityEngine.Random.state = prevRandom;
        // Shift to center and zoom camera.
        root.transform.localPosition = new Vector3(-width / 2, .001f, height / 2);
        cam.orthographicSize = 1 + maxDim / 3.5f;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) {
            Application.Quit();
            return;
        }
        if (Input.GetKeyDown(KeyCode.R)) {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            return;
        }
        UpdateMouse();
        UpdateSwitches();
        UpdateTrains();
        UpdateAudio();
    }
    void UpdateMouse() {
        Collider collider = (won || lost) ? null : Util.GetMouseCollider(layerMaskSwitch);
        if (collider == null || !switchColliders.ContainsKey(collider)) {
            foreach (var kvp in switchCircleRenderers) {
                kvp.Value.transform.parent.localScale = Vector3.Lerp(kvp.Value.transform.parent.localScale, new Vector3(1, 1, 1), .2f);
            }
            return;
        }
        Switch sweetch = switchColliders[collider];
        foreach (var kvp in switchCircleRenderers) {
            if (kvp.Key == sweetch) {
                kvp.Value.transform.parent.localScale = Vector3.Lerp(kvp.Value.transform.parent.localScale, new Vector3(.9f, .9f, .9f), .2f);
            } else {
                kvp.Value.transform.parent.localScale = Vector3.Lerp(kvp.Value.transform.parent.localScale, new Vector3(1, 1, 1), .2f);
            }
        }
        if (Input.GetMouseButtonDown(0)) {
            foreach (Train train in trainObjects.Keys) {
                if (train.coor.Equals(sweetch.coor) || train.nextCoor.Equals(sweetch.coor)) {
                    return;
                }
            }
            sweetch.Flip();
            sfxSwitch.Play();
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
        if (lost) {
            speed *= .88f;
        }
        t += speed;
        if (won) {
            wonTime += speed;
        }
        if (wonTime > 12) {
            // TODO: level transition
        }
        if (t >= 1) {
            t -= 1;
            for (int i = level.trains.Count - 1; i >= 0;  i--) {
                Train train = level.trains[i];
                if (train.nextCoor.Equals(level.exit)) {
                    if (won || train.isPlayer) {
                        won = true;
                        deadTrains.Add(train);
                        level.trains.RemoveAt(i);
                    } else if (!won) {
                        // The player train must be first to leave the level.
                        lost = true;
                        deadTrains.Add(train);
                        level.trains.RemoveAt(i);
                        GameObject sign = Instantiate(prefabCollisionSign, root.transform);
                        sign.transform.localPosition = new Vector3(train.nextCoor.Item1 + .5f, 0, -train.nextCoor.Item2 - .5f);
                        sign.transform.GetChild(0).GetComponent<SpriteRenderer>().sprite = spriteNoCargo;
                    }
                } else {
                    train.Update(level.GetNextCoor(train.nextCoor, train.coor));
                }
            }
            // Check for collisions.
            HashSet<Tuple<int, int>> collisions = new HashSet<Tuple<int, int>>();
            foreach (Train train in level.trains) {
                Tuple<int, int> collision = train.nextCoor;
                if (collisions.Contains(collision)) {
                    continue;
                }
                collisions.Add(collision);
                foreach (Train other in level.trains) {
                    if (train == other) {
                        continue;
                    }
                    if (train.nextCoor.Equals(other.nextCoor) || (train.nextCoor.Equals(other.coor) && other.nextCoor.Equals(train.coor))) {
                        lost = true;
                        Instantiate(prefabCollisionSign, root.transform).transform.localPosition = new Vector3(train.nextCoor.Item1 + .5f, 0, -train.nextCoor.Item2 - .5f);
                    }
                }
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
            trainObject.transform.Translate(dx * speed, 0, -dz * speed, Space.World);
        }
    }
    void UpdateAudio() {
        audioMixer.SetFloat("TrainVol", Mathf.Lerp(-15, -80, Mathf.InverseLerp(SPEED, 0, speed)));
    }
}
