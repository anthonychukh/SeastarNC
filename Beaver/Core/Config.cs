using Grasshopper.Kernel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seastar.Core
{
    /// <summary>
    /// Read and set configuration.
    /// </summary>
    public class Config 
    {
        public Hashtable Settings;
        public Machine Machine = new Machine();

        public Config()
        {

        }

        public Config(Machine _machine)
        {
            Machine = _machine;

            //add raxes
        }
        public Config(List<string> _config) //construct config instance with setting strings
        {
            Settings = Config.ToHashtable(_config);
        }

        public Config(List<string> _config, Machine _machine) //construct config instance with setting strings
        {
            Settings = Config.ToHashtable(_config);
            Machine = _machine;
        }

        public static Hashtable ToHashtable(List<string> _config)
        {
            Hashtable s = new Hashtable();
            foreach (string cc in _config)
            {
                if (!cc.Contains("[") && !s.ContainsKey(GetSettingName(cc)))
                {
                    s.Add(GetSettingName(cc), GetSettingValue(cc));
                }
            }
            return s;
        }

        /// <summary>
        /// Append a slider to cooresponding input node if a value can be found
        /// </summary>
        /// <param name="_config">configuration</param>
        /// <param name="_this">this component</param>
        /// <param name="_createZero">If True, a slider of 0 value will be appended to input node where value is NOT found.\nIf false, no slider will be created for that node</param>
        public static void CreateSlider(Config _config, GH_Component _this, bool _createZero)  //create slider if find setting
        {
            Hashtable cfg = _config.Settings;

            for (int i = 0; i < _this.Params.Input.Count - 1; i++)
            {
                string key = _this.Params.Input[i].Name;
                if (_this.Params.Input[i].SourceCount == 0)
                {
                    if (cfg.ContainsKey(key))
                    {
                        string value = cfg[key].ToString();
                        if (value.Contains("%"))
                        {
                            //value.Trim('%');
                            double v = Convert.ToDouble(value.Trim('%')) / 100;
                            value = v.ToString();
                        }
                        if (value.Contains(","))
                        {
                            //value.Trim('%');
                            double v = Convert.ToDouble(value.Split(',')[0]) / 100;
                            value = v.ToString();
                        }
                        Extension.Slider(i, key, Convert.ToDouble(value), _this);
                    }
                    else
                    {
                        if (_createZero)
                        {
                            //Extension.Slider(i, key, 0, 2, _this);
                            Extension.Slider(i, key, -1, -1, 100, 2, _this);
                        }
                    }
                }
            }
        }

        public static List<string> UpdateConfig(List<string> _config, GH_Component _this) //update input config string by checking component param input
        {
            List<string> cfgOut = new List<string>();
            Hashtable cfg = Config.ToHashtable(_config);

            for (int i = 0; i < _this.Params.Input.Count - 1; i++)
            {
                string key = _this.Params.Input[i].Name;
                if (_this.Params.Input[i].SourceCount == 0)
                {
                    if (cfg.ContainsKey(key))
                    {
                        string value = cfg[key].ToString();
                        if (value.Contains("%"))
                        {
                            //value.Trim('%');
                            double v = Convert.ToDouble(value.Trim('%')) / 100;
                            value = v.ToString();
                        }
                        if (value.Contains(","))
                        {
                            //value.Trim('%');
                            double v = Convert.ToDouble(value.Split(',')[0]) / 100;
                            value = v.ToString();
                        }

                        Extension.Slider(i, key, Convert.ToDecimal(value), 2, _this);
                        string s = key + " = " + value;
                        cfgOut.Add(s);
                    }
                    else
                    {
                        Extension.Slider(i, key, 0, 2, _this);
                        string s = key + " = " + _this.Params.Input[i].VolatileData.AllData(false).ToString();
                        //string s = key + " = " + _this.DA.GetData

                        cfgOut.Add(s);
                    }
                }
                else
                {
                    string s = key + " = " + _this.Params.Input[i].VolatileData.AllData(false).ToString();
                    cfgOut.Add(s);
                }
            }

            return cfgOut;
        }


        public static string[] GetSetting(string _setting)
        {
            string[] setting = new string[2];

            if (_setting.Contains("="))
            {
                string[] str = _setting.Split('=');
                setting[0] = str[0].Replace(" ", "");
                setting[1] = str[1].Replace(" ", "");
                return setting;
            }
            else
            {
                return null;
            }
        }


        /// <summary>
        /// Get the name of this setting string. Setting would be in format of {name} = {value}
        /// </summary>
        /// <param name="_setting">setting</param>
        /// <returns></returns>
        public static string GetSettingName(string _setting)
        {
            if (_setting.Contains("="))
            {
                string[] str = _setting.Split('=');
                string val = str[0].Replace(" ", "");
                return val;
            }
            else
            {
                return null;
            }

        }


        /// <summary>
        /// Get the value of this setting string. Setting would be in format of {name} = {value}
        /// </summary>
        /// <param name="_setting"></param>
        /// <returns></returns>
        public static string GetSettingValue(string _setting)
        {
            if (_setting.Contains("="))
            {
                string[] str = _setting.Split('=');
                string val = str[1].Replace(" ", "");
                return val;
            }
            else
            {
                return null;
            }
        }



        //Settings to ask for.................................................................................

        public enum PrinterSetting //setting to use from ini file
        {
            print_shape,
            printer_x,
            printer_y,
            printer_height,
            allow_negavtive_x,
            allow_negavtive_y,
            allow_negavtive_z
        }


        public enum RouterSetting
        {
            x_size,
            y_size,
            z_size
        }

        public enum PrintSetting
        {
            xy_tolerance,
            layer_height,
            first_layer_height,
            disable_fan_first_layers,
            extrusion_width,
            infill_extrusion_width,
            first_layer_extrusion_multiplier,
            extrusion_multiplier,
            infill_overlap,
            perimeter,
            fill_density,
            print_speed,
            perimeter_speed,
            external_perimeter_speed,
            small_perimeter_speed,
            corner_pause,
            corner_threshold,
            infill_speed,
            first_layer_speed,
            travel_speed,
            speed_multiplier,
            raft_layers
        }

        public enum FilamentSetting
        {
            filament_name,
            filament_diameter,
            retract_speed,
            retract_length,
            filament_density,
            filament_cost,
            disable_fan_first_layers,
            temperature,
            bed_temperature
        }
    }

}
