using UnityEngine;
using UnityEngine.SceneManagement;

namespace Legacy2048
{
public class MenuManger : MonoBehaviour
{
    public GameObject settingsPanel;
    public GameObject infoPanel;
    public void Play() 
    {
        SceneManager.LoadScene(1);

    }

    public void InfoButton() 
    {
        infoPanel.SetActive(true);

    }
    public void InfoClose() 
    {
        infoPanel.SetActive(false);

    }

    public void SettingsButton() 
    {
        settingsPanel.SetActive(true);
       
    }
    public void SettingsClose()
    {
        settingsPanel.SetActive(false);


    }
}
}
