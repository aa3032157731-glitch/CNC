using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class Play_home : MonoBehaviour, IPointerClickHandler
{
    public GameObject obj;
    public void OnPointerClick(PointerEventData eventData)
    {
        SceneManager.LoadScene(1);
        obj.SetActive(false);
    }
}
//LoadSceneMode.AdditiveºÏ²¢³¡¾°