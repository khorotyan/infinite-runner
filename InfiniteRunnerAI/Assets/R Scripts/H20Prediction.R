library(h2o)

setwd('D:/Vahagn/Programming/Unity/Projects/Infinite Runner/Assets/Data')

### Import data and scale it

data <- read.csv(file = 'data.csv', header = TRUE, sep = ',')
data <- data[!duplicated(data), ] 
# write.csv(data,'data.csv')

index <- sample(1:nrow(data),round(0.75*nrow(data)))
# index <- 1:round(0.75*nrow(data))
train <- data[index,]
test <- data[-index,]

maxs <- apply(data, 2, max, na.rm = T) 
mins <- apply(data, 2, min, na.rm = T)

minP = min(test$PlayerX)
maxP = max(test$PlayerX)

write(minP, file = 'minPlX.txt')
write(maxP, file = 'maxPlX.txt')

write(mins, file = 'minRays.txt')
write(maxs, file = 'maxRays.txt')

scaled <- as.data.frame(scale(data, center = mins, scale = maxs - mins))

train_ <- scaled[index, ]
test_ <- scaled[-index, ]

### Initialize , train and predict with H2o

localH2O <- h2o.init(nthreads = -1)
train.h2o <- as.h2o(train_)
test.h2o <- as.h2o(test_)

y.dep <- dim(data)[2]
x.indep <- 1:(dim(data)[2] - 1)

# regression.model <- h2o.glm( y = y.dep, x = x.indep, training_frame = train.h2o, family = "gaussian")
# h2o.performance(regression.model)
# predict.reg <- as.data.frame(h2o.predict(regression.model, test.h2o))
# mean((test$PlayerX - predict.reg$predict)^2)

dlearning.model <- h2o.deeplearning(y = y.dep,
                                    x = x.indep,
                                    training_frame = train.h2o,
                                    epochs = 200,
                                    nfolds = 0,
                                    hidden = c(5),
                                    l2 = 0.00001,
                                    activation = "Tanh",
                                    standardize = FALSE,
                                    seed = 1122, export_weights_and_biases=T)

h2o.performance(dlearning.model)
predict.dl2 <- as.data.frame(h2o.predict(dlearning.model, test.h2o))
predict.dl2 <- predict.dl2 * (max(test$PlayerX) - min(test$PlayerX)) + min(test$PlayerX)

mean((test$PlayerX - predict.dl2$predict)^2)

### Save the weights and visualize predictions

weights.nn2 <- list()
for(i in 1:(length(dlearning.model@parameters$hidden) + 1)){
  weights.nn2[[i]] <- t(cbind(as.data.frame(h2o.biases(dlearning.model, vector_id = i)), as.data.frame(h2o.weights(dlearning.model, matrix_id = i))))
  colnames(weights.nn2[[i]]) <- NULL
}

require(rlist)
list.save(weights.nn2, file = 'weights2.json')

ggplot(environment = .e) + 
  geom_point(data = test, aes(x = 1:dim(test)[1], y = PlayerX, colour = 'Player')) + 
  geom_point(aes(x = 1:length(predict.dl2$predict), y = predict.dl2$predict, colour = 'Bot'))

h2o.shutdown()

