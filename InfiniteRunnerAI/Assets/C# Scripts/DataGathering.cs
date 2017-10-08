using System.Text;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class DataGathering : MonoBehaviour
{
    public PlayerController playerController;
    private string[] rowDataTemp;
    public static List<string[]> rowData = new List<string[]>();

    private void Awake()
    {
        rowDataTemp = new string[PlayerController.angles.Length + 1];
        
        for (int i = 0; i < rowDataTemp.Length - 1; i++)
        {
            rowDataTemp[i] = "Ray" + (i + 1).ToString();
        }

        rowDataTemp[rowDataTemp.Length - 1] = "PlayerX";

        rowData.Add(rowDataTemp);
    }

    private void Start()
    {
        if (playerController.training == true)
            StartCoroutine(SaveTrainingData());
    }

    // Save the data to a csv file
    IEnumerator SaveTrainingData()
    {
        yield return new WaitForSeconds(240);

        string[][] output = new string[rowData.Count][];

        for (int i = 0; i < output.Length; i++)
        {
            output[i] = rowData[i];
        }

        int length = output.GetLength(0);
        string delimiter = ",";

        StringBuilder sb = new StringBuilder();

        for (int index = 0; index < length; index++)
            sb.AppendLine(string.Join(delimiter, output[index]));

        string filePath = Application.dataPath + "/Data" + "/data.csv";

        StreamWriter outStream = File.CreateText(filePath);
        outStream.WriteLine(sb);
        outStream.Close();

        Debug.Log("Success");
    }
}
