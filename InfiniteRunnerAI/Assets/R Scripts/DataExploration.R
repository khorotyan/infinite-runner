ggplot(data = data[1:200, ]) +
  geom_point(aes(x = Ray1, y = PlayerX, colour = 'Ray1')) +
  geom_point(aes(x = Ray2, y = PlayerX, colour = 'Ray2')) +
  geom_point(aes(x = Ray3, y = PlayerX, colour = 'Ray3')) +
  geom_point(aes(x = Ray4, y = PlayerX, colour = 'Ray4'))

# Motion Planning
# I must have a physical model