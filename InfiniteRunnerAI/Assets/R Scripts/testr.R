# ptm <- proc.time()

setwd('D:/Vahagn/Programming/Unity/Projects/Using R in Unity/Assets/Data')

load("model.txt")

pred_sample <- dget("curr.txt")

newdata = data.frame(matrix(c(pred_sample[1], pred_sample[2]),  nrow = 1))
newdata$X1 = as.integer(newdata$X1)
newdata$X2 = factor(newdata$X2, levels = c(0, 1))
colnames(newdata) <- c('Strength', 'Won')

prediction <- predict(model, newdata = newdata, type = 'response')

dput(prediction[[1]], file = "prediction.txt")

# proc.time() - ptm