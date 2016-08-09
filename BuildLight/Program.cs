using Blink1Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BuildLight
{
    class Program
    {
        static void Main(string[] args)
        {
            Blink1 blink1 = new Blink1();

            int count = blink1.enumerate();
            Console.WriteLine("detected " + count + " blink(1 devices");

            if (count != 0)
            {
                string serialnum = blink1.getCachedSerial(0);
                Console.WriteLine("blink(1) serial number: " + serialnum);
            }

            blink1.open();

            Console.WriteLine("setting white");
            blink1.setRGB(255, 0, 0);

            Thread.Sleep(5000);

            blink1.close();

            Console.ReadLine();
        }
    }
}
