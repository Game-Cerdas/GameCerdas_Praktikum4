using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneSwitchButton : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField]
    private string aStarSceneName = "P04_AStarGrid";

    [SerializeField]
    private string navMeshSceneName = "P04_NavMesh";

    [Header("References")]
    [SerializeField]
    private Text buttonLabel;

    private string targetSceneName;

    private void Start()
    {
        UpdateLabelForCurrentScene();
    }

    private void UpdateLabelForCurrentScene()
    {
        string currentScene = SceneManager.GetActiveScene().name;

        targetSceneName = currentScene == aStarSceneName
            ? navMeshSceneName
            : aStarSceneName;

        if (buttonLabel != null)
        {
            buttonLabel.text = "Ke " + targetSceneName;
        }
    }

    public void SwitchScene()
    {
        SceneManager.LoadScene(targetSceneName);
    }
}
