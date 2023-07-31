using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel;
using Grasshopper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Melanchall.DryWetMidi.Smf.Interaction;

namespace Seastar.Core
{

    public enum Info
    {
        channel = 1,
        note = 2,
        time,
        duration
    }

    public class MidiExtension
    {
        /// <summary>
        /// Get information of note.
        /// </summary>
        /// <param name="_note"></param>
        /// <returns></returns>
        public static string NoteInfo(Note _note)
        {
            string info = "";
            info += _note.Channel.ToString() + " ";
            info += _note.NoteNumber.ToString() + " ";
            info += _note.Time.ToString() + " ";
            info += _note.Length.ToString();
            //info += _note.

            return info;
        }

        public static string NoteInfo(Note _note, Info _info)
        {
            string info = "";
            switch (_info)
            {
                case Info.channel:
                    info += _note.Channel.ToString() + " ";
                    break;
                case Info.note:
                    info += _note.NoteNumber.ToString() + " ";
                    break;
                case Info.time:
                    info += _note.Time.ToString() + " ";
                    break;
                case Info.duration:
                    info += _note.Length.ToString() + " ";
                    break;
            }


            return info;
        }

        public static GH_Structure<GH_String> NoteInfo(List<Note> _noteList)
        {
            GH_Structure<GH_String> channelStrings = new GH_Structure<GH_String>();
            for (int i = 0; i < _noteList.Count; i++)
            {
                Note thisNote = _noteList[i];
                GH_Path pth = new GH_Path(thisNote.Channel);

                //.....to string.....................
                string noteInfo = MidiExtension.NoteInfo(thisNote);
                channelStrings.Append(new GH_String(noteInfo), pth);
            }

            return channelStrings;
        }

        public static GH_Structure<GH_String> NoteInfo(List<Note> _noteList, Info _info)
        {
            GH_Structure<GH_String> channelStrings = new GH_Structure<GH_String>();
            for (int i = 0; i < _noteList.Count; i++)
            {
                Note thisNote = _noteList[i];
                GH_Path pth = new GH_Path(thisNote.Channel);

                //.....to string.....................
                string noteInfo = MidiExtension.NoteInfo(thisNote, _info);
                channelStrings.Append(new GH_String(noteInfo), pth);
            }

            return channelStrings;
        }


        public static DataTree<Note> ChannelTree(List<Note> _noteList)
        {
            DataTree<Note> nT = new DataTree<Note>();
            for (int i = 0; i < _noteList.Count; i++)
            {
                Note thisNote = _noteList[i];
                GH_Path pth = new GH_Path(thisNote.Channel);
                nT.Add(thisNote, pth);  //Branch index == channel
            }
            return nT;
        }
        public static List<long> AllUniqueTime(List<Note> _notes)
        {
            List<long> uTime = new List<long>();
            List<long> allTime = new List<long>();

            for (int i = 0; i < _notes.Count; i++)
            {
                allTime.Add(_notes[i].Time);
            }
            allTime.Sort();
            long lastTime = 0;
            uTime.Add(lastTime);

            for (int j = 0; j < allTime.Count; j++)
            {
                long thisTime = allTime[j];
                if (thisTime == lastTime)
                {

                }
                else
                {
                    lastTime = thisTime;
                    uTime.Add(thisTime);
                }
            }

            return uTime;
        }

        /// <summary>
        /// Break down a note to equal time segment, listing note number at every time step
        /// </summary>
        /// <param name="_totalTime">Total time this note last until next note begin, including play time and silence between notes</param>
        /// <param name="_playTime">Time this note is played</param>
        /// <param name="_note">Note number</param>
        /// <param name="step">Time step. Good number is 20(ms)</param>
        /// <returns></returns>
        public static IEnumerable<GH_Integer> Segment(long _totalTime, long _playTime, int _note, int step)
        {
            List<GH_Integer> segs = new List<GH_Integer>();
            for (int i = 0; i * step < _totalTime; i++)
            {
                if (i * step < _playTime)
                {
                    GH_Integer gnote = new GH_Integer();
                    GH_Convert.ToGHInteger(_note, GH_Conversion.Primary, ref gnote);
                    segs.Add(gnote);
                }
                else
                {
                    segs.Add(new GH_Integer(0));
                }
            }
            return segs;
        }

        /// <summary>
        /// Divide notes into segment of equal time setp and list out note number played at each step
        /// </summary>
        /// <param name="_noteTree">Data tree of Notes to segment</param>
        /// <param name="_stepSize">Size of each time step in ms. Good number is 20(ms)</param>
        /// <param name="_channels">List of channels to export</param>
        /// <returns></returns>
        public static GH_Structure<GH_Integer> TimeStep(DataTree<Note> _noteTree, int _stepSize, List<int> _channels)
        {
            GH_Structure<GH_Integer> channelTimeStep = new GH_Structure<GH_Integer>();
            for (int i = 0; i < _channels.Count; i++)
            {
                int thisChannel = _channels[i];
                GH_Path pth = new GH_Path(thisChannel);

                if (!_noteTree.PathExists(pth))
                {
                    Debug.WriteLine("MidiExtension::TimeStep::Channel" + pth.ToString() + " does not exist");
                    continue;
                }


                long time = 0;
                long lastTime = 0;
                var notes = _noteTree.Branch(thisChannel);
                long firstTimeLapse = notes[0].Time;

                channelTimeStep.AppendRange(MidiExtension.Segment(firstTimeLapse, notes[0].Length, notes[0].NoteNumber, _stepSize), pth);

                for (int j = 0; j < notes.Count; j++)
                {
                    time = notes[j].Time;
                    long timeLapse = notes[j].Time - lastTime;
                    channelTimeStep.AppendRange(MidiExtension.Segment(timeLapse, notes[j].Length, notes[j].NoteNumber, _stepSize), pth);
                    lastTime = notes[j].Time;
                }
            }

            return channelTimeStep;
        }

        /// <summary>
        /// Divide notes into segment of equal time setp and list out note number played at each step
        /// </summary>
        /// <param name="_noteTree">Data tree of Notes to segment</param>
        /// <param name="_stepSize">Size of each time step in ms. Good number is 20(ms)</param>
        /// <param name="_channels">Number of channels to export, starting at channel 0</param>
        /// <returns></returns>
        public static GH_Structure<GH_Integer> TimeStep(DataTree<Note> _noteTree, int _stepSize, int _channels)
        {
            List<int> _ch = new List<int>();
            for (int i = 0; i < _channels; i++)
            {
                _ch.Add(i);
            }
            return TimeStep(_noteTree, _stepSize, _ch);
        }
    }

}
