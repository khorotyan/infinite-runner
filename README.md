# README #

Infinite Runner is an application which uses C# and R programming languages to create an artificially intelligent bot. 
The player trains the bot by playing the game by moving left or right to avoid the obstacles. The data is collected and saved to a csv file. 
The file is then loaded into R, some processing is done, and a neural network model is created based on the data. 
The weights of the NN is then saved and loaded into C# and based on it Forward Propagation is applied and the bot predicts whether to go left or to right.
A neural network with one hidden layer with 5 nodes made it possible for the bot to survive over 50 minutes. 