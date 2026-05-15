using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class SceneController : MonoBehaviour
{
    public GameObject prefab;
    public int addObjectsCount = 1000;

    public Text infoText;

    public Text objectsCountText;

    public Button addButton;
    public Button removeButton;
    public Button switchSceneButton;

    private readonly List<GameObject> _objects = new (50_000);

    private void Start()
    {
        infoText.text = $"Scene: {SceneManager.GetActiveScene().name}\n";

        addButton.onClick.AddListener(AddObjects);
        removeButton.onClick.AddListener(RemoveObjects);
        switchSceneButton.onClick.AddListener(SwitchScene);

        AddObjects();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
    }

    private void OnDestroy()
    {
        addButton.onClick.RemoveAllListeners();
        removeButton.onClick.RemoveAllListeners();
        switchSceneButton.onClick.RemoveAllListeners();
    }

    private void RemoveObjects()
    {
        for (int i = 0; i < addObjectsCount; i++)
        {
            if (_objects.Count == 0)
                return;

            var obj = _objects[^1];
            _objects.Remove(obj);
            Destroy(obj);
        }

        objectsCountText.text = $"Objects count: {_objects.Count.ToString()}";
    }

    private void AddObjects()
    {
        for (int i = 0; i < addObjectsCount; i++)
        {
            var x = _objects.Count / 100;
            var y = _objects.Count % 100;
            _objects.Add(Instantiate(prefab, new Vector3(x, 0, y), Quaternion.identity));
        }

        objectsCountText.text = $"Objects count: {_objects.Count.ToString()}";
    }

    private void SwitchScene()
    {
        foreach (var obj in _objects)
            Destroy(obj);

        _objects.Clear();

        int index = SceneManager.GetActiveScene().buildIndex == 0 ? 1 : 0;
        SceneManager.LoadScene(index);
    }
}
