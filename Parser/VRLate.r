#Copyright 2017 Technische Hochschule Ingolstadt
#
#Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"),
#to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, 
#and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions: 
#
#The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
#
#THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
#FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
#LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS 
#IN THE SOFTWARE.

#dev.off()
#library(tikzDevice)
poti = read.csv("C:/Users/Becher/Desktop/OutputLatency/TrackingInput.csv", header=TRUE, sep="\t")
photo = read.csv("C:/Users/Becher/Desktop/OutputLatency/MonitorOutput.csv", header=TRUE, sep="\t")
#pdf('Latency.pdf')
#tikz('Latency.tex',width = 3.25, height = 3.25)


# if value = -1 -> HMD went black. Therefore take old intersection point  
lastReading = -1
leadingZeros = 0
lastVal = 0
for (i in 1:length(poti$Interval))
{
  if(photo$Value[i]>0){
    lastReading = photo$Value[i]
  } else {
    photo$Value[i] = lastReading
    if(lastReading < 0){
      leadingZeros = i 
    }
  }
}

#Crosscorrelate data
Find_Abs_Max_CCF<- function(a,b)
{
  d <- ccf(a, b, plot = FALSE, lag.max = length(a)-5)
  cor = d$acf[,,1]
  abscor = abs(d$acf[,,1])
  lag = d$lag[,,1]
  res = data.frame(cor,lag)
  absres = data.frame(abscor,lag)
  absres_max = res[which.max(absres$abscor),]
  return(absres_max)
}
latency = Find_Abs_Max_CCF(photo$Value[-c(0:leadingZeros)],poti$Value[-c(0:leadingZeros)])

# Draw Plots
par(mfrow=c(3,1))
plot(poti$Interval[-c(0:leadingZeros)],poti$Value[-c(0:leadingZeros)], xlim=c(0,5000),  col = "blue",xlab = "Time (ms)",ylab = "Rotation", main ="Potentiometer")
plot(photo$Interval[-c(0:leadingZeros)],photo$Value[-c(0:leadingZeros)], xlim=c(0,5000), col = "red",ylab = "Rotation",xlab = "Time (ms)", main ="Photodiode")
ccf(photo$Value[-c(0:leadingZeros)],poti$Value[-c(0:leadingZeros)], lag.max = 100, lag.min = -100,ylab = "cross-correlation",main = "Cross-Correlation(photo,poti)")
title(main=paste("Latency: ",latency[2]," ms"), adj=1)