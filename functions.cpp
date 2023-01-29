#include "pch.h"
#include "functions.h"


void ScaleDown(int pixelTab [])
{
    for (int i = 0; i < 4; i++)
    {
        pixelTab[0 + (i * 3)] = (pixelTab[0 + (i * 6)]
            + pixelTab[3 + (i * 6)]) / 2;//R

        pixelTab[1 + (i * 3)] = (pixelTab[1 + (i * 6)]
            + pixelTab[4 + (i * 6)]) / 2;//G

        pixelTab[2 + (i * 3)] = (pixelTab[2 + (i * 6)]
            + pixelTab[5 + (i * 6)]) / 2;//B

    }
}