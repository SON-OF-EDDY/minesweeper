using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class MainPageLogic : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void GoToPlayScene()
    {
        SceneManager.LoadScene("SampleScene");
    }

    public void EndGame()
    {
        Debug.Log("Closing the game now...");
        Application.Quit();
    }


}
