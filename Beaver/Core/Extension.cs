using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;

namespace Seastar.Core
{

    public static class Extension
    {
        //public static GH_Structure<T> ToGHStructure<T, U>(DataTree<U> _data) where T : IComparable<T>
        //{
        //    GH_Structure<T> dataOut = new GH_Structure<T>();
        //    for(int i = 0; i < _data.BranchCount; i++)
        //    {
        //        for(int j = 0; j< _data.Branch(i).Count; j++)
        //        {
        //            var obj = GH_Convert.ToVariant(_data.Branch(i)[j]);
        //            if(obj.GetType() == T)
        //            {

        //            }
        //        }
        //    }
        //}

        public static List<T> SetListLength<T>(List<T> _list, int _targetLength, T _replacement) where T : IComparable<T>
        {
            List<T> newList = new List<T>();

            for (int i = 0; i < _targetLength; i++)
            {
                if (i < _list.Count)
                {
                    newList.Add(_list[i]);
                }
                else
                {
                    newList.Add(_replacement);
                }
            }

            return newList;
        }

        public static void Slider(int _input, string _name, Decimal _defVal, int _psc, GH_Component _this) //Add Slider 
        {
            if (_this.Params.Input[_input].SourceCount == 0) //only add slider if no source input
            {
                var slider = new Grasshopper.Kernel.Special.GH_NumberSlider();
                slider.CreateAttributes();
                slider.Name = _name;
                slider.NickName = _name;


                slider.Slider.Maximum = _defVal;
                slider.Slider.DecimalPlaces = _psc;
                slider.SetSliderValue(_defVal);

                slider.Attributes.Pivot = new PointF((float)_this.Attributes.DocObject.Attributes.Bounds.Left - slider.Attributes.Bounds.Width - 70, (float)_this.Params.Input[_input].Attributes.Bounds.Y + 10);
                _this.OnPingDocument().AddObject(slider, false);

                _this.Params.Input[_input].AddSource(slider);
                slider.ExpireSolution(true);
            }
        }

        public static void Slider(int _inputIndex, string _name, double _defaultValue, double _min, double _max, int _precision, GH_Component _this) //Add Slider 
        {
            if (_this.Params.Input[_inputIndex].SourceCount == 0) //only add slider if no source input
            {
                var slider = new Grasshopper.Kernel.Special.GH_NumberSlider();
                slider.CreateAttributes();
                slider.Name = _name;
                slider.NickName = _name;

                slider.Slider.Maximum = Convert.ToDecimal(_max);
                slider.Slider.Minimum = Convert.ToDecimal(_min);
                slider.Slider.DecimalPlaces = _precision;
                slider.SetSliderValue(Convert.ToDecimal(_defaultValue));

                slider.Attributes.Pivot = new PointF((float)_this.Attributes.DocObject.Attributes.Bounds.Left - slider.Attributes.Bounds.Width - 70, (float)_this.Params.Input[_inputIndex].Attributes.Bounds.Y + 10);
                _this.OnPingDocument().AddObject(slider, false);

                _this.Params.Input[_inputIndex].AddSource(slider);
                slider.ExpireSolution(true);
            }
        }

        public static void Slider(int _inputIndex, string _name, double _defaultValue, GH_Component _this) //Add Slider 
        {
            double _max;
            double _min;
            int _precision;
            int dPlace = 0;
            if (Math.Abs(_defaultValue) < 1)
            {
                dPlace = 0;
            }
            else
            {
                dPlace = Convert.ToInt32(Math.Abs(_defaultValue)).ToString().Length;
            }

            if (_defaultValue > 0)
            {
                _max = Math.Pow(10, dPlace);
                _min = 0;
            }
            else
            {
                _min = Math.Pow(10, dPlace) * -1;
                _max = 0;
            }

            string p = _defaultValue.ToString();
            if (p.Contains("."))
            {
                _precision = p.Split('.')[1].Length;
            }
            else
            {
                _precision = 0;
            }

            Extension.Slider(_inputIndex, _name, _defaultValue, _min, _max, _precision, _this);
        }

        public static void DropDown(int _input, string _name, List<string> _items, GH_Component _this) //Add Dropdown menue
        {
            if (_this.Params.Input[_input].SourceCount == 0 && _this.Params.Input[0].SourceCount > 0)
            {
                var vallist = new Grasshopper.Kernel.Special.GH_ValueList();
                vallist.CreateAttributes();
                vallist.Name = _name;
                vallist.NickName = _name;
                vallist.ListMode = Grasshopper.Kernel.Special.GH_ValueListMode.DropDown;

                int inputcount = _this.Params.Input[_input].SourceCount;
                vallist.Attributes.Pivot = new PointF((float)_this.Attributes.DocObject.Attributes.Bounds.Left - vallist.Attributes.Bounds.Width - 100, (float)_this.Params.Input[_input].Attributes.Bounds.Y);

                vallist.ListItems.Clear();

                for (int i = 0; i < _items.Count; i++)
                {
                    vallist.ListItems.Add(new Grasshopper.Kernel.Special.GH_ValueListItem(_items[i], i.ToString()));

                }
                vallist.Description = _items.Count.ToString() + " ";


                _this.OnPingDocument().AddObject(vallist, false);

                _this.Params.Input[_input].AddSource(vallist);
                vallist.ExpireSolution(true);


                //_this.AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "dropdown added");
            }
        }

        /// <summary>
        /// Create and append dropdown value list to this component
        /// </summary>
        /// <param name="_input">Index of input to connect to</param>
        /// <param name="_name">Name of the value list</param>
        /// <param name="_items">List of item names</param>
        /// <param name="_values">Corresponding list of value</param>
        /// <param name="_this">This component</param>
        public static void DropDown(int _input, string _name, List<string> _items, List<string> _values, GH_Component _this) //Add Dropdown menue
        {
            if (_this.Params.Input[_input].SourceCount == 0)
            {
                var vallist = new Grasshopper.Kernel.Special.GH_ValueList();
                vallist.CreateAttributes();
                vallist.Name = _name;
                vallist.NickName = _name;
                vallist.ListMode = Grasshopper.Kernel.Special.GH_ValueListMode.DropDown;

                int inputcount = _this.Params.Input[_input].SourceCount;
                vallist.Attributes.Pivot = new PointF((float)_this.Attributes.DocObject.Attributes.Bounds.Left - vallist.Attributes.Bounds.Width - 100, (float)_this.Params.Input[_input].Attributes.Bounds.Y);

                vallist.ListItems.Clear();

                for (int i = 0; i < _items.Count; i++)
                {
                    vallist.ListItems.Add(new Grasshopper.Kernel.Special.GH_ValueListItem(_items[i], _values[i]));
                }
                vallist.Description = _items.Count.ToString() + " ";


                _this.OnPingDocument().AddObject(vallist, false);

                _this.Params.Input[_input].AddSource(vallist);
                vallist.ExpireSolution(true);


                //_this.AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "dropdown added");
            }
        }

        /// <summary>
        /// Expire another component that is on the same document as this
        /// </summary>
        /// <param name="_otherName">Other component's name</param>
        /// <param name="_this">this component</param>
        public static void expireOthers(string _otherName, GH_Component _this)
        {
            foreach (IGH_DocumentObject obj in _this.OnPingDocument().Objects)
            {
                if (obj.Name == _otherName)
                {
                    obj.ExpireSolution(true);
                    break;
                }
            }
        }
    }


    public class Resources
    {
        private static ResourceManager resourceMan;

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public static ResourceManager ResourceManager
        {
            get
            {
                if (resourceMan == null)
                {
                    ResourceManager resourceManager = resourceMan = new ResourceManager("Seastar.Properties.Resources", typeof(Resources).Assembly);
                }
                return resourceMan;
            }
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public static CultureInfo Culture { get; set; }

        public static Bitmap joker
        {
            get
            {
                object obj = ResourceManager.GetObject("joker2", Culture);
                return (Bitmap)obj;
            }
        }
    }


}
