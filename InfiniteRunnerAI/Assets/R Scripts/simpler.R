library(randomForest)

setwd('D:/Vahagn/Programming/Unity/Projects/Infinite Runner/Assets/Data')

data <- read.csv(file = 'data.csv', header = TRUE, sep = ',')
data <- data[!duplicated(data), ] 
# write.csv(data,'data.csv')

index <- sample(1:nrow(data),round(0.75*nrow(data)))
# index <- 1:round(0.75*nrow(data))
train <- data[index,]
test <- data[-index,]

model.rf <- randomForest(PlayerX ~ Ray1 + Ray2 + Ray3 + Ray4 + Ray5 + Ray6 + Ray7 + Ray8 + Ray9, data = train)

model.rf[c(1, 3:6, 8:12, 14:17)] <- NULL
model.rf$forest[c(1)] <- NULL

save(model.rf, file = 'model.rf')

#pred.rf <- predict(model.rf, test[, 1:9])
#MSE.rf <- mean((test$PlayerX - pred.rf)^2)