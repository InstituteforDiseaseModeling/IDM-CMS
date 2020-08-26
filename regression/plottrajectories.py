# documentation

import argparse
import csv
import math
import matplotlib.pyplot as pyplot
import numpy
import string
import sys

def ByteToHex(byte):
    ones  = string.hexdigits[byte % 16]
    hexes = string.hexdigits[(byte >> 4) % 16]
    return string.upper(hexes + ones)

def GetColors(count):
    colors = []
    for i in range(count):
        hue = i * 360.0 / count
        sat = 1.0
        val = 1.0
        # http://en.wikipedia.org/wiki/HSL_and_HSV#From_HSV
        chroma = val * sat
        hPrime = hue / 60.0
        x      = chroma * (1.0 - abs((hPrime % 2.0) - 1))
        if (hPrime < 1): # 0<= hPrime < 1
            R1 = chroma
            G1 = x
            B1 = 0
        elif (hPrime < 2): # 1 <= hPrime < 2
            R1 = x
            G1 = chroma
            B1 = 0
        elif (hPrime < 3): # 2 <= hPrime < 3
            R1 = 0
            G1 = chroma
            B1 = x
        elif (hPrime < 4): # 3 <= hPrime < 4
            R1 = 0
            G1 = x
            B1 = chroma
        elif (hPrime < 5): #4 <= hPrime < 5
            R1 = x
            G1 = 0
            B1 = chroma
        else: # 5 <= hPrime < 6
            R1 = chroma
            G1 = 0
            B1 = x
        colors.append((R1, G1, B1))
        
    return colors

def PlotFile(fileName, overlays, plotAllTrajectories):
    print(f"Plotting {fileName}")

    observables = LoadFile(fileName)
    colors      = GetColors(len(observables))

    if plotAllTrajectories:
        obsIndex = 0
        for observable in observables:
            a = numpy.array(observables[observable])
            r, g, b = colors[obsIndex]
            for row in a:
                pyplot.plot(row, color=(r, g, b, 0.2))
            obsIndex = obsIndex + 1

    obsIndex = 0
    for observable in observables:
        a = numpy.array(observables[observable])
        m = numpy.mean(a, axis=0)
        s = numpy.std(a, axis=0)
        pyplot.plot(m, color=colors[obsIndex], linewidth=2.0, label=observable)
        pyplot.plot(m+s, ':', color=colors[obsIndex])
        pyplot.plot(m-s, ':', color=colors[obsIndex])
        obsIndex = obsIndex + 1

    for overlay in overlays:
        for trajectory in overlay:
            a = numpy.array(overlay[trajectory])
            m = numpy.mean(a, axis=0)
            pyplot.plot(m, 'b--')

    pyplot.title(fileName)
    pyplot.legend()

def LoadFile(fileName):
    csvReader = csv.reader(open(fileName, 'rt'))
    currentObservable = ''
    observables = {}
    data = []
    for row in csvReader:
        if 'FrameworkVersion' not in row[0] and "sampletimes" not in row[0]:
            observable = row[0].partition('{')[0]
            if observable != currentObservable:
                currentObservable = observable
                data = []
                observables[observable] = data
            data.append(list(map(float, row[1:])))
    return observables

if __name__ == '__main__':

    parser = argparse.ArgumentParser()
    parser.add_argument('-o', '--overlay', action='append')
    parser.add_argument('-t', '--title', default='Trajectory Plot')
    parser.add_argument('-a', '--all', action='store_true')
    parser.add_argument('csvfile', nargs='+')
    inputs = parser.parse_args()

    nPlots   = len(inputs.csvfile)
    nRows    = int(pow(nPlots, 0.5))
    nColumns = int(math.ceil(float(nPlots) / nRows))
    iFigure = 1

    overlays = []
    if (inputs.overlay != None):
        for file in inputs.overlay:
            observables = LoadFile(file)
            overlays.append(observables)

    pyplot.figure(inputs.title)
    for file in inputs.csvfile:
        pyplot.subplot(nRows, nColumns, iFigure)
        PlotFile(file, overlays, inputs.all)
        iFigure += 1
    pyplot.show()
