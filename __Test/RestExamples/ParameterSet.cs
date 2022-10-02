using Haley.Abstractions;
using Haley.Enums;
using Haley.Events;
using Haley.Models;
using Haley.MVVM;
using Haley.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace RestExamples
{
    public class ParameterSet :BaseVM
    {
        public string Id { get; }

        private string _key;
        public string Key
        {
            get { return _key; }
            set { SetProp(ref _key, value); }
        }

        private string _value;
        public string Value
        {
            get { return _value; }
            set { SetProp(ref _value, value); }
        }

        public override string ToString()
        {
            var _result = Key + " " + Value;
            if (string.IsNullOrWhiteSpace(_result)) _result = Id;
            return _result.Trim();
        }

        public override bool Equals(object obj)
        {
            return Id.Equals((obj as ParameterSet).Id);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public ParameterSet(string key,string value)
        { 
            Id = Guid.NewGuid().ToString();
            Key = key;
            Value = value;
        }
    }
}
