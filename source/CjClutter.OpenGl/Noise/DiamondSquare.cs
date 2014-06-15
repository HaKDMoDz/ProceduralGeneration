﻿namespace CjClutter.OpenGl.Noise
{
    public class DiamondSquare
    {
        public double[] Generate(double h0, double h1, double h2, double h3, int levels)
        {
            var old = new[] { h0, h1, h2, h3 };
            var result = old;

            var rowVertices = 2;
            for (var i = 0; i < levels; i++)
            {
                result = Subdivide(old, rowVertices);
            }

            return result;
        }

        private double[] Subdivide(double[] old, int rowVertices)
        {
            var newRowVertices = (rowVertices - 1) * 2 + 1;
            var newValues = new double[newRowVertices * newRowVertices];

            for (var oldRow = 0; oldRow < rowVertices; oldRow++)
            {
                for (int oldColumn = 0; oldColumn < rowVertices; oldColumn++)
                {
                    var newIndex = oldRow * newRowVertices * 2 + oldColumn * 2;
                    newValues[newIndex] = old[rowVertices * oldRow + oldColumn];
                }
            }

            for (int row = 0; row < newRowVertices; row += 2)
            {
                for (int column = 1; column < newRowVertices; column += 2)
                {
                    var index = row * newRowVertices + column;
                    var value = (newValues[index - 1] + newValues[index + 1]) / 2;
                    newValues[index] = value;
                }
            }

            for (int row = 1; row < newRowVertices; row += 2)
            {
                for (int column = 0; column < newRowVertices; column += 2)
                {
                    var index = row * newRowVertices + column;
                    var previous = (row - 1) * newRowVertices + column;
                    var next = (row + 1) * newRowVertices + column;

                    var value = (newValues[previous] + newValues[next]) / 2;
                    newValues[index] = value;
                }
            }

            for (var row = 1; row < newRowVertices; row += 2)
            {
                for (var column = 1; column < newRowVertices; column += 2)
                {
                    var index = row * newRowVertices + column;
                    var previous = (row - 1) * newRowVertices + column;
                    var next = (row + 1) * newRowVertices + column;

                    var value = (newValues[previous] + newValues[next] + newValues[index - 1] + newValues[index + 1]) / 4;
                    newValues[index] = value;
                }
            }

            return newValues;
        }
    }
}