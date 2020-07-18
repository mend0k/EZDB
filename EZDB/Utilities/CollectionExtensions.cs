using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace EZDB.Utilities
{
    public static class CollectionExtensions
    {
        public static List<T> ToModelList<T>(this ICollection collection)
        {
            return collection.Cast<T>().ToList();

            //T[] arr = new T[collection.Count];
            //collection.CopyTo(arr, 0);
                        
            //return arr.ToList();
        }

    }
}
