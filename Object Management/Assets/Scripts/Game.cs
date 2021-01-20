using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;
using System.IO;
using UnityEngine.UI;

public class Game : PersistableObject {

    List<Shape> shapes;
    //string savePath;

    [SerializeField] ShapeFactory shapeFactory;
    [SerializeField] bool reseedOnLoad;
    [SerializeField] Slider creationSpeedSlider;
    [SerializeField] Slider destructionSpeedSlider;

    public KeyCode createKey = KeyCode.C;
    public KeyCode destroyKey = KeyCode.X;
    public KeyCode newGameKey = KeyCode.N;
    public KeyCode saveKey = KeyCode.S;
    public KeyCode loadKey = KeyCode.L;

    public PersistentStorage storage;

    public Random.State mainRandomState;

    const int saveVersion = 4;
    public int levelCount;
    public int loadedLevelBuildIndex;
    public float creationProgress, DestructionProgress;
    public float CreationSpeed { get; set; }
    public float DestructionSpeed { get; set; }

    public void Awake()
    {
        shapes = new List<Shape>();
        //savePath = Path.Combine(Application.persistentDataPath, "saveFile");
    }

    public void Start()
    {
        mainRandomState = Random.state;

        if (Application.isEditor) {
            for (int i = 0; i < SceneManager.sceneCount; i++) {
                Scene loadedScene = SceneManager.GetSceneAt(i);
                if (loadedScene.name.Contains("Level ")) {
                    SceneManager.SetActiveScene(loadedScene);
                    loadedLevelBuildIndex = loadedScene.buildIndex;
                    return;
                }
            }
        }
        BeginNewGame();
        StartCoroutine(LoadLevel(1));       
    }

    public void Update()
    {
        if (Input.GetKeyDown(createKey)) {
            //Instantiate(prefab);
            CreateShape();
        }
        else if (Input.GetKeyDown(newGameKey)) {
            BeginNewGame();
            StartCoroutine(LoadLevel(loadedLevelBuildIndex));
        }
        else if (Input.GetKeyDown(saveKey)) {
            storage.Save(this, saveVersion);
            //Save();
        }
        else if (Input.GetKeyDown(loadKey)) {
            BeginNewGame();
            storage.Load(this);
            //Load();
        }
        else if (Input.GetKeyDown(destroyKey)) {
            DestroyShape();
        }
        else{
            for (int i = 1; i <= levelCount; i++) {
                if (Input.GetKeyDown(KeyCode.Alpha0 + i)) {
                    BeginNewGame();
                    StartCoroutine(LoadLevel(i));
                    return;
                }
            }
        }
    }

    public void FixedUpdate()
    {
        for (int i = 0; i < shapes.Count; i++)
        {
            shapes[i].GameUpdate();
        }
        creationProgress += Time.deltaTime * CreationSpeed;
        while(creationProgress >= 1f) {
            creationProgress -= 1f;
            CreateShape();
        }
        DestructionProgress += Time.deltaTime * DestructionSpeed;
        while (DestructionProgress >= 1f) {
            DestructionProgress -= 1f;
            DestroyShape();
        }
        
    }

    public void CreateShape() {
        Shape instance = shapeFactory.GetRandom();
        GameLevel.Current.ConfigureSpawn(instance);
        shapes.Add(instance);
    }
    public void BeginNewGame() {
        Random.state = mainRandomState;
        int seed = Random.Range(0, int.MaxValue);
        mainRandomState = Random.state;
        Random.InitState(seed);
        CreationSpeed = 0;
        creationSpeedSlider.value = 0;
        DestructionSpeed = 0;
        destructionSpeedSlider.value = 0;

        for (int i = 0; i < shapes.Count; i++) {
            //Destroy(shapes[i].gameObject);
            shapeFactory.Reclaim(shapes[i]);
        }
        shapes.Clear();
    }

    public override void Save(GameDataWriter writer)
    {
        //writer.Write(-saveVersion);
        writer.Write(shapes.Count);
        writer.Write(Random.state);
        writer.Write(CreationSpeed);
        writer.Write(creationProgress);
        writer.Write(DestructionSpeed);
        writer.Write(DestructionProgress);
        writer.Write(loadedLevelBuildIndex);
        GameLevel.Current.Save(writer); 
        for (int i = 0; i < shapes.Count; i++) {
            writer.Write(shapes[i].ShapeId);
            writer.Write(shapes[i].MaterialId);
            shapes[i].Save(writer);
        }
    }

    public override void Load(GameDataReader reader)
    {
        int version = reader.Version;
        if (version > saveVersion) {
            Debug.LogError("Unsupported future save version" + version);
            return;
        }
        StartCoroutine(LoadGame(reader));
    }
    IEnumerator LoadGame (GameDataReader reader) {
        int version = reader.Version;
        int count = version <= 0 ? -version : reader.ReadInt();
        if (version >= 3) {
            Random.State state = reader.ReadRandomState();
            if (!reseedOnLoad) {
                Random.state = state;
            }
            creationSpeedSlider.value = CreationSpeed = reader.ReadFloat();
            creationProgress = reader.ReadFloat();
            destructionSpeedSlider.value =  DestructionSpeed = reader.ReadFloat();
            DestructionProgress = reader.ReadFloat();
        }
        yield return LoadLevel(version < 2 ? 1 : reader.ReadInt());
        if (version >= 3) {
            GameLevel.Current.Load(reader);
        }
        for (int i = 0; i < count; i++) {
            int shapeId = version > 0 ? reader.ReadInt() : 0;
            int materialId = version > 0 ? reader.ReadInt() : 0;
            Shape instance = shapeFactory.Get(shapeId, materialId);
            instance.Load(reader);
            shapes.Add(instance);
        }
    }

    public void DestroyShape() {
        if (shapes.Count > 0) {
            int index = Random.Range(0, shapes.Count);
            //Destroy(shapes[index].gameObject);
            shapeFactory.Reclaim(shapes[index]);
            int lastIndex = shapes.Count - 1;
            shapes[index] = shapes[lastIndex];
            shapes.RemoveAt(lastIndex);
        }
    }

    public static Game Instance { get; private set; }

    IEnumerator LoadLevel (int levelBuildIndex) {
        //SceneManager.LoadScene("Level 1", LoadSceneMode.Additive);
        //yield return null;
        enabled = false;
        if (loadedLevelBuildIndex > 0) {
            yield return SceneManager.UnloadSceneAsync(loadedLevelBuildIndex);
        }
        yield return SceneManager.LoadSceneAsync(levelBuildIndex, LoadSceneMode.Additive);
        SceneManager.SetActiveScene(SceneManager.GetSceneByBuildIndex(levelBuildIndex));
        loadedLevelBuildIndex = levelBuildIndex;
        enabled = true;
    }

    //public void Save() {
    //    using (
    //        var writer = new BinaryWriter(File.Open(savePath, FileMode.Create))
    //    ) {
    //        writer.Write(objects.Count);
    //        for (int i = 0; i < objects.Count; i++) {
    //            Transform t = objects[i];
    //            writer.Write(t.localPosition.x);
    //            writer.Write(t.localPosition.y);
    //            writer.Write(t.localPosition.z);
    //        }
    //    }
    //    //Debug.Log(savePath);
    //}

    //public void Load() {
    //    BeginNewGame();
    //    using (
    //        var reader = new BinaryReader(File.Open(savePath, FileMode.Open))
    //        ) {
    //        int count = reader.ReadInt32();
    //        for (int i = 0; i < count; i++) {
    //            Vector3 p;
    //            p.x = reader.ReadSingle();
    //            p.y = reader.ReadSingle();
    //            p.z = reader.ReadSingle();
    //            Transform t = Instantiate(prefab);
    //            t.localPosition = p;
    //            objects.Add(t);
    //        }
    //    }
    //}

}
