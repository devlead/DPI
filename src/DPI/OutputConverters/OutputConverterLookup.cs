using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DPI.Models;

namespace DPI.OutputConverters
{
    public class OutputConverterLookup : ILookup<OutputFormat, IOutputConverter>
    {
        private ILookup<OutputFormat, IOutputConverter> Lookup { get; }
        
        public OutputConverterLookup(IEnumerable<IOutputConverter> outputConverters)
        {
            Lookup = outputConverters
                .ToLookup(
                    key => key.OutputFormat,
                    value => value
                );
        }

        public IEnumerator<IGrouping<OutputFormat, IOutputConverter>> GetEnumerator()
            => Lookup.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        public bool Contains(OutputFormat key) 
            => Lookup.Contains(key);


        public int Count => Lookup.Count;

        public IEnumerable<IOutputConverter> this[OutputFormat key] => Lookup[key];
    }
}