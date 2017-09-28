﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using Config.Net.TypeParsers;

namespace Config.Net.Core
{
   class IoHandler
   {
      private readonly IEnumerable<IConfigStore> _stores;
      private readonly DefaultParser _defaultParser = new DefaultParser();
      private readonly ConcurrentDictionary<Type, ITypeParser> _parsers = new ConcurrentDictionary<Type, ITypeParser>();

      public IoHandler(IEnumerable<IConfigStore> stores)
      {
         _stores = stores ?? throw new ArgumentNullException(nameof(stores));

         foreach (ITypeParser pc in GetBuiltInParsers())
         {
            foreach(Type t in pc.SupportedTypes)
            {
               _parsers[t] = pc;
            }
         }
      }

      public object Read(PropertyOptions property)
      {
         return ReadNonCached(property);
      }

      public object ReadNonCached(PropertyOptions property)
      {
         //assume configuration is valid

         object result;

         string rawValue = ReadFirstValue(property.Name);
         if(rawValue == null)
         {
            result = property.DefaultValue;
         }
         else
         {
            if(_defaultParser.IsSupported(property.Type))   //type here must be a non-nullable one
            {
               if(!_defaultParser.TryParse(rawValue, property.Type, out result))
               {
                  result = property.DefaultValue;
               }
            }
            else
            {
               ITypeParser typeParser = GetParser(property.Type);
               if(!typeParser.TryParse(rawValue, property.Type, out result))
               {
                  result = property.DefaultValue;
               }
            }
         }

         return result;
      }

      private string ReadFirstValue(string key)
      {
         foreach (IConfigStore store in _stores)
         {
            if (store.CanRead)
            {
               string value = store.Read(key);

               if (value != null) return value;
            }
         }
         return null;
      }

      /// <summary>
      /// Scans assembly for types implementing <see cref="ITypeParser"/> and builds Type => instance dictionary.
      /// Not sure if I should use reflection here, however the assembly is small and this shouldn't cause any
      /// performance issues
      /// </summary>
      /// <returns></returns>
      private static IEnumerable<ITypeParser> GetBuiltInParsers()
      {
         return new ITypeParser[]
         {
            new DoubleParser(),
            new IntParser(),
            new JiraTimeParser(),
            new LongParser(),
            new StringArrayParser(),
            new StringParser(),
            new TimeSpanParser(),
            new CoreParsers(),
            new NetworkCredentialParser()
         };
      }

      private ITypeParser GetParser(Type t)
      {
         ITypeParser result;
         _parsers.TryGetValue(t, out result);
         return result;
      }

   }
}
