using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public string sceneToLoadName;
    
    public void LoadLevel(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public async Task LoadLevelAsync(string sceneName)
    {
        await SceneManager.LoadSceneAsync(sceneName);
    }


    public void LoadSceneAsyncButton()
    {
        SceneManager.LoadSceneAsync(sceneToLoadName);
    }
}