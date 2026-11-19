using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class Play_home : MonoBehaviour, IPointerClickHandler
{
    public GameObject obj;

    public void OnPointerClick(PointerEventData eventData)
    {
        SceneManager.LoadScene(1);
        if (obj != null) obj.SetActive(false);
    }
}
