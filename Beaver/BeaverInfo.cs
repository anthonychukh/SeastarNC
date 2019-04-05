using System;
using System.Drawing;
using Grasshopper.Kernel;
using System.Data.SqlClient;

namespace BeaverGrasshopper
{
    public class BeaverInfo : GH_AssemblyInfo
    {
        public override string Name
        {
            get
            {
                return "Beaver";
            }
        }
        public override Bitmap Icon
        {
            get
            {
                //Return a 24x24 pixel bitmap to represent this GHA library.
                return null;
            }
        }
        public override string Description
        {
            get
            {
                //Return a short string describing the purpose of this GHA library.
                return "Convert Rhino Curves into GCode for 3D printing and CNC routing";
            }
        }
        public override Guid Id
        {
            get
            {
                return new Guid("516f83e8-3712-4ece-b326-5f5eb86e3712");
            }
        }

        public override string AuthorName
        {
            get
            {
                //Return a string identifying you or your company.
                return "Anthony Chu";
            }
        }
        public override string AuthorContact
        {
            get
            {
                //Return a string representing your preferred contact details.
                return "chukha@gsd.harvard.edu";
            }
        }
    }
}
