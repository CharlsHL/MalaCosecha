using System.IO;
using System.Linq;
using MalaCosecha.Interaction;
using MalaCosecha.Inventory;
using MalaCosecha.Player;
using MalaCosecha.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace MalaCosecha.EditorTools
{
    /// <summary>
    /// Herramienta de Editor: arma la escena de prueba del Hito 1 con un clic.
    /// Menú: MalaCosecha > Hito 1 > Crear escena de prueba
    ///
    /// Los scripts dentro de una carpeta llamada "Editor" solo existen en el Editor de Unity:
    /// no se incluyen en el juego compilado. Por eso pueden usar UnityEditor.
    ///
    /// Qué crea:
    ///  - Materiales greybox en Art/Greybox/Materials (colores planos).
    ///  - Prefabs Player, Pickup_Materiales y Pickup_Municion en Prefabs/
    ///    (si ya existen, los reutiliza: tus cambios en los prefabs no se pisan).
    ///  - La escena Scenes/Prototipo_Hito1 con un circuito para probar cada mecánica.
    /// </summary>
    public static class Hito1SceneBuilder
    {
        const string ScenePath = "Assets/_Project/Scenes/Prototipo_Hito1.unity";
        const string MaterialsFolder = "Assets/_Project/Art/Greybox/Materials";
        const string PrefabsFolder = "Assets/_Project/Prefabs";

        // Código de colores del greybox: de un vistazo se sabe qué es cada cosa.
        static Material groundMat, wallMat, obstacleMat, houseMat, playerMat, materialsMat, ammoMat;

        [MenuItem("MalaCosecha/Hito 1/Crear escena de prueba")]
        public static void BuildScene()
        {
            if (File.Exists(ScenePath) &&
                !EditorUtility.DisplayDialog("Escena existente",
                    $"{ScenePath} ya existe. ¿Reemplazarla?\n(Los prefabs existentes se conservan.)",
                    "Reemplazar", "Cancelar"))
                return;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            CreateMaterials();

            // DefaultGameObjects trae cámara + luz direccional + skybox. La cámara la borramos
            // porque el Player trae la suya.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            Object.DestroyImmediate(GameObject.Find("Main Camera"));
            var sun = GameObject.Find("Directional Light");
            if (sun != null)
            {
                sun.name = "Sol";
                sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }

            BuildLevel();

            var playerPrefab = GetOrCreatePrefab($"{PrefabsFolder}/Player.prefab", BuildPlayer);
            var materialsPrefab = GetOrCreatePrefab($"{PrefabsFolder}/Pickup_Materiales.prefab",
                () => BuildPickup("Pickup_Materiales", ResourceType.Materials, 5, materialsMat, 0.5f));
            var ammoPrefab = GetOrCreatePrefab($"{PrefabsFolder}/Pickup_Municion.prefab",
                () => BuildPickup("Pickup_Municion", ResourceType.Ammo, 4, ammoMat, 0.35f));

            var player = Spawn(playerPrefab, new Vector3(0f, 0f, -8f), null);

            var pickups = new GameObject("Recursos").transform;
            Vector3[] materialSpots =
            {
                new(3f, 0f, -4f), new(-12f, 0f, 3f), new(6f, 2f, 11.5f), new(-8f, 0f, 12f), new(12f, 0f, -10f)
            };
            foreach (var spot in materialSpots)
                Spawn(materialsPrefab, spot, pickups);

            Spawn(ammoPrefab, new Vector3(-3f, 1f, 4f), pickups);   // arriba del obstáculo de 1 m
            Spawn(ammoPrefab, new Vector3(14f, 0f, 14f), pickups);

            new GameObject("DebugHUD", typeof(DebugHUD));

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
            Selection.activeGameObject = player;

            Debug.Log($"[MalaCosecha] Escena creada: {ScenePath}. Dale Play para probar.");
        }

        // ---------------------------------------------------------------- Nivel

        static void BuildLevel()
        {
            var level = new GameObject("Nivel").transform;

            Block("Suelo", new(0f, -0.5f, 0f), new(60f, 1f, 60f), groundMat, level);

            // Límites para no caerse del mundo.
            Block("Limite_Norte", new(0f, 1.5f, 30.5f), new(62f, 3f, 1f), wallMat, level);
            Block("Limite_Sur", new(0f, 1.5f, -30.5f), new(62f, 3f, 1f), wallMat, level);
            Block("Limite_Este", new(30.5f, 1.5f, 0f), new(1f, 3f, 60f), wallMat, level);
            Block("Limite_Oeste", new(-30.5f, 1.5f, 0f), new(1f, 3f, 60f), wallMat, level);

            // Saltos: 0.5 m se salta fácil, 1 m justo, 1.6 m imposible.
            var jumps = Group("Zona_Saltos", level);
            Block("Obstaculo_0.5m", new(-6f, 0.25f, 4f), new(2f, 0.5f, 1f), obstacleMat, jumps);
            Block("Obstaculo_1m", new(-3f, 0.5f, 4f), new(2f, 1f, 1f), obstacleMat, jumps);
            Block("Muro_1.6m", new(0f, 0.8f, 4f), new(2f, 1.6f, 1f), obstacleMat, jumps);

            // Rampa con plataforma: prueba pendientes y que isGrounded no parpadee al bajar.
            var ramp = Group("Zona_Rampa", level);
            var slope = Block("Rampa", new(6f, 0.9f, 6f), new(3f, 0.2f, 6f), obstacleMat, ramp);
            slope.transform.rotation = Quaternion.Euler(-20f, 0f, 0f);
            Block("Plataforma", new(6f, 1.5f, 10.8f), new(3f, 1f, 4f), obstacleMat, ramp);

            // Túnel de 1.3 m de alto: de pie (1.8 m) no se pasa, agachado (1 m) sí.
            var tunnel = Group("Zona_Tunel", level);
            Block("Pared_Izquierda", new(-13.25f, 0.65f, 3f), new(0.5f, 1.3f, 6f), wallMat, tunnel);
            Block("Pared_Derecha", new(-10.75f, 0.65f, 3f), new(0.5f, 1.3f, 6f), wallMat, tunnel);
            Block("Techo", new(-12f, 1.4f, 3f), new(3f, 0.2f, 6f), wallMat, tunnel);

            // Referencia de escala y orientación. En el Hito 2 se convierte en la granja.
            Block("Casa_Placeholder", new(0f, 2f, 20f), new(8f, 4f, 6f), houseMat, level);

            var crates = Group("Cajas", level);
            Block("Caja", new(10f, 0.5f, -3f), Vector3.one, obstacleMat, crates);
            Block("Caja", new(11.2f, 0.5f, -3.3f), Vector3.one, obstacleMat, crates);
            Block("Caja", new(10.5f, 1.5f, -3.1f), Vector3.one, obstacleMat, crates);
            Block("Caja", new(-15f, 0.5f, -12f), Vector3.one, obstacleMat, crates);
            Block("Caja", new(18f, 0.75f, 8f), new(1.5f, 1.5f, 1.5f), obstacleMat, crates);
        }

        static GameObject Block(string name, Vector3 position, Vector3 scale, Material material, Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            go.isStatic = true;
            return go;
        }

        static Transform Group(string name, Transform parent)
        {
            var group = new GameObject(name).transform;
            group.SetParent(parent, false);
            return group;
        }

        // ---------------------------------------------------------------- Prefabs

        /// <summary>
        /// Estructura del jugador:
        ///   Player            ← CharacterController + scripts (la lógica)
        ///    ├─ Visual        ← solo el mesh. Acá va a ir tu modelo 3D.
        ///    └─ CameraRoot    ← altura de los ojos; rota arriba/abajo
        ///        └─ Camera
        /// </summary>
        static GameObject BuildPlayer()
        {
            var root = new GameObject("Player");

            var controller = root.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.35f;
            controller.center = new Vector3(0f, 0.9f, 0f);
            controller.stepOffset = 0.3f;

            root.AddComponent<PlayerInputReader>();
            root.AddComponent<PlayerStamina>();
            root.AddComponent<PlayerInventory>();
            var playerController = root.AddComponent<PlayerController>();
            var look = root.AddComponent<PlayerLook>();
            var interactor = root.AddComponent<PlayerInteractor>();

            var visual = CreateVisual(PrimitiveType.Capsule, root.transform, playerMat);
            visual.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            visual.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
            // En primera persona no queremos ver nuestro propio cuerpo, pero sí su sombra.
            visual.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.ShadowsOnly;

            var cameraRoot = new GameObject("CameraRoot").transform;
            cameraRoot.SetParent(root.transform, false);
            cameraRoot.localPosition = new Vector3(0f, 1.6f, 0f);

            var cameraGo = new GameObject("Camera", typeof(Camera), typeof(AudioListener));
            cameraGo.tag = "MainCamera";
            cameraGo.transform.SetParent(cameraRoot, false);
            var camera = cameraGo.GetComponent<Camera>();
            camera.fieldOfView = 75f;
            camera.nearClipPlane = 0.05f;

            // Equivale a arrastrar los objetos a los campos del Inspector.
            SetReference(playerController, "cameraRoot", cameraRoot);
            SetReference(look, "cameraRoot", cameraRoot);
            SetReference(interactor, "cameraTransform", cameraGo.transform);

            return root;
        }

        static GameObject BuildPickup(string name, ResourceType type, int amount, Material material, float size)
        {
            var root = new GameObject(name);

            var box = root.AddComponent<BoxCollider>();
            box.size = Vector3.one * size;
            box.center = Vector3.up * (size / 2f);

            var pickup = root.AddComponent<ResourcePickup>();
            var so = new SerializedObject(pickup);
            so.FindProperty("resourceType").enumValueIndex = (int)type;
            so.FindProperty("amount").intValue = amount;
            so.ApplyModifiedPropertiesWithoutUndo();

            var visual = CreateVisual(PrimitiveType.Cube, root.transform, material);
            visual.transform.localPosition = Vector3.up * (size / 2f);
            visual.transform.localScale = Vector3.one * size;

            return root;
        }

        /// <summary>Un primitivo SIN collider: es solo la parte visual.</summary>
        static GameObject CreateVisual(PrimitiveType type, Transform parent, Material material)
        {
            var visual = GameObject.CreatePrimitive(type);
            visual.name = "Visual";
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            visual.transform.SetParent(parent, false);
            visual.GetComponent<Renderer>().sharedMaterial = material;
            return visual;
        }

        static GameObject GetOrCreatePrefab(string path, System.Func<GameObject> build)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
                return existing;

            EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            var temp = build();
            var prefab = PrefabUtility.SaveAsPrefabAsset(temp, path);
            Object.DestroyImmediate(temp);
            return prefab;
        }

        static GameObject Spawn(GameObject prefab, Vector3 position, Transform parent)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.SetParent(parent, false);
            instance.transform.position = position;
            return instance;
        }

        static void SetReference(Object target, string propertyName, Object value)
        {
            var so = new SerializedObject(target);
            var property = so.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogError($"No existe el campo '{propertyName}' en {target.GetType().Name}.");
                return;
            }

            property.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------------------------------------------------------------- Assets

        static void CreateMaterials()
        {
            groundMat = GetOrCreateMaterial("Suelo", new Color(0.36f, 0.45f, 0.26f));
            wallMat = GetOrCreateMaterial("Pared", new Color(0.5f, 0.5f, 0.52f));
            obstacleMat = GetOrCreateMaterial("Obstaculo", new Color(0.55f, 0.42f, 0.3f));
            houseMat = GetOrCreateMaterial("Casa", new Color(0.78f, 0.72f, 0.6f));
            playerMat = GetOrCreateMaterial("Jugador", new Color(0.2f, 0.4f, 0.85f));
            materialsMat = GetOrCreateMaterial("Recurso_Materiales", new Color(1f, 0.8f, 0.1f));
            ammoMat = GetOrCreateMaterial("Recurso_Municion", new Color(0.9f, 0.2f, 0.2f));
        }

        static Material GetOrCreateMaterial(string name, Color color)
        {
            string path = $"{MaterialsFolder}/M_Greybox_{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
                return material;

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            material = new Material(shader) { color = color };
            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", 0.15f);

            EnsureFolder(MaterialsFolder);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        static void AddSceneToBuildSettings(string path)
        {
            // Pone esta escena primera y descarta entradas a escenas que ya no existen.
            var scenes = EditorBuildSettings.scenes
                .Where(s => s.path != path && File.Exists(s.path))
                .Prepend(new EditorBuildSettingsScene(path, true))
                .ToArray();
            EditorBuildSettings.scenes = scenes;
        }
    }
}
