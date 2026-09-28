using TMPro;
using UnityEngine;

namespace Legacy2048
{
public class UI_Text : MonoBehaviour
{
    public TextMeshProUGUI LevelText ;
    public static UI_Text Instance { get; private set; }
    private void Awake()
    {
       if (Instance == null)
           Instance = this;
       else
             Destroy(gameObject);
    }
    public void updateLevelText(string text) 
    {
        LevelText.text = "LEVEL " + text;

    }

}
}
