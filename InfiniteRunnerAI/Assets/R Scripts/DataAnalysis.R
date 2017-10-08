require(ggplot2)

.e = environment()

ggplot() + geom_point(data = data, aes(x = Ray3Len, y = Ray4Len))

# Regression
# model.lm <- lm(PlayerX ~ polym(Ray1, Ray2, Ray3, Ray4, Ray5, Ray6, Ray7, Ray8, Ray9, degree=2, raw=TRUE), data = train)
model.lm <- lm(PlayerX ~ poly(Ray1, 4) + poly(Ray2, 4) + poly(Ray3, 4) + poly(Ray4, 4) + poly(Ray5, 4) + poly(Ray6, 4) + poly(Ray7, 4) + poly(Ray8, 4) + poly(Ray9, 4), data = train)
pred.lm <- predict(model.lm, newdata = test, type = 'response')

MSE.lm <- sum((test$PlayerX - pred.lm)^2) / nrow(test)

ggplot(environment = .e) + 
  geom_point(data = test, aes(x = 1:dim(test)[1], y = PlayerX, colour = 'Player')) + 
  geom_point(aes(x = 1:length(pred.lm), y = pred.lm, colour = 'Bot'))

# Neural Network
library(neuralnet)

maxs <- apply(data, 2, max, na.rm = T) 
mins <- apply(data, 2, min, na.rm = T)

scaled <- as.data.frame(scale(data, center = mins, scale = maxs - mins))

train_ <- scaled[index, ]
test_ <- scaled[-index, ]

###
library('foreach')
library('doParallel')
library('parallel')
clust <- makeCluster(6)  
registerDoParallel(clust) 
stopCluster(clust)  

# 8 , 6 , 4
kf <- neuralnet(PlayerX ~ Ray1 + Ray2 + Ray3 + Ray4, data = train_, hidden=c(6, 4, 2), linear.output = T, lifesign = 'full', threshold = 1.015, lifesign.step = 2000)

kf.pp_ <- compute(kf, test_[, 1:(dim(data)[2] - 1)])

minP = min(test$PlayerX)
maxP = max(test$PlayerX)

write(minP, file = 'minPlX.txt')
write(maxP, file = 'maxPlX.txt')

write(mins, file = 'minRays.txt')
write(maxs, file = 'maxRays.txt')

kf.pp <- kf.pp_$net.result * (max(test$PlayerX) - min(test$PlayerX)) + min(test$PlayerX)

mean((test$PlayerX - kf.pp)^2)

weights.nn <- kf$weights[[1]]

require(rlist)
list.save(weights.nn, file = 'weights.json')
###

ggplot(environment = .e) + 
  geom_point(data = test, aes(x = 1:dim(test)[1], y = PlayerX, colour = 'Player')) + 
  geom_point(aes(x = 1:length(kf.pp), y = kf.pp, colour = 'Bot'))


# Random Forest
library(randomForest)

# model.rf <- randomForest(PlayerX ~ Ray1 + Ray2 + Ray3 + Ray4 + Ray5 + Ray6 + Ray7 + Ray8 + Ray9, data = train)
model.rf <- randomForest(PlayerX ~ Ray1, data = train[1:100, ], ntree = 10)
# model.rf[c(1, 3:6, 8:12, 14:17)] <- NULL
# model.rf$forest[c(1)] <- NULL

pred.rf <- predict(model.rf, test[, 1:9])
MSE.rf <- mean((test$PlayerX - pred.rf)^2)

pred.rf2 <- customPredict(model.rf, test[, 1:9])
MSE.rf2 <- mean((test$PlayerX - pred.rf2)^2)

test$PlayerX[25]
pred.rf[[25]]

ggplot(environment = .e) + 
  geom_point(data = test, aes(x = 1:dim(test)[1], y = PlayerX, colour = 'Player')) + 
  geom_point(aes(x = 1:length(pred.rf), y = pred.rf, colour = 'Bot'))

print(paste(MSE.lm, MSE.nn, MSE.rf))

randomForest:::predict.randomForest # Explore the function

# Decision Trees
library(rpart)
library(rattle)

# Instead of calling each machine learning model, we can call 'train' with the 'method' specified 
#   examples of methods can be found here'http://topepo.github.io/caret/train-models-by-tag.html'

model.dt <- rpart(PlayerX ~ Ray1, data = train[1:300, ])
pred.dt <- predict(model.dt, newdata = test)
MSE.dt <- mean((test$PlayerX - pred.dt)^2)

ggplot(environment = .e) + 
  geom_point(data = test, aes(x = 1:dim(test)[1], y = PlayerX, colour = 'Player')) + 
  geom_point(aes(x = 1:length(pred.dt), y = pred.dt, colour = 'Bot'))

# Multicore Neuralnet
library(caret)

tc <- trainControl(method="boot",number=25)
tcc <- train(PlayerX ~ .,data=train_,method="nnet",trControl=tc)
tcc$finalModel
