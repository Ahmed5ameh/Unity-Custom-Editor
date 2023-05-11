using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

//CustomEditor(typeof(ObjectSpawner))]
public class TaskEditor : EditorWindow
{
    private SerializedObject _serializedObjectSpawner;
    [SerializeField] private VisualTreeAsset taskTree;
    
    //Visual Elements
    private Button _spawnButton;
    private Button _deleteButton;
    private Button _scaleButton;
    private VisualElement _mainVe;
    private VisualElement _spawnVe;
    private VisualElement _deleteVe;
    private VisualElement _scaleVe;
    
    private ObjectField _prefabOf;

    private GameObject _prefab;
    private FloatField _stepFf;
    private float _scaleValue;
    private Vector2 _minMaxScale = new Vector2(0.25f, 64);
    private Stack<int> _historyStack = new();


    [MenuItem("ObjectSpawner/ObjectSpawnerWindow")]
    public static void ShowWindow()
    {
        var window = GetWindow<TaskEditor>();
        window.titleContent = new GUIContent("Object Spawner");
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneUI;
    }

    private void CreateGUI()
    {
        taskTree.CloneTree(rootVisualElement);
        InitFields();
    }
    
    void InitFields()
    {
        //Buttons
        _spawnButton = rootVisualElement.Q<Button>("SpawnBTN");
        _deleteButton = rootVisualElement.Q<Button>("DeleteBTN");
        _scaleButton = rootVisualElement.Q<Button>("ScaleBTN");

        //Visual Elements
        _mainVe = rootVisualElement.Q<VisualElement>("MainVE");
        _spawnVe = rootVisualElement.Q<VisualElement>("SpawnVE");
        _deleteVe = rootVisualElement.Q<VisualElement>("DeleteVE");
        _scaleVe = rootVisualElement.Q<VisualElement>("ScaleVE");
        
        //Object fields
        _prefabOf = rootVisualElement.Q<ObjectField>("OF_playerPrefab");
        
        //Float fields
        _stepFf = rootVisualElement.Q<FloatField>("StepFF");

        //--------------------------------------------------------------------//
        
        //Init default display for all VEs
        _spawnVe.style.display = DisplayStyle.Flex;
        _deleteVe.style.display = DisplayStyle.None;
        _scaleVe.style.display = DisplayStyle.None;
        _historyStack.Push(1);

        //Assign event action delegates to corresponding buttons
        _spawnButton.clicked += () => OpenSpawnVe();
        _deleteButton.clicked += () => OpenDeleteVe();
        _scaleButton.clicked += () => OpenScaleVe();

        _prefabOf.RegisterValueChangedCallback(evt =>
        {
            _prefab = evt.newValue as GameObject;
        });

        _stepFf.RegisterValueChangedCallback(evt =>
        {
            _scaleValue = _stepFf.value;
        });
    }

    void OpenSpawnVe()
    {
        _spawnVe.style.display = DisplayStyle.Flex;
        _deleteVe.style.display = DisplayStyle.None;
        _scaleVe.style.display = DisplayStyle.None;
        if (_historyStack.Peek() != 1)
        {
            _historyStack.Push(1);   
        }
        //Debug.Log(_historyStack.Peek());
    }
    
    void OpenDeleteVe()
    {
        _spawnVe.style.display = DisplayStyle.None;
        _deleteVe.style.display = DisplayStyle.Flex;
        _scaleVe.style.display = DisplayStyle.None;
        if (_historyStack.Peek() != 2)
        {
            _historyStack.Push(2);   
        }
        //Debug.Log(_historyStack.Peek());
    }
    
    void OpenScaleVe()
    {
        _spawnVe.style.display = DisplayStyle.None;
        _deleteVe.style.display = DisplayStyle.None;
        _scaleVe.style.display = DisplayStyle.Flex;
        if (_historyStack.Peek() != 3)
        {
            _historyStack.Push(3);
        }
        //Debug.Log(_historyStack.Peek());
    }
    
    private GameObject InstantiateMyPrefab(Vector3 position)
    {
        var obj = PrefabUtility.InstantiatePrefab(_prefab) as GameObject;
        if (obj != null)
        {
            obj.transform.position = position;
        }
        return obj;
    }
    
    private void OnSceneUI(SceneView sceneView)
    {
        /*Spawning a prefab*/
        if (_spawnVe.style.display == DisplayStyle.Flex 
            && Event.current.type == EventType.MouseDown && Event.current.button == 0)
        {
            var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            Physics.Raycast(ray, out var raycastHit, Mathf.Infinity, LayerMask.GetMask("Ground"));
            if (raycastHit.collider)
            {
                InstantiateMyPrefab(raycastHit.point);
            }
        }
        
        /*Deleting a prefab*/
        if (_deleteVe.style.display == DisplayStyle.Flex 
            && Event.current.type == EventType.MouseDown && Event.current.button == 0)
        {
            var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            Physics.Raycast(ray, out var raycastHit, Mathf.Infinity, LayerMask.GetMask("Objects"));
            if (raycastHit.collider)
            {
                DestroyImmediate(raycastHit.collider.gameObject);
            }
        }
        
        /* Scaling a prefab */
        if (_scaleVe.style.display == DisplayStyle.Flex)
        {
            var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            Physics.Raycast(ray, out var raycastHit, Mathf.Infinity, LayerMask.GetMask("Objects"));
            
            if (raycastHit.collider)
            {
                //Scale down => RMB
                if (/*Event.current.control &&*/ (Event.current.type == EventType.MouseDown && Event.current.button == 1))
                {
                    if (raycastHit.collider.gameObject.transform.localScale.x > _minMaxScale.x)
                    {
                        raycastHit.collider.gameObject.transform.localScale /= _scaleValue;
                    }
                }
                
                //Scale up => LMB
                else if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && 
                         raycastHit.collider.gameObject.transform.localScale.x < _minMaxScale.y)
                {
                    raycastHit.collider.gameObject.transform.localScale *= _scaleValue;
                }
            }
        }
        
        //Holding down control swaps to delete mode
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.LeftControl /*&& _deleteVe.style.display != DisplayStyle.Flex*/)
        {
            Debug.Log("CTRL PRESSED DOWN, You are now in the delete mode, click on any object to delete it");
            OpenDeleteVe();
        }
        
        if (Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.LeftControl)
        {
            Debug.Log("CTRL PRESSED UP, you are back to your previous tab");
            _historyStack.Pop();
            switch (_historyStack.Peek())
            {
                case 1:
                    OpenSpawnVe();
                    break;
                case 2:
                    OpenDeleteVe();
                    break;
                case 3:
                    OpenScaleVe();
                    break;
            }
        }
    }
}