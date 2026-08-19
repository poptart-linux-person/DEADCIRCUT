#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using FishNet.Object;
using DeadCircuit.Gameplay;
using DeadCircuit.Networking;
using DeadCircuit.UI;

namespace DeadCircuit.EditorTools
{
    public static class DeadCircuitSceneBuilder
    {
        const string Root = "Assets/DeadCircuit/Scenes";

        [MenuItem("Dead Circuit/Build Foundation Scenes")]
        public static void BuildAll()
        {
            System.IO.Directory.CreateDirectory(Application.dataPath + "/DeadCircuit/Scenes");
            CreateMenu();
            CreateMode("Story_01", false);
            CreateMode("Survival_01", true);
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Dead Circuit", "Foundation scenes generated. Open MainMenu first.", "Bet");
        }

        static void CreateMenu()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var state = new GameObject("GameState");
            state.AddComponent<DeadCircuitGameState>();
            var menu = new GameObject("MainMenu");
            menu.AddComponent<DeadCircuitMenu>();
            var canvas = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            CreateButton(canvas.transform, "STORY", new Vector2(0, 80), menu.GetComponent<DeadCircuitMenu>().PlayStory);
            CreateButton(canvas.transform, "SURVIVAL", new Vector2(0, 0), menu.GetComponent<DeadCircuitMenu>().PlaySurvival);
            CreateButton(canvas.transform, "QUIT", new Vector2(0, -80), menu.GetComponent<DeadCircuitMenu>().Quit);
            Save(scene, "MainMenu");
        }

        static void CreateMode(string name, bool survival)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var state = new GameObject("GameState");
            state.AddComponent<DeadCircuitGameState>();
            var director = new GameObject("MatchDirector");
            director.AddComponent<NetworkObject>();
            director.AddComponent<MatchDirector>();

            var gridRoot = new GameObject("PowerGrid");
            gridRoot.AddComponent<NetworkObject>();
            gridRoot.AddComponent<PowerGrid>();
            gridRoot.AddComponent<PoweredEquipment>();

            CreateRoom("StartRoom", Vector3.zero, new Vector3(22, 1, 22));
            CreateRoom("BackRoom", new Vector3(0, 0, 26), new Vector3(18, 1, 18));
            CreateWall("WallA", new Vector3(-11, 2, 13), new Vector3(1, 4, 26));
            CreateWall("WallB", new Vector3(11, 2, 13), new Vector3(1, 4, 26));
            CreateWall("WallC", new Vector3(0, 2, -13), new Vector3(22, 4, 1));
            CreateWall("WallD", new Vector3(0, 2, 39), new Vector3(22, 4, 1));

            var spawn = new GameObject(survival ? "SurvivalSpawn" : "StorySpawn");
            spawn.transform.position = new Vector3(0, 1, 4);

            var light = new GameObject("NightLight");
            var point = light.AddComponent<Light>();
            point.type = LightType.Point;
            point.range = 18;
            point.intensity = survival ? 2.5f : 3f;
            light.transform.position = new Vector3(0, 4, 4);

            var atmosphere = new GameObject("Atmosphere");
            atmosphere.AddComponent<DeadCircuit.Presentation.DeadCircuitAtmosphere>();

            CreateGenerator(new Vector3(8, 0.5f, 8), GeneratorType.Diesel);
            CreateGenerator(new Vector3(-8, 0.5f, 20), survival ? GeneratorType.Gas : GeneratorType.Emergency);
            CreateMonster(new Vector3(0, 1, 30));

            if (survival)
            {
                var note = new GameObject("SurvivalMarker");
                note.transform.position = new Vector3(0, 1, 25);
            }
            else
            {
                var objective = GameObject.CreatePrimitive(PrimitiveType.Cube);
                objective.name = "ChapterObjective";
                objective.transform.position = new Vector3(0, 1.1f, 28);
                objective.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
            }

            Save(scene, name);
        }

        static void CreateGenerator(Vector3 position, GeneratorType type)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = $"Generator_{type}";
            go.transform.position = position;
            go.transform.localScale = new Vector3(1.2f, 0.8f, 1.2f);
            go.AddComponent<NetworkObject>();
            go.AddComponent<GeneratorSystem>();
            var serialized = new SerializedObject(go.GetComponent<GeneratorSystem>());
            serialized.FindProperty("type").enumValueIndex = (int)type;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            go.AddComponent<PowerMinigame>();
        }

        static void CreateMonster(Vector3 position)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "DeadCircuitMonster";
            go.transform.position = position;
            go.transform.localScale = new Vector3(1.4f, 2.2f, 1.4f);
            go.AddComponent<NetworkObject>();
            go.AddComponent<DeadCircuit.AI.DeadCircuitMonster>();
            go.AddComponent<DeadCircuit.AI.MonsterCombatAbilities>();
            go.AddComponent<DeadCircuit.Combat.DynamicGrabQTE>();
            go.AddComponent<DeadCircuit.Combat.CoopQTE>();
        }

        static void CreateRoom(string name, Vector3 position, Vector3 scale)
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = name;
            floor.transform.position = position;
            floor.transform.localScale = scale;
        }

        static void CreateWall(string name, Vector3 position, Vector3 scale)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.position = position;
            wall.transform.localScale = scale;
        }

        static void CreateButton(Transform parent, string label, Vector2 position, UnityEngine.Events.UnityAction action)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(320, 64);
            rect.anchoredPosition = position;
            var text = new GameObject("Text", typeof(RectTransform), typeof(Text));
            text.transform.SetParent(go.transform, false);
            var t = text.GetComponent<Text>();
            t.text = label;
            t.alignment = TextAnchor.MiddleCenter;
            t.fontSize = 28;
            t.color = Color.white;
            var tr = text.GetComponent<RectTransform>();
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = tr.offsetMax = Vector2.zero;
            go.GetComponent<Button>().onClick.AddListener(action);
        }

        static void Save(Scene scene, string name)
        {
            EditorSceneManager.SaveScene(scene, $"{Root}/{name}.unity");
        }
    }
}
#endif
