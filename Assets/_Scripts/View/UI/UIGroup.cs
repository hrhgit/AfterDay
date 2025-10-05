using System.Collections.Generic;
using UnityEngine;

public class UIGroup : MonoBehaviour
{
    public List<GameObject> UIElements = new List<GameObject>();
    public GameObject UIElementPrefab;

    public void Show()
    {
        foreach (var gameObject in UIElements)
        {
            gameObject.SetActive(false);
        }
        UIElementPrefab.SetActive(true);
    }
    
}
