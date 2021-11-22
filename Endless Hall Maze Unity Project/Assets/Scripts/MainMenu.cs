using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;


public class MainMenu : MonoBehaviour
{
    // TODO OPTIONS
    // https://stackoverflow.com/questions/32306704/how-to-pass-data-between-scenes-in-unity

    public TMP_InputField mazeSizeInput;
    public TMP_InputField nbOfColorsInput;
    public TMP_InputField seedInput;

    public void PlayGame()
    {
        SaveSettings();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void QuitGame()
    {
        Debug.Log("QUIT!");
        Application.Quit();
    }


    private void SaveSettings()
    {
        int mazeSize = -1;
        int nbOfColors = -1;
        int seed = -1;

        BinaryFormatter bf = new BinaryFormatter();
        FileStream file = File.Create(Application.persistentDataPath + "/playerInfo.dat");

        if (mazeSizeInput.text != "")
            mazeSize = int.Parse(mazeSizeInput.text);
        if (nbOfColorsInput.text != "")
            nbOfColors = int.Parse(nbOfColorsInput.text);
        if (seedInput.text != "")
            seed = int.Parse(seedInput.text);

        GameSettings gp = new GameSettings(mazeSize, nbOfColors, seed);
        bf.Serialize(file, gp);
        file.Close();

        // Source
        // https://answers.unity.com/questions/1457625/saving-data-for-a-class.html
    }

}
