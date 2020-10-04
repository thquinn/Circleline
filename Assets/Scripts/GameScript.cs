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
    static float ACCEL = SPEED / 60;
    static float TRANSITION_DISTANCE = 50;
    static float TRANSITION_SPEED = .005f;

    public GameObject prefabTrack, prefabTrain, prefabPlayerTrain, prefabCollisionSign, prefabSwitchCircle, prefabTree;
    public Sprite spriteTrackTurn, spriteTrackSwitchFork, spriteTrackSwitchStraight, spriteTrackSwitchTurn, spriteTrackCross, spriteNoCargo;
    public AudioSource sfxSwitch, sfxSwitchAuto;
    public GameObject gridObject;
    public AudioMixer audioMixer;
    public TextAsset[] levelTexts;
    public LayerMask layerMaskSwitch;

    public Camera cam;

    float t;
    float speed;
    int levelIndex = 0;
    Level level, lastLevel;
    GameObject root, lastRoot;
    Dictionary<Switch, SpriteRenderer> switchRenderers;
    Dictionary<Collider, Switch> switchColliders;
    Dictionary<Switch, SpriteRenderer> switchCircleRenderers;
    Dictionary<Train, GameObject> trainObjects;
    bool won, lost;
    float wonTime;
    float lostLerpT;
    float transitionT;
    Vector3 transitionFrom, transitionTo;

    void Awake() {
        Application.targetFrameRate = 60;
        trainObjects = new Dictionary<Train, GameObject>();
        Restart();
        cam.orthographicSize = level.OrthographicSize();
    }
    void Restart(bool wipe = true) {
        level = new Level(levelTexts[levelIndex]);
        if (wipe) {
            Destroy(root);
            trainObjects.Clear();
        }
        won = false;
        lost = false;
        wonTime = 0;
        lostLerpT = 1;
        ConstructLevel();
        UpdateTrains(level);
        UpdateSwitches();
    }
    void TransitionToNextLevel() {
        levelIndex++;
        lastLevel = level;
        lastRoot = root;
        Restart(false);
        // Place the newly constructed level.
        List<LevelExitDirection> possibleDirections = new List<LevelExitDirection>(Enum.GetValues(typeof(LevelExitDirection)) as LevelExitDirection[]);
        possibleDirections.Remove(lastLevel.exitDirection);
        possibleDirections.Remove(Util.GetOppositeDirection(level.exitDirection));
        LevelExitDirection direction = possibleDirections[UnityEngine.Random.Range(0, possibleDirections.Count)];
        transitionT = 0;
        transitionTo = root.transform.localPosition;
        root.transform.localPosition += Util.GetDirectionVector(direction, TRANSITION_DISTANCE);
        transitionFrom = root.transform.localPosition;
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
                            level.exitDirection = trackLeft ? LevelExitDirection.Right : LevelExitDirection.Left;
                            trackObject.transform.Rotate(0, 0, 90);
                        } else {
                            dy = trackUp ? 1 : -1;
                            level.exitDirection = trackLeft ? LevelExitDirection.Down : LevelExitDirection.Up;
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
                        if (sweetch.interaction != SwitchInteraction.None) {
                            GameObject switchCircle = Instantiate(prefabSwitchCircle, root.transform);
                            switchCircle.transform.localPosition = new Vector3(x, 0, -y);
                            SpriteRenderer switchCircleRenderer = switchCircle.transform.GetChild(0).GetComponent<SpriteRenderer>();
                            switchCircleRenderers[sweetch] = switchCircleRenderer;
                            if (sweetch.interaction == SwitchInteraction.Auto) {
                                switchCircleRenderer.color = new Color(1, .64f, 0);
                            }
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
        foreach (Train train in level.trains) {
            trainObjects[train] = Instantiate(train.isPlayer ? prefabPlayerTrain : prefabTrain, root.transform);
        }
        // Place trees.
        int width = level.tiles.GetLength(0);
        int height = level.tiles.GetLength(1);
        int maxDim = Mathf.Max(width, height);
        HashSet<Tuple<int, int>> treeCoors = new HashSet<Tuple<int, int>>();
        UnityEngine.Random.State prevRandom = UnityEngine.Random.state;
        UnityEngine.Random.InitState(levelTexts[levelIndex].name.GetHashCode());
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
            GameObject tree = Instantiate(prefabTree, root.transform);
            tree.transform.localPosition = new Vector3(treeCoor.Item1, 0, -treeCoor.Item2);
            tree.GetComponent<TreeScript>().Init();
        }
        UnityEngine.Random.state = prevRandom;
        // Shift to center and zoom camera.
        root.transform.localPosition = new Vector3(-width / 2, .001f, height / 2);
    }

    void Update() {
        if (lastLevel != null) {
            UpdateTransition();
            return;
        }
        if (Input.GetKeyDown(KeyCode.Escape)) {
            Application.Quit();
            return;
        }
        bool canRestart = !won || lost;
        if (canRestart && Input.GetKeyDown(KeyCode.R)) {
            Restart();
            return;
        }
        UpdateMouse();
        UpdateSwitches();
        if (!lost) {
            speed = Mathf.Min(speed + ACCEL, SPEED);
        }
        UpdateTrains(level);
        UpdateAudio();
        if (wonTime > 12) {
            TransitionToNextLevel();
        }
    }
    void UpdateTransition() {
        UpdateTrains(lastLevel, false);
        float previousT = transitionT;
        transitionT = Mathf.Min(1, transitionT + TRANSITION_SPEED);
        if (previousT < .5f && transitionT >= .5f) {
            t = 0;
            speed = 0;
            UpdateTrains(level);
            UpdateAudio();
        }
        float easedT = EasingFunction.EaseInOutQuad(0, 1, transitionT);
        Vector3 lastPos = root.transform.localPosition;
        root.transform.localPosition = Vector3.Lerp(transitionFrom, transitionTo, easedT);
        Vector3 delta = root.transform.localPosition - lastPos;
        lastRoot.transform.localPosition += delta;
        gridObject.transform.localPosition += delta;
        gridObject.transform.localPosition = new Vector3(gridObject.transform.localPosition.x % 1, gridObject.transform.localPosition.y, gridObject.transform.localPosition.z % 1);
        float lastZoom = lastLevel.OrthographicSize();
        float nextZoom = level.OrthographicSize();
        cam.orthographicSize = Mathf.Lerp(lastZoom, nextZoom, easedT);
        if (transitionT >= 1) {
            foreach (Train train in lastLevel.trains) {
                trainObjects.Remove(train);
            }
            foreach (Train train in lastLevel.deadTrains) {
                trainObjects.Remove(train);
            }
            lastLevel = null;
            Destroy(lastRoot);
        }
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
            } else if (kvp.Key.type == SwitchType.Cross) {
                kvp.Value.sprite = spriteTrackCross;
            }
        }
    }
    void UpdateTrains(Level trainLevel, bool realUpdate = true) {
        if (lost) {
            speed *= lostLerpT;
        }
        t += speed;
        if (won) {
            wonTime += speed;
        }
        if (t >= 1) {
            t -= 1;
            for (int i = trainLevel.trains.Count - 1; i >= 0; i--) {
                Train train = trainLevel.trains[i];
                if (train.nextCoor.Equals(trainLevel.exit)) {
                    if (train.isPlayer && realUpdate) {
                        won = true;
                    } else if (!won && realUpdate) {
                        // The player train must be first to leave the level.
                        lost = true;
                        lostLerpT = .88f;
                        GameObject sign = Instantiate(prefabCollisionSign, root.transform);
                        sign.transform.localPosition = new Vector3(train.nextCoor.Item1 + .5f, 0, -train.nextCoor.Item2 - .5f);
                        sign.transform.GetChild(0).GetComponent<SpriteRenderer>().sprite = spriteNoCargo;
                    }
                    trainLevel.deadTrains.Add(train);
                    trainLevel.trains.RemoveAt(i);
                } else {
                    train.Update(trainLevel.GetNextCoor(train.nextCoor, train.coor));
                }
            }
            if (realUpdate) {
                CollisionCheck();
            }
            // Switch auto switches.
            Tuple<int, int>[] vacatedSpaces = trainLevel.trains.Select(t => t.lastCoor).Except(trainLevel.trains.Select(t => t.coor)).ToArray();
            foreach (Tuple<int, int> vacatedSpace in vacatedSpaces) {
                if (!trainLevel.switches.ContainsKey(vacatedSpace)) {
                    continue;
                }
                Switch sweetch = trainLevel.switches[vacatedSpace];
                if (sweetch.interaction == SwitchInteraction.Auto) {
                    sweetch.Flip();
                    sfxSwitchAuto.Play();
                }
            }
        }
        foreach (Train train in trainLevel.trains) {
            GameObject trainObject = trainObjects[train];
            var oldTransform = trainLevel.GetTransform(train.coor, train.lastCoor);
            var newTransform = trainLevel.GetTransform(train.nextCoor, train.coor);
            float x = Util.EaseTrack(oldTransform.Item1.x, newTransform.Item1.x, t);
            float z = Util.EaseTrack(oldTransform.Item1.z, newTransform.Item1.z, t);
            trainObject.transform.localPosition = new Vector3(x, 0, z);
            trainObject.transform.localRotation = Quaternion.Lerp(oldTransform.Item2, newTransform.Item2, t);
        }
        foreach (Train train in trainLevel.deadTrains) {
            GameObject trainObject = trainObjects[train];
            float dx = train.nextCoor.Item1 - train.coor.Item1;
            float dz = train.nextCoor.Item2 - train.coor.Item2;
            trainObject.transform.Translate(dx * speed, 0, -dz * speed, Space.World);
        }
    }
    void CollisionCheck() {
        if (lost) {
            return;
        }
        Vector3 signPosition = Vector3.zero;
        foreach (Train train in level.trains) {
            foreach (Train other in level.trains) {
                if (train == other) {
                    continue;
                }
                if (train.nextCoor.Equals(other.coor) && other.nextCoor.Equals(train.coor)) {
                    lost = true;
                    lostLerpT = .1f;
                    signPosition = new Vector3(train.nextCoor.Item1 + .5f, 0, -train.nextCoor.Item2 - .5f);
                }
                if (train.nextCoor.Equals(other.nextCoor)) {
                    lost = true;
                    lostLerpT = Mathf.Min(lostLerpT, .88f);
                    signPosition = new Vector3(train.nextCoor.Item1 + .5f, 0, -train.nextCoor.Item2 - .5f);
                }
            }
        }
        if (lost) {
            Instantiate(prefabCollisionSign, root.transform).transform.localPosition = signPosition;
        }
    }

    void UpdateAudio() {
        audioMixer.SetFloat("TrainVol", Mathf.Lerp(-12, -80, Mathf.InverseLerp(SPEED, 0, speed)));
    }
}
