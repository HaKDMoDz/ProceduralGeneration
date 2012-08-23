﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CjClutter.OpenGl.Input
{
    public class ByteArrayComparer : IEqualityComparer<byte[]>
    {
        public bool Equals(byte[] x, byte[] y)
        {
            if(x.Length != y.Length)
            {
                return false;
            }

            for (int i = 0; i < x.Length; i++)
            {
                if(x[i] != y[i])
                {
                    return false;
                }
            }

            return x.Length == y.Length;
        }

        public int GetHashCode(byte[] obj)
        {
            return obj.GetHashCode();
        }
    }
}