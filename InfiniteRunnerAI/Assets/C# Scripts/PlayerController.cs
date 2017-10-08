using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics;
using System;

public class PlayerController : MonoBehaviour
{
    public Text survivalLen;
    private float timer = 0;

    public bool training = true;
    public static bool colliding = false;
    public static float speedY = 4f;
    public static float[] angles = new float[] {-40, -20, -10, 0, 0, 10, 20, 40};
    private float visionLen = 8f;

    private float cooldown = 2f;
    private float dataGatherTimer = 0;
    private float speedX = 5f;

    private double[][,] weights;

    double[] minRays;
    double[] maxRays;

    private void Awake()
    {
        weights = ExtractInfo();

        minRays = File.ReadAllText(Application.dataPath + "/Data/" + "minRays.txt").Split(new char[] { ' ', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(item => double.Parse(item)).ToArray();
        maxRays = File.ReadAllText(Application.dataPath + "/Data/" + "maxRays.txt").Split(new char[] { ' ', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(item => double.Parse(item)).ToArray();
    }

    private void Update()
    {  
        if (training == true)
            Move();
        else
            ApplyPrediction(needRescale: true);

        UpdateTimer();
        //PredictContinually();
    }

    private void Move()
    {
        transform.Translate(Input.GetAxis("Horizontal") * speedX * Time.deltaTime, 0, speedY * Time.deltaTime);

        SendRays();
    }

    // Raycast to detect obstacles with custom weights, lengths and angles
    private string[] DoRaycast(bool needPlayerPos)
    {
        Ray[] ray = new Ray[angles.Length];
        RaycastHit[] raycastHit = new RaycastHit[angles.Length];

        for (int i = 0; i < ray.Length; i++)
        {
            if (i < ray.Length / 2)
            {
                ray[i].origin = transform.position + new Vector3(-0.5f, 0, 0);
            }
            else
            {
                ray[i].origin = transform.position + new Vector3(0.5f, 0, 0);
            }

            ray[i].direction = new Vector3(Mathf.Sin(Mathf.Deg2Rad * angles[i]), 0, Mathf.Cos(Mathf.Deg2Rad * angles[i])); //Quaternion.AngleAxis(angles[i], Vector3.forward) * Vector3.forward;
        }

        string[] rowDataTemp = needPlayerPos == true ? new string[angles.Length + 1] : new string[angles.Length];

        for (int i = 0; i < ray.Length; i++)
        {
            float visionLenCurr = visionLen;

            // The first and last rays must be shorter in order not to constantly collide with the walls 
            if (i == 0 || i == ray.Length - 1)
                visionLenCurr /= 2.5f;

            float rayWeight = 25;

            // Give higher weight to the 2 rays directly in front of the bot because they are the most important ones
            //      for example, by minimizing the ray lengths, the bot will move forward if there is no obstacle if not will 
            //      use the other censors to determine the direction
            if (i == Mathf.Ceil(ray.Length / 2) || i == Mathf.Ceil(ray.Length / 2) - 1)
            {
                rayWeight = 40f;
                visionLenCurr /= 1.15f;
            }

            if (Physics.Raycast(ray[i], out raycastHit[i], visionLenCurr))
            {
                if (raycastHit[i].transform.tag == "Obstacle")
                {
                    Debug.DrawRay(ray[i].origin, ray[i].direction * visionLenCurr, Color.red, 0.02f);
                }

                rowDataTemp[i] = Vector3.Distance(transform.position, raycastHit[i].point) + "";
            }
            else
            {
                Debug.DrawRay(ray[i].origin, ray[i].direction * visionLenCurr, Color.green, 0.02f);

                rowDataTemp[i] = rayWeight.ToString();
            }

            if (needPlayerPos == true)
            {
                //rowDataTemp[angles.Length] = transform.position.x + "";
                rowDataTemp[angles.Length] = Input.GetAxis("Horizontal") + "";
            }
        }

        return rowDataTemp;
    }

    // The frequency of collecting data
    private void SendRays()
    {
        DataGathering.rowData.Add(DoRaycast(needPlayerPos: true));
        /*
        if (dataGatherTimer < cooldown)
        {
            dataGatherTimer += 1;
        }
        else
        {
            
            
            dataGatherTimer = 0;
        }
        */
    }

    // Do string manipulation to convert the data in the form we need
    private double[][,] ExtractInfo()
    {
        string weightsTxt = File.ReadAllText(Application.dataPath + "/Data/" + "weights2.json");
        weightsTxt = weightsTxt.Substring(3, weightsTxt.Length - 8);
        weightsTxt = weightsTxt.Replace("]],[[", "\n").Replace(",", " ").Replace("] [", ";");

        string[] weightsDiv = weightsTxt.Split('\n');
        
        double[][,] simple = new double[weightsDiv.Length][,];

        for (int k = 0; k < weightsDiv.Length; k++)
        {
            string[] rows = weightsDiv[k].Split(';');
            
            int colNum = (weightsDiv[k].Count(item => item == ' ') + rows.Length) / rows.Length;

            simple[k] = new double[rows.Length, colNum];

            for (int i = 0; i < rows.Length; i++)
            {
                string[] cols = rows[i].Split(' ');

                for (int j = 0; j < colNum; j++)
                {
                    simple[k][i, j] = double.Parse(cols[j]);
                }
            }
        }

        return simple;
    }

    // Forward Propagation
    private float PredictPosX(bool needRescale)
    {
        double[][,] weights = ExtractInfo();
        double[] rayData = DoRaycast(needPlayerPos: false).Select(item => double.Parse(item)).ToArray();
        double[,] inputD = new double[rayData.Length + 1, 1];
        inputD[0, 0] = 1.0;

        for (int i = 1; i < rayData.Length + 1; i++)
        {
            if (needRescale == true)
                inputD[i, 0] = (rayData[i - 1] - minRays[i - 1]) / (maxRays[i - 1] - minRays[i - 1]);
            else
                inputD[i, 0] = rayData[i - 1];
        }

        Matrix<double> input = DenseMatrix.OfArray(inputD);
        //input = DenseMatrix.OfArray(new double[,] { { 1 }, { 7.177935 }, { 6.811018 },   { 20 }, { 7.542564 } });

        Matrix<double> theta = DenseMatrix.OfArray(weights[0]);
        Matrix<double> a = ActivationFunc((theta.Transpose() * input), name: "TanH");

        for (int i = 1; i < weights.Length - 1; i++)
        {
            theta = DenseMatrix.OfArray(weights[i]);
            a = ActivationFunc((theta.Transpose() * a.InsertRow(0, DenseVector.OfArray(new double[] { 1 }))), name: "TanH");
        }

        theta = DenseMatrix.OfArray(weights[weights.Length - 1]);
        a = ((theta.Transpose() * a.InsertRow(0, DenseVector.OfArray(new double[] { 1 }))));

        /*
        Matrix<double> input = DenseMatrix.OfArray(inputD);
        Matrix<double> theta1 = DenseMatrix.OfArray(weights[0]);
        Matrix<double> theta2 = DenseMatrix.OfArray(weights[1]);
        Matrix<double> theta3 = DenseMatrix.OfArray(weights[2]);
        Matrix<double> theta4 = DenseMatrix.OfArray(weights[3]);
        
        var a2 = Sigmoid((theta1.Transpose() * input));
        var a3 = Sigmoid((theta2.Transpose() * a2.InsertRow(0, DenseVector.OfArray(new double[] { 1 }))));
        var a4 = Sigmoid((theta3.Transpose() * a3.InsertRow(0, DenseVector.OfArray(new double[] { 1 }))));
        var a5 = ((theta4.Transpose() * a4.InsertRow(0, DenseVector.OfArray(new double[] { 1 }))));
        */
        return (float) a.ToArray()[0, 0];
    }

    // Apply the controls
    private void ApplyPrediction(bool needRescale)
    {
        float posX = PredictPosX(needRescale: true);

        float minX = float.Parse(File.ReadAllText(Application.dataPath + "/Data/" + "minPlX.txt"));
        float maxX = float.Parse(File.ReadAllText(Application.dataPath + "/Data/" + "maxPlX.txt"));

        if (needRescale == true)
            posX = posX * (maxX - minX) + minX;

        float[] rayData = DoRaycast(needPlayerPos: false).Select(item => float.Parse(item)).ToArray();
        
        // Neural Network with the current weights always fails in these two cases, in this case conditions work fine 
        if (rayData[0] <= 25 && rayData[1] < 25 && rayData[2] < 25 && rayData[3] < 40 && rayData[4] < 40 && rayData[5] < 25 && rayData[6] < 25 && rayData[7] < 25)
        {
            transform.Translate(-1 * speedX * Time.deltaTime, 0, speedY * Time.deltaTime);
            Debug.Log("Destroy Humanity");
            //Time.timeScale = 0;
        }
        else if (rayData[0] < 25 && rayData[1] < 25 && rayData[2] < 25 && rayData[3] < 40 && rayData[4] < 40 && rayData[5] < 25 && rayData[6] < 25 && rayData[7] <= 25)
        {
            transform.Translate(1 * speedX * Time.deltaTime, 0, speedY * Time.deltaTime);
            Debug.Log("Don't destroy Humanity");
            //Time.timeScale = 0;
        } 
        else
        {
            transform.Translate(posX * speedX * Time.deltaTime, 0, speedY * Time.deltaTime);
        }
        
        if (Mathf.Abs(transform.position.x) > 4.4f)
            transform.position = new Vector3(Mathf.Sign(transform.position.x) * 4.4f, transform.position.y, transform.position.z);
        
    }

    // The activation function for the forward propagation
    private Matrix<double> ActivationFunc(Matrix<double> x, string name)
    {
        if (name == "Sigmoid")
            return 1 / (1 + Matrix.Exp(-x));
        else if (name == "TanH")
            return Matrix.Tanh(x);
        else
            return DenseMatrix.OfArray(new double[,] { });
    }

    private void UpdateTimer()
    {
        int minutes = (int) Mathf.Floor(timer / 60);
        int seconds = (int) timer - minutes * 60;

        string min = minutes < 10 ? "0" + minutes.ToString() : minutes.ToString();
        string sec = seconds < 10 ? "0" + seconds.ToString() : seconds.ToString();

        survivalLen.text = min + " : " + sec;
        timer += 1 * Time.deltaTime;

        if (transform.position.y < 0.9)
            Time.timeScale = 0;
    }

    /*
     * R.NET version which is very slow
     * 
    private void Predict()
    {
        var watch = new System.Diagnostics.Stopwatch();
        watch.Start();

        var currDir = Directory.GetCurrentDirectory();

        REngine.SetEnvironmentVariables();
        REngine engine = REngine.GetInstance();

        engine.Evaluate("setwd('D:/Vahagn/Programming/Unity/Projects/Infinite Runner/Assets/Data')");

        engine.Evaluate("library(randomForest)");

        engine.Evaluate("data <- read.csv(file = 'data.csv', header = TRUE, sep = ',')");
        engine.Evaluate("index <- sample(1:nrow(data),round(0.75*nrow(data)))");
        engine.Evaluate("train <- data[index,]");
        engine.Evaluate("test <- data[-index,]");

        engine.Evaluate("load('model.rf')");
        engine.Evaluate("pred.rf <- predict(model.rf, test[, 1:9])");

        Debug.Log(engine.Evaluate("pred.rf[[25]]").AsNumeric().First());

        
        //engine.Evaluate("data <- read.csv(file = 'data.csv', header = TRUE, sep = ',')");
        //engine.Evaluate("index <- sample(1:nrow(data),round(0.75*nrow(data)))");
        //engine.Evaluate("train <- data[index,]");
        //engine.Evaluate("test <- data[-index,]");

        //engine.Evaluate("library(randomForest)");

        //engine.Evaluate("model.rf <- randomForest(PlayerX ~ Ray1 + Ray2 + Ray3 + Ray4 + Ray5 + Ray6 + Ray7 + Ray8 + Ray9, data = train)");

        //engine.Evaluate("pred.rf <- predict(model.rf, test[, 1:9])");
        //engine.Evaluate("MSE.rf <- mean((test$PlayerX - pred.rf)^2)");

        //Debug.Log(string.Join(" ,", engine.Evaluate("pred.rf[[25]]").AsNumeric().First()));

        //engine.Dispose();

        Directory.SetCurrentDirectory(currDir);
    }

    private void PredictContinually()
    {
        //NumericVector numVec = engine.CreateNumericVector(new double[] { 30.02, 29.99, 30.11, 29.97, 30.01, 29.99 });
        CreateFile();

        REngine.SetEnvironmentVariables();
        REngine engine = REngine.GetInstance();

        engine.Evaluate("setwd('D:/Vahagn/Programming/Unity/Projects/Infinite Runner/Assets/Data')");

        //engine.Evaluate("library(randomForest)");
        //engine.Evaluate("load('model.rf')");
        engine.Evaluate("library(neuralnet)");
        engine.Evaluate("load('model.nn')");

        engine.Evaluate("test <- read.csv(file = 'test.csv', header = TRUE, sep = ',')");

        engine.Evaluate("load('maxs')");
        engine.Evaluate("load('mins')");

        engine.Evaluate("scaled <- as.data.frame(scale(test, center = mins, scale = maxs - mins))");
        engine.Evaluate("pred.nn_ <- compute(model.nn, scaled[1:9])");
        engine.Evaluate("pred.nn <- pred.nn_$net.result * (maxs - mins) + mins");
        transform.position = new Vector3((float)engine.Evaluate("pred.nn").AsNumeric().First(), transform.position.y, transform.position.z);
        //transform.position = new Vector3((float) engine.Evaluate("predict(model.rf, test[, 1:9])").AsNumeric().First(), transform.position.y, transform.position.z);

        //Debug.Log(engine.Evaluate("predict(model.rf, test[, 1:9])").AsNumeric().First());

        //engine.Dispose();
    }

    private void CreateFile()
    {
        string[] temp = new string[10];
        List<string[]> data = new List<string[]>();

        temp[0] = "Ray1";
        temp[1] = "Ray2";
        temp[2] = "Ray3";
        temp[3] = "Ray4";
        temp[4] = "Ray5";
        temp[5] = "Ray6";
        temp[6] = "Ray7";
        temp[7] = "Ray8";
        temp[8] = "Ray9";
        temp[9] = "PlayerX";

        data.Add(temp);

        // Add the data
        float[] angles = new float[] { -28, -21, -14, -7, 0, 7, 14, 21, 28 };
        Ray[] ray = new Ray[9];
        RaycastHit[] raycastHit = new RaycastHit[9];

        for (int i = 0; i < ray.Length; i++)
        {
            ray[i].origin = transform.position;
            ray[i].direction = new Vector3(Mathf.Sin(Mathf.Deg2Rad * angles[i]), 0, Mathf.Cos(Mathf.Deg2Rad * angles[i])); //Quaternion.AngleAxis(angles[i], Vector3.forward) * Vector3.forward;
        }

        string[] rowDataTemp = new string[10];

        for (int i = 0; i < ray.Length; i++)
        {
            if (Physics.Raycast(ray[i], out raycastHit[i], 15f))
            {
                if (raycastHit[i].transform.tag == "Obstacle")
                {
                    Debug.DrawRay(transform.position, ray[i].direction * 15, Color.green, 0.1f);
                }

                rowDataTemp[i] = Vector3.Distance(transform.position, raycastHit[i].transform.position) + "";
            }
            else
            {
                rowDataTemp[i] = "20";
            }

            rowDataTemp[9] = transform.position.x + "";
        }

        data.Add(rowDataTemp);

        string[][] output = new string[data.Count][];

        for (int i = 0; i < output.Length; i++)
        {
            output[i] = data[i];
        }

        int length = output.GetLength(0);
        string delimiter = ",";

        StringBuilder sb = new StringBuilder();

        for (int index = 0; index < length; index++)
            sb.AppendLine(string.Join(delimiter, output[index]));

        string filePath = Application.dataPath + "/Data" + "/test.csv";

        StreamWriter outStream = File.CreateText(filePath);
        outStream.WriteLine(sb);
        outStream.Close();
    }
    */
}