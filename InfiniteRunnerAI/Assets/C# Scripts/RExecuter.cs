using System.Text;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

// The project was inspiried by Udacity's Self driving car - https://github.com/udacity/self-driving-car
//
// This class can be used to automate all the steps, thus the data will be automatically sent to R,
//      it will construct the model, save the weights of the model and some other important information
//      then the information can be used to navigate in the map
//      
//  The class can be used to do both training and prediction in R, which is very slow
//      because of System.Diagnostics.Process, minimum 0.5 second is needed for connecting to R
//      thus every time the process is started and stopped, the training model is lost, which has to
//      be generated again or saved/loaded which again takes a lot of time if the model is big 
//      (this last part is also true for R.NET).
//
//  Like Udacity used python to make a self driving car in unity the same way this project can work with the use of this class 
//     
//  Notice that the class is not used in the project, it was the initial test to make sure that R and C# work together correctly
public class RExecuter : MonoBehaviour
{
    private System.Diagnostics.ProcessStartInfo start = new System.Diagnostics.ProcessStartInfo();
    private List<string[]> rowData = new List<string[]>();

    private void Awake()
    {
        ProcessInitializer();
    }

    void Start()
    {
        StartCoroutine(MakeTheModel());
        StartCoroutine(PredictOnModel());
    }

    // Make the machine learning model
    IEnumerator MakeTheModel()
    {
        yield return new WaitForSeconds(1);

        SaveTrainingData();

        RunRScript(filename: "simpler.R");
    }

    // After some time, predict on new data
    IEnumerator PredictOnModel()
    {
        yield return new WaitForSeconds(5);

        SaveNewInfo(var1: 72, var2: 0);
        RunRScript(filename: "testr.R");

        string[] prediction = File.ReadAllLines(Application.dataPath + "/Data" + "/prediction.txt", Encoding.UTF8);
        Debug.Log(prediction[0]);
        Debug.Log(prediction.Length);
    }

    // Initializes "ProcessStartInfo" information in order not to repeat the same chunk over and over
    void ProcessInitializer()
    {
        start.FileName = @"C:\Program Files\Microsoft\R Client\R_SERVER\bin\x64\Rscript.exe";

        start.WorkingDirectory = Application.dataPath + "/R Scripts";
        start.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
        start.CreateNoWindow = true;
        start.UseShellExecute = false;
        start.RedirectStandardOutput = true;
        start.RedirectStandardError = true;
        start.RedirectStandardError = true;
    }

    // Runs the "filename" R script and returns the output
    void RunRScript(string filename)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();

        start.Arguments = filename;

        /*
        using (System.Diagnostics.Process process = System.Diagnostics.Process.Start(start))
        {
            using (StreamReader reader = process.StandardOutput)
            {
                //output = reader.ReadLine();
            }

            //process.WaitForExit();
        }
        */

        System.Diagnostics.Process process = new System.Diagnostics.Process();
        process.StartInfo = start;
        process.Start();

        // StreamReader reader = process.StandardOutput;

        // var output = reader.ReadToEnd();
   
        // string err = process.StandardError.ReadToEnd();
        // Debug.Log(err);

        // process.StandardOutput.Close();
        // process.WaitForExit();
        process.Close();

        watch.Stop();
        var elapsedMs = watch.ElapsedMilliseconds;
        Debug.Log(elapsedMs);
    }

    // Saves the data based on which prediction will be done
    void SaveNewInfo(int var1, int var2)
    {
        string info = "";

        info = "c(" + var1 + ", " + var2 + ")";

        StreamWriter file = new StreamWriter(Application.dataPath + "/Data" + "/curr.txt");
        file.WriteLine(info);
        file.Close();
    }

    // Makes a dummy data 
    void SaveTrainingData()
    {
        string[] rowDataTemp = new string[2];
        rowDataTemp[0] = "Strength";
        rowDataTemp[1] = "Won";
        rowData.Add(rowDataTemp);

        for (int i = 0; i < 100000; i++)
        {
            int probability = Random.Range(1, 11);

            rowDataTemp = new string[2];
            rowDataTemp[0] = "" + Random.Range(10, 100);

            if (int.Parse(rowDataTemp[0]) + probability > 70)
            {
                rowDataTemp[1] = "" + (probability > 3 ? 1 : 0);
            }
            else
            {
                rowDataTemp[1] = "" + 0;
            }

            rowData.Add(rowDataTemp);
        }

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
    }
}