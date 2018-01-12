using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientWrapeprTester
{
    class Program
    {
        static void Main(string[] args)
        {
            SpeechClientWrapper.Speech speech = new SpeechClientWrapper.Speech("insert token here");
            speech.Update += Speech_Update; ;
            while(true)
            {

            }
        }

        private static void Speech_Update(object sender, SpeechClientWrapper.SpeechUpdateEventArgs e)
        {
            Console.WriteLine(e.Moment);
        }
    }
}
