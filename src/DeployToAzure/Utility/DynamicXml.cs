using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Xml.Linq;

namespace DeployToAzure.Utility
{
    public class DynamicXml : DynamicObject, IEnumerable
    {
        private readonly List<XElement> _elements;

        public DynamicXml(string text)
        {
            var doc = XDocument.Parse(text);
            _elements = new List<XElement> { doc.Root };
        }

        private DynamicXml(XElement element)
        {
            _elements = new List<XElement> { element };
        }

        private DynamicXml(IEnumerable<XElement> elements)
        {
            _elements = new List<XElement>(elements);
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = null;
            switch (binder.Name)
            {
                case "Value":
                    result = _elements[0].Value;
                    break;
                case "Count":
                    result = _elements.Count;
                    break;
                default:
                    {
                        var attr = _elements[0].Attribute(XName.Get(binder.Name));
                        if (attr != null)
                            result = attr;
                        else
                        {
                            var items = _elements.Descendants(XName.Get(binder.Name));
                            if (items == null)
                                return true;
                            var itemsArray = items.ToArray();
                            if (itemsArray.Length == 0)
                                return false;
                            result = new DynamicXml(itemsArray);
                        }
                    }
                    break;
            }
            return true;
        }

        public override bool TryConvert(ConvertBinder binder, out object result)
        {
            if (binder.ReturnType == typeof(string))
            {
                result = _elements[0].Value;
                return true;
            }
            return base.TryConvert(binder, out result);
        }

        public override bool TryGetIndex(GetIndexBinder binder,
             object[] indexes, out object result)
        {
            var ndx = (int)indexes[0];
            result = new DynamicXml(_elements[ndx]);
            return true;
        }

        public IEnumerator GetEnumerator()
        {
            return _elements.Select(element => new DynamicXml(element)).GetEnumerator();
        }
    }
}
